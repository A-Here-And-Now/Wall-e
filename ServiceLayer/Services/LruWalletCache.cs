using System.Collections.Concurrent;
using walle.Domain;

namespace walle.ServiceLayer
{
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
}