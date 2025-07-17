namespace walle.Domain{
    public class ValueExceedsAlert : Alert {

        private decimal threshold { get; set; }
        private decimal transactionValue { get; set; }
        private decimal exchangeRate { get; set; }
        private Currency baseCurrency { get; set; }
        private Currency transactionCurrency { get; set; }
        public ValueExceedsAlert(decimal _threshold, Transaction txn, decimal exRate, Currency _baseCurrency)
        {
            threshold = _threshold;
            transactionValue = txn.GetAmount();
            baseCurrency = _baseCurrency;
            transactionCurrency = txn.Currency;
            exchangeRate = exRate;
        }
        
        public bool IsTriggered()
        {
            return transactionValue * exchangeRate >= threshold;
        }
        
        public override string GetMessage()
        {
            return $"I'm a Value Exceeds Alert! The value of {transactionValue} in {transactionCurrency} currency "
            + $"exceeds the threshold of {threshold} in {baseCurrency} currency by {transactionValue * exchangeRate - threshold} {baseCurrency}.";
        }
    }
}