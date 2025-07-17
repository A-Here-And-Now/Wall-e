using walle.Domain;
using walle.Domain.Enums;

namespace walle.ServiceLayer
{
    public class WalletTransactionFraudDetector
    {
        private Dictionary<Currency, Queue<DateTime>> recentWithdrawals { get; set; }
        private DateTime lastDeposit { get; set; }
        private DateTime lastWithdrawal { get; set; }
        private Dictionary<Currency, int> count;
        private Dictionary<Currency, decimal> sum_x;
        private Dictionary<Currency, decimal> sum_x2;
        private int stdDevThresholdMultiplier { get; set; }
        public WalletTransactionFraudDetector(int _stdDevThresholdMultiplier = 3)
        {
            recentWithdrawals = new Dictionary<Currency, Queue<DateTime>>();
            count = new Dictionary<Currency, int>();
            sum_x = new Dictionary<Currency, decimal>();
            sum_x2 = new Dictionary<Currency, decimal>();
            stdDevThresholdMultiplier = _stdDevThresholdMultiplier;
        }

        public (bool, FraudDetectionException?) CheckAndAddEntry(Transaction txn)
        {
            if (txn.Type == TransactionType.Withdrawal)
            {
                if (recentWithdrawals.ContainsKey(txn.Currency) && recentWithdrawals[txn.Currency].Count > 4
                        && recentWithdrawals[txn.Currency].Dequeue().AddMinutes(2) >= txn.Timestamp)
                    return (false, new FraudDetectionException(FraudDetectionType.RateLimitExceeded,
                        new Exception("Too many transactions in small window.")));
                else if (lastDeposit.AddSeconds(30) >= txn.Timestamp)
                    return (false, new FraudDetectionException(FraudDetectionType.SuspiciousRoundTrip,
                        new Exception("Suspicious round trip of Deposit/Withdrawal within 30 seconds of eachother.")));
                recentWithdrawals[txn.Currency].Enqueue(txn.Timestamp);
                lastWithdrawal = txn.Timestamp;
            }
            else
            {
                if (lastWithdrawal.AddSeconds(30) >= txn.Timestamp)
                    return (false, new FraudDetectionException(FraudDetectionType.SuspiciousRoundTrip,
                        new Exception("Suspicious round trip of Deposit/Withdrawal within 30 seconds of eachother.")));
                lastDeposit = txn.Timestamp;
            }

            count[txn.Currency]++;
            sum_x[txn.Currency] += txn.Amount;
            sum_x2[txn.Currency] += txn.Amount * txn.Amount;

            if (IsAnomalousTransaction(txn))
            {
                count[txn.Currency]--;
                sum_x[txn.Currency] -= txn.Amount;
                sum_x2[txn.Currency] -= txn.Amount * txn.Amount;
                return (false, new FraudDetectionException(FraudDetectionType.SuspiciousRoundTrip,
                    new Exception("Anomalous transaction detected.")));
            }
            return (true, null);
        }

        private decimal GetMean(Currency currency)
        {
            if (count[currency] == 0) return 0.0M;
            return sum_x[currency] / count[currency];
        }

        private decimal GetStandardDeviation(Currency currency)
        {
            if (count[currency] <= 1) return 0.0M; // Need at least two data points for standard deviation

            decimal mean = GetMean(currency);
            decimal variance = (sum_x2[currency] / count[currency]) - (mean * mean);

            // Handle potential negative variance due to floating point inaccuracies
            if (variance < 0) return 0.0M;

            return (decimal)Math.Sqrt((double)variance);
        }

        private bool IsAnomalousTransaction(Transaction txn)
        {
            return Math.Abs(txn.Amount - GetMean(txn.Currency)) > stdDevThresholdMultiplier * GetStandardDeviation(txn.Currency);
        }
    }
}