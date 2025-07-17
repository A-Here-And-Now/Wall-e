using walle.Domain;
using walle.ServiceLayer;

namespace walle.Domain
{
    public class Wallet
    {
        public string WalletId { get; private set; }
        private List<Transaction> Transactions { get; set; }
        public Currency BaseCurrency { get; private set; }
        private IWalletTransactionFraudDetector WalletTransactionFraudDetector { get; set; }

        public Wallet(string walletId, Currency baseCurrency)
        {
            WalletId = walletId;
            Transactions = new List<Transaction>();
            BaseCurrency = baseCurrency;
            WalletTransactionFraudDetector = (IWalletTransactionFraudDetector)new WalletTransactionFraudDetector();
        }

        public List<Transaction> GetTransactionsForCurrency(Currency currency)
        {
            var res = new List<Transaction>();
            var transactions = Transactions.Where(x => x.Currency == currency).ToList();
            res.AddRange(transactions.GetRange(0, transactions.Count));
            return res;
        }

        public (bool, FraudDetectionException?) TryApplyTransaction(Transaction txn)
        {
            return WalletTransactionFraudDetector.CheckAndAddEntry(txn);
        }
    }
}