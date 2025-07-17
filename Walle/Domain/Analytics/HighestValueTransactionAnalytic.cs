namespace walle.Domain{
    public class HighestValueTransactionAnalytic : Analytic<decimal>
    {
        protected override Dictionary<Currency, decimal> CurrentValue { get; set; }
        public override AnalyticType AnalyticType { get => AnalyticType.HighestValueTransaction; }

        public HighestValueTransactionAnalytic(Transaction txn)
        {
            CurrentValue = new Dictionary<Currency, decimal>();
            Update(txn);
        }

        public override decimal Get(Currency currency, DateTime? date = null)
        {
            return CurrentValue[currency];
        }

        public override void Update(Transaction txn)
        {
            CurrentValue[txn.Currency] = Math.Max(txn.GetAmount(), CurrentValue[txn.Currency]);
        }
    }
}