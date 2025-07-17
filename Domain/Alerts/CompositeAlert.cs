namespace walle.Domain{
    public class CompositeAlert : Alert
    {
        private List<Alert> sub_alerts = new List<Alert>();

        public CompositeAlert()
        {
            sub_alerts = new List<Alert>();
        }

        public override string GetMessage()
        {
            return string.Join("\r\n", sub_alerts.Select(x => x.GetMessage()));
        }

        public void AddAlert(Alert alert)
        {
            sub_alerts.Add(alert);
        }

        public bool Any()
        {
            return sub_alerts.Any();
        }
    }
}