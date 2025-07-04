public enum TrannyType{
    Deposit,
    Withdrawal
}

public class Transaction {
    public string TransactionId { get; }
    public DateTime Timestamp { get; }
    public decimal Amount { get; }
    public TrannyType Type { get; } // "deposit" or "withdrawal"
    public Transaction(string id, DateTime timestamp, decimal amount, TrannyType type) {
        
    }
}

public class Wallet {
    private string UserId { get; }
    private List<Transaction> Transactions { get; }

    public void ApplyTransaction(Transaction txn) {
        Transactions.Add(txn);
    }
    
    public double GetBalance() {
        return Transactions.Select(x => x.Type == TrannyType.Deposit ? x.Amount : -x.Amount).Sum();
    }
}

public interface IWalletService {
    List<Wallet> GetWallets(); // pass in filter function?
    Wallet GetWallet(int userId);
    bool SaveWallet(Wallet wallet);
}

public class WalletService : IWalletService {
    private List<Wallet> wallets {get;set;}

    public void WalletService() {
        
    }

    public List<Wallet> GetWallets(){
        // get all from db
    }

    public Wallet GetWallet(int userId){
        // get by userId
    }

    public bool SaveWallet(Wallet wallet){
        if (Wallet.IsLocked) return null;
        // save to db
    }
}

public enum Result {
    Retry,
    Locked,
    Success,
    Failed
}

public class TransactionService {
    private IWalletService walletService {get;set;}
    private Dictionary<int, bool> vault {get;set;}

    public TransactionService(IWalletService _walletService){
        walletService = _walletService;
        vault = new Dictionary<int, bool>();
    }  

    public async Task<Result> HandleTransaction(Transaction txn, int userId){
        if (vault[userId]) return Result.Locked;
        try {
            vault[userId] = true;
            var wallet = walletService.GetWallet(userId);
            newWallet.ApplyTransaction(txn);
            walletService.SaveWallet(newWallet);
            vault[wallet.userId] = false;
        } catch (Exception e){
            throw;
        }
    }
    
}

// var wallet = new Wallet("user-123");
// wallet.ApplyTransaction(new Transaction(...));

// var service = new WalletAnalyticsService(wallets);
// var lowBalanceUsers = service.GetUsersBelowBalance(100);

// var alerts = service.CheckAlerts();
