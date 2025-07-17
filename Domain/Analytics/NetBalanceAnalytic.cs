namespace walle.Domain
{
    public class NetBalanceAnalytic : Analytic<decimal>
    {
        protected override Dictionary<Currency, decimal> CurrentValue { get; set; }
        public override AnalyticType AnalyticType { get => AnalyticType.NetBalance; }
        public NetBalanceAnalytic(Transaction txn)
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
            if (!CurrentValue.ContainsKey(txn.Currency)) CurrentValue[txn.Currency] = 0;
            CurrentValue[txn.Currency] += txn.GetAmount();
        }
    }
}