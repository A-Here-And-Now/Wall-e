using walle.Domain;

namespace walle.ServiceLayer
{
    public interface ICache<T>
    {
        Task<(bool found, CacheItem<T> item)> TryGetAsync(string key);
        Task<bool> PutAsync(string key, T value);
    }
}