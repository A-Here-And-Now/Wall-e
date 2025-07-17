namespace walle.ServiceLayer{
    public interface IAnalyticsService
    {
        Alert UpdateUserAnalytics(Transaction txn, string walletId, Currency baseCurrency);
        decimal GetAnalytic(AnalyticType type, string walletId, Currency currency, DateTime? date = null);
    }
}