using System.Collections.Concurrent;
using System.Threading.Tasks;

public enum TrannyType
{
    Deposit,
    Withdrawal
}

public class Transaction
{
    public string TransactionId { get; }
    public DateTime Timestamp { get; }
    public decimal Amount { get; }
    public TrannyType Type { get; }
    public Currency Currency { get; }
    public Transaction(string id, DateTime timestamp, decimal amount, TrannyType type, Currency currency)
    {
        TransactionId = id;
        Timestamp = timestamp;
        Amount = amount;
        Type = type;
        Currency = currency;
    }

    public decimal GetAmount()
    {
        return Type == TrannyType.Deposit ? Amount : -Amount;
    }
}

public enum FraudDetectionType {
    RateLimitExceeded,
    SuspiciousRoundTrip,
    AnomalousTransaction
}

public class FraudDetectionException {
    public FraudDetectionType Type { get; set; }
    public Exception Exception { get; set; }

    public FraudDetectionException(FraudDetectionType t, Exception e)
    {
        Type = t;
        Exception = e;
    }
}

public class FraudDetector
{
    private Dictionary<Currency, Queue<DateTime>> recentWithdrawals { get; set; }
    private DateTime lastDeposit { get; set; }
    private DateTime lastWithdrawal { get; set; }
    private Dictionary<Currency, int> count;
    private Dictionary<Currency, decimal> sum_x;
    private Dictionary<Currency, decimal> sum_x2;
    private int stdDevThresholdMultiplier { get; set; }
    public FraudDetector(int _stdDevThresholdMultiplier = 3)
    {
        recentWithdrawals = new Dictionary<Currency, Queue<DateTime>>();
        count = new Dictionary<Currency, int>();
        sum_x = new Dictionary<Currency, decimal>();
        sum_x2 = new Dictionary<Currency, decimal>();
        stdDevThresholdMultiplier = _stdDevThresholdMultiplier;
    }

    public (bool, FraudDetectionException?) CheckAndAddEntry(Transaction txn)
    {
        if (txn.Type == TrannyType.Withdrawal)
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

    public decimal GetMean(Currency currency)
    {
        if (count[currency] == 0) return 0.0M;
        return sum_x[currency] / count[currency];
    }

    public decimal GetStandardDeviation(Currency currency)
    {
        if (count[currency] <= 1) return 0.0M; // Need at least two data points for standard deviation

        decimal mean = GetMean(currency);
        decimal variance = (sum_x2[currency] / count[currency]) - (mean * mean);

        // Handle potential negative variance due to floating point inaccuracies
        if (variance < 0) return 0.0M;

        return (decimal)Math.Sqrt((double)variance);
    }

    public bool IsAnomalousTransaction(Transaction txn)
    {
        return Math.Abs(txn.Amount - GetMean(txn.Currency)) > stdDevThresholdMultiplier * GetStandardDeviation(txn.Currency);
    }
}

public class Wallet
{
    public string WalletId { get; private set; }
    private List<Transaction> Transactions { get; set; }
    public Currency BaseCurrency { get; private set; }
    private FraudDetector fraudDetector = new FraudDetector();

    public Wallet(string walletId, Currency baseCurrency)
    {
        WalletId = walletId;
        Transactions = new List<Transaction>();
        BaseCurrency = baseCurrency;
        fraudDetector = new FraudDetector();
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
        return fraudDetector.CheckAndAddEntry(txn);
    }
}

public class CacheItem<T>
{
    public string Key { get; set; }
    public T Value { get; set; }
    public CacheItem(string key, T val)
    {
        Key = key;
        Value = val;
    }
}

public interface ICache<T>
{
    Task<(bool found, CacheItem<T> item)> TryGetAsync(string key);
    Task<bool> PutAsync(string key, T value);
}

public class LruWalletCache : ICache<Wallet>
{
    private ConcurrentDictionary<string, SemaphoreSlim> locks { get; set; }
    private LinkedList<LinkedListNode<CacheItem<Wallet>>> peckingOrder { get; set; }
    private Dictionary<string, LinkedListNode<CacheItem<Wallet>>> cache { get; set; }
    private int Capacity { get; set; }

