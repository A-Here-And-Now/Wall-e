using walle.Domain.Enums;

namespace walle.Domain
{
    public class Transaction
    {
        public string TransactionId { get; }
        public DateTime Timestamp { get; }
        public decimal Amount { get; }
        public TransactionType Type { get; }
        public Currency Currency { get; }
        public Transaction(string id, DateTime timestamp, decimal amount, TransactionType type, Currency currency)
        {
            TransactionId = id;
            Timestamp = timestamp;
            Amount = amount;
            Type = type;
            Currency = currency;
        }

        public decimal GetAmount()
        {
            return Type == TransactionType.Deposit ? Amount : -Amount;
        }
    }
}