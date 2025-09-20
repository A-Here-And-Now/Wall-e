namespace walle.Domain {
    public abstract class Analytic<T> : IAnalytic
    {
        protected AnalyticType analyticType { get; set; }
        protected abstract Dictionary<Currency, T> CurrentValue { get; set; }
        public abstract AnalyticType AnalyticType { get; }

        public abstract decimal Get(Currency currency, DateTime? date = null);

        public abstract void Update(Transaction txn);
    }
}