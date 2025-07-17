namespace walle.ServiceLayer{
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
}