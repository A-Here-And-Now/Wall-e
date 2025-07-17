namespace walle.Domain    
{    
    public class BalanceBelowAlert : Alert {

        private decimal threshold { get; set; }
        private decimal value { get; set; }
        private decimal exchangeRate { get; set; }
        private Currency tranCurrency { get; set; }
        private Currency baseCurrency { get; set; }

        public BalanceBelowAlert(decimal _threshold, NetBalanceAnalytic analytic, decimal exRate, Currency _baseCurrency, Currency _tranCurrency)
        {
            threshold = _threshold;
            tranCurrency = _tranCurrency;
            exchangeRate = exRate;
            baseCurrency = _baseCurrency;
            value = analytic.Get(tranCurrency);
        }

        public bool IsTriggered()
        {
            return value * exchangeRate < threshold;
        }

        public override string GetMessage()
        {
            return $"I'm a Balance Below Alert for the {tranCurrency} currency! The value of {value} {tranCurrency} "
            + $"is below the minimum threshold of {threshold} {baseCurrency} by {threshold - exchangeRate * value} {baseCurrency}.";
        }
    }
}