using walle.Domain.Enums;
namespace walle.Domain
{
    public class FraudDetectionException
    {
        public FraudDetectionType Type { get; set; }
        public Exception Exception { get; set; }

        public FraudDetectionException(FraudDetectionType t, Exception e)
        {
            Type = t;
            Exception = e;
        }
    }
}