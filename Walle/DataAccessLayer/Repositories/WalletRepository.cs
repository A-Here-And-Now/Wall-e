using System.Collections.Concurrent;
using walle.Domain;
using walle.ServiceLayer;
using walle.Helpers;

namespace walle.DataAccessLayer
{
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
            if (exists) return new(true, cacheItem.Value);

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

}