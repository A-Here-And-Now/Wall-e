namespace walle.Domain{
    public class AllGoodAlert : Alert
    {
        public AllGoodAlert() { }

        public override string GetMessage()
        {
            return "I'm a happy Alert. Success.";
        }
    }
}