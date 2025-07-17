namespace walle.Domain{
    public class TransactionCanceledAlert : Alert
    {
        private FraudDetectionException exception { get; set; }
        private Transaction transaction { get; set; }
        private Currency baseCurrency { get; set; }
        public TransactionCanceledAlert(FraudDetectionException _exception, Transaction txn, Currency _baseCurrency)
        {
            exception = _exception;
            transaction = txn;
            baseCurrency = _baseCurrency;
        }

        public override string GetMessage()
        {
            return "This transaction was canceled for the following reason: " + exception.Exception.Message
            + $"\r\n\t Transaction details: "
            + $"\r\n\t Date: {transaction.Timestamp.ToShortDateString()}"
            + $"\r\n\t Timestamp: {transaction.Timestamp.ToLongTimeString()}"
            + $"\r\n\t Currency: {transaction.Currency}"
            + $"\r\n\t TrannyType: {transaction.Type}"
            + $"\r\n\t Amount: {transaction.Amount}";
        }
    }
}