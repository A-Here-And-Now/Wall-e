namespace walle.Domain
{
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
}