namespace walle.ServiceLayer{
    public class AnalyticsService : IAnalyticsService
    {
        private IRateService rateService { get; set; }
        private Dictionary<string, List<IAnalytic>> userAnalytics { get; set; }

        //UserAnalyticsDTO
        public AnalyticsService(IRateService _rateService)
        {
            rateService = _rateService;
            userAnalytics = new Dictionary<string, List<IAnalytic>>();
        }

        public decimal GetAnalytic(AnalyticType type, string walletId, Currency currency, DateTime? date = null)
        {
            if (!userAnalytics.ContainsKey(walletId)) throw new Exception("404 can't find wallet");

            return userAnalytics[walletId].First(x => x.AnalyticType == type).Get(currency);
        }

        public Alert UpdateUserAnalytics(Transaction txn, string walletId, Currency baseCurrency)
        {
            if (!userAnalytics.ContainsKey(walletId))
            {
                userAnalytics.Add(walletId, new List<IAnalytic>() { new NetBalanceAnalytic(txn), new DailyAverageTransactionValueAnalytic(txn), new HighestValueTransactionAnalytic(txn) });
            }

            userAnalytics[walletId].ForEach(x => x.Update(txn));

            return new AlertFactory(userAnalytics[walletId], rateService).GetAlert(txn, baseCurrency);
        }
    }
}