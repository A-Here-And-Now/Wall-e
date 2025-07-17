using walle.Domain;

namespace walle.ServiceLayer
{
    public interface IWalletTransactionFraudDetector
    {
        (bool, FraudDetectionException?) CheckAndAddEntry(Transaction txn);
    }
}