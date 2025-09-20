namespace walle.ServiceLayer{
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
}