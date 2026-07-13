namespace ReswareConnectorWeb.Data.Entities
{
    public class TransactionItem
    {
        public long TransactionItemId { get; set; }
        public long TransactionId { get; set; }
        public byte TransactionTypeId { get; set; }
        public bool Processed { get; set; } = false;
        public byte? RetryCount { get; set; }
        public bool? ResponseSent { get; set; }
        public DateTime? LastUpdatedTime { get; set; }
        public virtual Transaction Transaction { get; set; }
        public virtual ICollection<TransactionResponse>? Responses { get; set; }
    }
}
