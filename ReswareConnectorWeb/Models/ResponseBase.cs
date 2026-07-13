using ActionEventServiceNS;
using ReswareConnectorWeb.Enums;

namespace ReswareConnectorWeb.Models
{
    public class ResponseBase
    {
        public string FileNumber { get; set; } = "";
        public long? TransactionReferenceNumber { get; set; }
        public string? Message { get; set; }
        public bool AwaitDeferredResponse { get; set; }
        public TransactionTypeEnum? TransactionTypeName { get; set; }
        public int? TransactionType { get ; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public object? OriginalResponse { get; set; }
    }
}
