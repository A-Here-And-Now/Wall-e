namespace walle.Domain
{    
    public class DailyAverageTransactionValueAnalytic : Analytic<SortedDictionary<DateTime, Tuple<decimal, int>>>
    {
        protected override Dictionary<Currency, SortedDictionary<DateTime, Tuple<decimal, int>>> CurrentValue { get; set; }
        public override AnalyticType AnalyticType { get => AnalyticType.DailyAverageTransactionValue; }
        public DailyAverageTransactionValueAnalytic(Transaction txn)
        {
            CurrentValue = new Dictionary<Currency, SortedDictionary<DateTime, Tuple<decimal, int>>>();
            Update(txn);    
        }

        public override decimal Get(Currency currency, DateTime? date = null)
        {
            if (!date.HasValue) return 0;
            if (!CurrentValue.ContainsKey(currency)) return 0;
            if (!CurrentValue[currency].ContainsKey(date.Value)) return 0;
            return CurrentValue[currency][date.Value].Item1;
        }

        public decimal GetRange(Currency currency, DateTime? startDate = null, DateTime? endDate = null)
        {
            var items = CurrentValue[currency].Where(x => x.Key >= startDate && x.Key <= endDate);
            var result = 0.0M;
            var totalEntries = items.Sum(x => x.Value.Item2);
            if (items.Count() != 0)
            {
                foreach (var item in items)
                {
                    result += item.Value.Item1 * (item.Value.Item2 / totalEntries);
                }
            }
            return result;
        }

        public DateTime? GetHighestVolumeDay(Currency currency)
        {
            if (!CurrentValue.ContainsKey(currency)) return null;
            if (CurrentValue[currency].Count == 0) return null;
            var max = CurrentValue[currency].Values.Max(x => x.Item1 * x.Item2);
            return CurrentValue[currency].Keys.First(date => CurrentValue[currency][date].Item1 * CurrentValue[currency][date].Item2 == max);
        }

        public DateTime? DayWithMostTransactions(Currency currency)
        {
            if (!CurrentValue.ContainsKey(currency)) return null;
            if (CurrentValue.Count == 0) return null;
            var max = CurrentValue[currency].Values.Max(x => x.Item2);
            return CurrentValue[currency].Keys.First(key => CurrentValue[currency][key].Item2 == max);
        }

        public override void Update(Transaction txn)
        {
            var date = txn.Timestamp.Date;
            if (!CurrentValue.ContainsKey(txn.Currency))
            {
                CurrentValue[txn.Currency] = new SortedDictionary<DateTime, Tuple<decimal, int>>();
            }
            if (!CurrentValue[txn.Currency].ContainsKey(date))
            {
                CurrentValue[txn.Currency][date] = new Tuple<decimal, int>(0.0M, 0);
            }
            var tuple = CurrentValue[txn.Currency][date];
            CurrentValue[txn.Currency][date] = new Tuple<decimal, int>((tuple.Item1 * tuple.Item2 + txn.GetAmount()) / tuple.Item2 + 1, tuple.Item2 + 1);
        }
    }
}