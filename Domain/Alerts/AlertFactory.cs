namespace walle.Domain{
    public class AlertFactory {
        private List<IAnalytic>? analytics { get; set; }
        private IRateService? rateService { get; set; }
        private FraudDetectionException? fraudDetectionException;

        public AlertFactory(List<IAnalytic> _analytics, IRateService _rateService)
        {
            analytics = _analytics;
            rateService = _rateService;
        }

        public AlertFactory(FraudDetectionException _exception)
        {
            fraudDetectionException = _exception;
        }

        public Alert GetAlert(Transaction txn, Currency baseCurrency)
        {
            if (fraudDetectionException != null)
            {
                return new TransactionCanceledAlert(fraudDetectionException, txn, baseCurrency);
            }
            else if (rateService != null && analytics != null)
            {
                var compositeAlert = new CompositeAlert();
                var exRate = rateService.GetRate(baseCurrency, txn.Currency);
                foreach (var alert in Enum.GetValues(typeof(AlertType)))
                {
                    switch (alert)
                    {
                        case AlertType.BalanceBelow:
                            var bbA = new BalanceBelowAlert(1000, (NetBalanceAnalytic)analytics.First(x => x.AnalyticType == AnalyticType.NetBalance), exRate, baseCurrency, txn.Currency);
                            if (bbA.IsTriggered()) compositeAlert.AddAlert(bbA);
                            break;
                        case AlertType.ValueExceeds:
                            var veA = new ValueExceedsAlert(1000, txn, exRate, baseCurrency);
                            if (veA.IsTriggered()) compositeAlert.AddAlert(veA);
                            break;
                    }
                }
                return compositeAlert.Any() ? compositeAlert : new AllGoodAlert();
            }
            else return new AllGoodAlert();
        }
    }
}