using walle.Domain;
using walle.ServiceLayer;

namespace walle.DataAccessLayer
{
    public interface IWalletRepository
    {
        ICache<Wallet> Cache { get; set; }
        Dictionary<string, Wallet> DB_Context { get; set; }
        Task<(bool, Wallet)> TryGetWalletAsync(string walletId);
        Task<IEnumerable<Wallet>> GetWalletsAsync();
        Task<bool> SaveWalletAsync(Wallet wallet);
    }
}