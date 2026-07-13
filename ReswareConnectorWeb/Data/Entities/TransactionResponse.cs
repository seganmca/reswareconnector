namespace ReswareConnectorWeb.Data.Entities
{
    public class TransactionResponse
    {
        public long TransactionResponseId { get; set; }
        public long TransactionItemId { get; set; }
        public DateTime ReceivedTime { get; set; }
        public int ResponseCode { get; set; }
        public string? ResponseMessage { get; set; }
        public virtual TransactionItem? TransactionItem { get; set; }
    }
}
