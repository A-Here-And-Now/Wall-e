namespace walle.ServiceLayer {
    public interface IWalletService
    {
        Task<IEnumerable<Wallet>> GetWalletsAsync(); // pass in filter function?
        Task<(bool, Wallet)> TryGetWallet(string walletId);
        Task<bool> UpsertWallet(Wallet wallet);
    }
}