    public LruWalletCache(int capacity)
    {
        locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        peckingOrder = new LinkedList<LinkedListNode<CacheItem<Wallet>>>();
        cache = new Dictionary<string, LinkedListNode<CacheItem<Wallet>>>();
        Capacity = capacity;
    }

    public async Task<bool> PutAsync(string walletId, Wallet wallet)
    {
        var semaphore = locks.GetOrAdd(walletId, _ => new SemaphoreSlim(1, 1));

        return await ConcurrentActionExecutor.DoSafeConcurrentActionAsync(semaphore, () =>
        {
            LinkedListNode<CacheItem<Wallet>> node;
            if (cache.ContainsKey(walletId))
            {
                node = cache[walletId];
                node.Value.Value = wallet;
                peckingOrder.Remove(node);
            }
            else
            {
                node = new LinkedListNode<CacheItem<Wallet>>(new CacheItem<Wallet>(walletId, wallet));
                cache[walletId] = node;
            }
            peckingOrder.AddFirst(node);

            while (peckingOrder.Count > Capacity)
            {
                var rem = peckingOrder.Last();
                cache.Remove(rem.Value.Key);
                peckingOrder.RemoveLast();
            }
            return Task.FromResult(true);
        });
    }

    public async Task<(bool found, CacheItem<Wallet> item)> TryGetAsync(string walletId)
    {
        CacheItem<Wallet> result = new("", new Wallet("", Currency.USD));
        var semaphore = locks.GetOrAdd(walletId, _ => new SemaphoreSlim(1, 1));

        var found = await ConcurrentActionExecutor.DoSafeConcurrentActionAsync(semaphore, () =>
        {
            if (cache.TryGetValue(walletId, out var node))
            {
                peckingOrder.Remove(node);
                peckingOrder.AddFirst(node);
                result = node.Value;
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        });

        return (found, result);
    }
}

public static class ConcurrentActionExecutor
{
    public static async Task<bool> DoSafeConcurrentActionAsync(SemaphoreSlim semaphore, Func<Task<bool>> action)
    {
        await semaphore.WaitAsync();

        try
        {
            return await action();
        }
        finally
        {
            semaphore.Release();
        }
    }
}

public interface IWalletRepository
    {
        ICache<Wallet> Cache { get; set; }
        Dictionary<string, Wallet> DB_Context { get; set; }
        Task<(bool, Wallet)> TryGetWalletAsync(string walletId);
        Task<IEnumerable<Wallet>> GetWalletsAsync();
        Task<bool> SaveWalletAsync(Wallet wallet);
    }

public class WalletRepository : IWalletRepository
{
    private ConcurrentDictionary<string, SemaphoreSlim> db_locks { get; set; }
    public ICache<Wallet> Cache { get; set; }
    public Dictionary<string, Wallet> DB_Context { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

    public WalletRepository(ICache<Wallet> cache)
    {
        db_locks = new ConcurrentDictionary<string, SemaphoreSlim>();
        Cache = cache;
    }

    public async Task<(bool, Wallet)> TryGetWalletAsync(string walletId)
    {
        var (exists, cacheItem) = await Cache.TryGetAsync(walletId);
        if (exists) return new (true, cacheItem.Value);

        var semaphore = db_locks.GetOrAdd(walletId, _ => new SemaphoreSlim(1, 1));

        Wallet wallet = new Wallet("", Currency.USD);
        var found = await ConcurrentActionExecutor.DoSafeConcurrentActionAsync(semaphore, () =>
        {
            if (!DB_Context.ContainsKey(walletId)) return Task.FromResult(false);
            wallet = DB_Context[walletId];
            return Task.FromResult(true);
        });

        await Cache.PutAsync(walletId, wallet);
        return (found, wallet);
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
    public async Task<IEnumerable<Wallet>> GetWalletsAsync()
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    {
        return DB_Context.Values.AsEnumerable();
    }

    public async Task<bool> SaveWalletAsync(Wallet wallet)
    {
        var semaphore = db_locks.GetOrAdd(wallet.WalletId, _ => new SemaphoreSlim(1, 1));

        await ConcurrentActionExecutor.DoSafeConcurrentActionAsync(semaphore, () =>
        {
            DB_Context[wallet.WalletId] = wallet;
            return Task.FromResult(true);
        });

        await Cache.PutAsync(wallet.WalletId, wallet);
        return true;
    }
}

public interface IWalletService
{
    Task<IEnumerable<Wallet>> GetWalletsAsync(); // pass in filter function?
    Task<(bool, Wallet)> TryGetWallet(string walletId);
    Task<bool> UpsertWallet(Wallet wallet);
}

public class WalletService : IWalletService
{
    private IWalletRepository walletRepo { get; set; }


    public WalletService(IWalletRepository _walletRepo)
    {
        walletRepo = _walletRepo;
    }

    public async Task<IEnumerable<Wallet>> GetWalletsAsync()
    {
        return await walletRepo.GetWalletsAsync();
    }

    public async Task<(bool, Wallet)> TryGetWallet(string walletId)
    {
        return await walletRepo.TryGetWalletAsync(walletId);
    }

    public async Task<bool> UpsertWallet(Wallet wallet)
    {
        return await walletRepo.SaveWalletAsync(wallet);
    }
}

public class TransactionService
{
    private IWalletService walletService { get; set; }
    private IAnalyticsService analyticsService { get; set; }
    private List<string> lockedUserIds { get; set; }
    // private Queue<Transaction> queue = new Queue<Transaction>();

    public TransactionService(IWalletService _walletService, IAnalyticsService _analyticsService)
    {
        walletService = _walletService;
        analyticsService = _analyticsService;
        lockedUserIds = new List<string>();
    }

    public async Task<string> HandleTransaction(Transaction txn, string walletId, int retry = 0)
    {
        if (lockedUserIds.Contains(walletId))
        {
            if (retry == 3) throw new Exception("locked during 4 attempts");
            await HandleTransaction(txn, walletId, retry + 1);
        }
        try
        {
            lockedUserIds.Add(walletId);
            var (found, wallet) = await walletService.TryGetWallet(walletId);
            if (!found) throw new Exception("404 couldn't find wallet");
            var c = wallet.TryApplyTransaction(txn);
            Alert alert;
            var (processed, fdException) = wallet.TryApplyTransaction(txn);
            if (processed)
            {
                alert = analyticsService.UpdateUserAnalytics(txn, wallet.WalletId, wallet.BaseCurrency);
                if (!await walletService.UpsertWallet(wallet)) throw new Exception("failed to save");
            }
            else
            {
#pragma warning disable CS8604 // Possible null reference argument.
                alert = new AlertFactory(fdException).GetAlert(txn, wallet.BaseCurrency);
#pragma warning restore CS8604 // Possible null reference argument.
            }
            return alert.GetMessage();
        }
        catch (Exception)
        {
            throw;
        }
        finally
        {
            lockedUserIds.Remove(walletId);
        }
    }

}

public enum AnalyticType
{
    NetBalance,
    DailyAverageTransactionValue,
    HighestValueTransaction
}

public enum Currency
{
    USD,
    EUR,
    GBP,
    IRC
}

public interface IAnalytic
{
    AnalyticType AnalyticType { get; }
    decimal Get(Currency currency, DateTime? date = null);
    void Update(Transaction txn);
}

public abstract class Analytic<T> : IAnalytic
{
    protected AnalyticType analyticType { get; set; }
    protected abstract Dictionary<Currency, T> CurrentValue { get; set; }
    public abstract AnalyticType AnalyticType { get; }

    public abstract decimal Get(Currency currency, DateTime? date = null);

    public abstract void Update(Transaction txn);
}

public class NetBalanceAnalytic : Analytic<decimal>
{
    protected override Dictionary<Currency, decimal> CurrentValue { get; set; }
    public override AnalyticType AnalyticType { get => AnalyticType.NetBalance; }
    public NetBalanceAnalytic(Transaction txn)
    {
        CurrentValue = new Dictionary<Currency, decimal>();
        Update(txn);    
    }

    public override decimal Get(Currency currency, DateTime? date = null)
    {
        return CurrentValue[currency];
    }

    public override void Update(Transaction txn)
    {
        if (!CurrentValue.ContainsKey(txn.Currency)) CurrentValue[txn.Currency] = 0;
        CurrentValue[txn.Currency] += txn.GetAmount();
    }
}

public class DailyAverageTransactionValueAnalytic : Analytic<SortedDictionary<DateTime, Tuple<decimal, int>>>
{
    protected override Dictionary<Currency, SortedDictionary<DateTime, Tuple<decimal, int>>> CurrentValue { get; set; }
    public override AnalyticType AnalyticType { get => AnalyticType.DailyAverageTransactionValue; }
    public DailyAverageTransactionValueAnalytic(Transaction txn)
    {
        CurrentValue = new Dictionary<Currency, SortedDictionary<DateTime, Tuple<decimal, int>>>();
        Update(txn);    
    }

    public override decimal Get(Currency currency, DateTime? date = null)
    {
        if (!date.HasValue) return 0;
        if (!CurrentValue.ContainsKey(currency)) return 0;
        if (!CurrentValue[currency].ContainsKey(date.Value)) return 0;
        return CurrentValue[currency][date.Value].Item1;
    }

    public decimal GetRange(Currency currency, DateTime? startDate = null, DateTime? endDate = null)
    {
        var items = CurrentValue[currency].Where(x => x.Key >= startDate && x.Key <= endDate);
        var result = 0.0M;
        var totalEntries = items.Sum(x => x.Value.Item2);
        if (items.Count() != 0)
        {
            foreach (var item in items)
            {
                result += item.Value.Item1 * (item.Value.Item2 / totalEntries);
            }
        }
        return result;
    }

    public DateTime? GetHighestVolumeDay(Currency currency)
    {
        if (!CurrentValue.ContainsKey(currency)) return null;
        if (CurrentValue[currency].Count == 0) return null;
        var max = CurrentValue[currency].Values.Max(x => x.Item1 * x.Item2);
        return CurrentValue[currency].Keys.First(date => CurrentValue[currency][date].Item1 * CurrentValue[currency][date].Item2 == max);
    }

    public DateTime? DayWithMostTransactions(Currency currency)
    {
        if (!CurrentValue.ContainsKey(currency)) return null;
        if (CurrentValue.Count == 0) return null;
        var max = CurrentValue[currency].Values.Max(x => x.Item2);
        return CurrentValue[currency].Keys.First(key => CurrentValue[currency][key].Item2 == max);
    }

    public override void Update(Transaction txn)
    {
        var date = txn.Timestamp.Date;
        if (!CurrentValue.ContainsKey(txn.Currency))
        {
            CurrentValue[txn.Currency] = new SortedDictionary<DateTime, Tuple<decimal, int>>();
        }
        if (!CurrentValue[txn.Currency].ContainsKey(date))
        {
            CurrentValue[txn.Currency][date] = new Tuple<decimal, int>(0.0M, 0);
        }
        var tuple = CurrentValue[txn.Currency][date];
        CurrentValue[txn.Currency][date] = new Tuple<decimal, int>((tuple.Item1 * tuple.Item2 + txn.GetAmount()) / tuple.Item2 + 1, tuple.Item2 + 1);
    }
}

public class HighestValueTransactionAnalytic : Analytic<decimal>
{
    protected override Dictionary<Currency, decimal> CurrentValue { get; set; }
    public override AnalyticType AnalyticType { get => AnalyticType.HighestValueTransaction; }

    public HighestValueTransactionAnalytic(Transaction txn)
    {
        CurrentValue = new Dictionary<Currency, decimal>();
        Update(txn);
    }

    public override decimal Get(Currency currency, DateTime? date = null)
    {
        return CurrentValue[currency];
    }

    public override void Update(Transaction txn)
    {
        CurrentValue[txn.Currency] = Math.Max(txn.GetAmount(), CurrentValue[txn.Currency]);
    }
}

public interface IRateService
{
    decimal GetRate(Currency baseC, Currency origC);
}

public class RateService : IRateService
{
    private Dictionary<Currency, Dictionary<Currency, decimal>> rates = new Dictionary<Currency, Dictionary<Currency, decimal>>();

    public decimal GetRate(Currency b, Currency o)
    {
        if (!rates.ContainsKey(b)) rates[b] = new Dictionary<Currency, decimal>();
        if (!rates[b].ContainsKey(o)) rates[b][o] = new Random().Next(0, 2);
        return rates[b][o];
    }
}

public interface IAnalyticsService
    {
        Alert UpdateUserAnalytics(Transaction txn, string walletId, Currency baseCurrency);
        decimal GetAnalytic(AnalyticType type, string walletId, Currency currency, DateTime? date = null);
    }
public class AnalyticsService : IAnalyticsService
{
    private IRateService rateService { get; set; }
    private Dictionary<string, List<IAnalytic>> userAnalytics { get; set; }

    //UserAnalyticsDTO
    public AnalyticsService(IRateService _rateService)
    {
        rateService = _rateService;
        userAnalytics = new Dictionary<string, List<IAnalytic>>();
    }

    public decimal GetAnalytic(AnalyticType type, string walletId, Currency currency, DateTime? date = null)
    {
        if (!userAnalytics.ContainsKey(walletId)) throw new Exception("404 can't find wallet");

        return userAnalytics[walletId].First(x => x.AnalyticType == type).Get(currency);
    }

    public Alert UpdateUserAnalytics(Transaction txn, string walletId, Currency baseCurrency)
    {
        if (!userAnalytics.ContainsKey(walletId))
        {
            userAnalytics.Add(walletId, new List<IAnalytic>() { new NetBalanceAnalytic(txn), new DailyAverageTransactionValueAnalytic(txn), new HighestValueTransactionAnalytic(txn) });
        }

        userAnalytics[walletId].ForEach(x => x.Update(txn));

        return new AlertFactory(userAnalytics[walletId], rateService).GetAlert(txn, baseCurrency);
    }
}

public abstract class Alert
{
    public abstract string GetMessage();
}

public class ValueExceedsAlert : Alert {

    private decimal threshold { get; set; }
    private decimal transactionValue { get; set; }
    private decimal exchangeRate { get; set; }
    private Currency baseCurrency { get; set; }
    private Currency transactionCurrency { get; set; }
    public ValueExceedsAlert(decimal _threshold, Transaction txn, decimal exRate, Currency _baseCurrency)
    {
        threshold = _threshold;
        transactionValue = txn.GetAmount();
        baseCurrency = _baseCurrency;
        transactionCurrency = txn.Currency;
        exchangeRate = exRate;
    }
    
    public bool IsTriggered()
    {
        return transactionValue * exchangeRate >= threshold;
    }
    
    public override string GetMessage()
    {
        return $"I'm a Value Exceeds Alert! The value of {transactionValue} in {transactionCurrency} currency "
        + $"exceeds the threshold of {threshold} in {baseCurrency} currency by {transactionValue * exchangeRate - threshold} {baseCurrency}.";
    }
}

public class BalanceBelowAlert : Alert {

    private decimal threshold { get; set; }
    private decimal value { get; set; }
    private decimal exchangeRate { get; set; }
    private Currency tranCurrency { get; set; }
    private Currency baseCurrency { get; set; }

    public BalanceBelowAlert(decimal _threshold, NetBalanceAnalytic analytic, decimal exRate, Currency _baseCurrency, Currency _tranCurrency)
    {
        threshold = _threshold;
        tranCurrency = _tranCurrency;
        exchangeRate = exRate;
        baseCurrency = _baseCurrency;
        value = analytic.Get(tranCurrency);
    }

    public bool IsTriggered()
    {
        return value * exchangeRate < threshold;
    }

    public override string GetMessage()
    {
        return $"I'm a Balance Below Alert for the {tranCurrency} currency! The value of {value} {tranCurrency} "
        + $"is below the minimum threshold of {threshold} {baseCurrency} by {threshold - exchangeRate * value} {baseCurrency}.";
    }
}

public class TransactionCanceledAlert : Alert
{
    private FraudDetectionException exception { get; set; }
    private Transaction transaction { get; set; }
    private Currency baseCurrency { get; set; }
    public TransactionCanceledAlert(FraudDetectionException _exception, Transaction txn, Currency _baseCurrency)
    {
        exception = _exception;
        transaction = txn;
        baseCurrency = _baseCurrency;
    }

    public override string GetMessage()
    {
        return "This transaction was canceled for the following reason: " + exception.Exception.Message
        + $"\r\n\t Transaction details: "
        + $"\r\n\t Date: {transaction.Timestamp.ToShortDateString()}"
        + $"\r\n\t Timestamp: {transaction.Timestamp.ToLongTimeString()}"
        + $"\r\n\t Currency: {transaction.Currency}"
        + $"\r\n\t TrannyType: {transaction.Type}"
        + $"\r\n\t Amount: {transaction.Amount}";
    }
}

public class CompositeAlert : Alert
{
    private List<Alert> sub_alerts = new List<Alert>();

    public CompositeAlert()
    {
        sub_alerts = new List<Alert>();
    }

    public override string GetMessage()
    {
        return string.Join("\r\n", sub_alerts.Select(x => x.GetMessage()));
    }

    public void AddAlert(Alert alert)
    {
        sub_alerts.Add(alert);
    }

    public bool Any()
    {
        return sub_alerts.Any();
    }
}

public class AllGoodAlert : Alert
{
    public AllGoodAlert() { }

    public override string GetMessage()
    {
        return "I'm a happy Alert. Success.";
    }
}

public enum AlertType {
    BalanceBelow,
    ValueExceeds
}

public class AlertFactory {
    private List<IAnalytic>? analytics { get; set; }
    private IRateService? rateService { get; set; }
    private FraudDetectionException? fraudDetectionException;

    public AlertFactory(List<IAnalytic> _analytics, IRateService _rateService)
    {
        analytics = _analytics;
        rateService = _rateService;
    }

    public AlertFactory(FraudDetectionException _exception)
    {
        fraudDetectionException = _exception;
    }

    public Alert GetAlert(Transaction txn, Currency baseCurrency)
    {
        if (fraudDetectionException != null)
        {
            return new TransactionCanceledAlert(fraudDetectionException, txn, baseCurrency);
        }
        else if (rateService != null && analytics != null)
        {
            var compositeAlert = new CompositeAlert();
            var exRate = rateService.GetRate(baseCurrency, txn.Currency);
            foreach (var alert in Enum.GetValues(typeof(AlertType)))
            {
                switch (alert)
                {
                    case AlertType.BalanceBelow:
                        var bbA = new BalanceBelowAlert(1000, (NetBalanceAnalytic)analytics.First(x => x.AnalyticType == AnalyticType.NetBalance), exRate, baseCurrency, txn.Currency);
                        if (bbA.IsTriggered()) compositeAlert.AddAlert(bbA);
                        break;
                    case AlertType.ValueExceeds:
                        var veA = new ValueExceedsAlert(1000, txn, exRate, baseCurrency);
                        if (veA.IsTriggered()) compositeAlert.AddAlert(veA);
                        break;
                }
            }
            return compositeAlert.Any() ? compositeAlert : new AllGoodAlert();
        }
        else return new AllGoodAlert();
    }
}
