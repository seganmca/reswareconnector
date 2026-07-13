namespace ReswareConnectorWeb.Data.Entities
{
    public class Transaction
    {
        public long TransactionId { get; set; }
        public byte TransactionTypeId { get; set; }
        public required string FileNumber { get; set; }
        public required string DataPath { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool Processed { get; set; } = false;

        public virtual ICollection<TransactionItem> Items { get; set; } = new List<TransactionItem>();
    }
}
