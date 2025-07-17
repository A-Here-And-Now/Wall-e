public interface IAnalytic
{
    AnalyticType AnalyticType { get; }
    decimal Get(Currency currency, DateTime? date = null);
    void Update(Transaction txn);
}