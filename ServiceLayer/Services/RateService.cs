namespace walle.ServiceLayer{
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
}