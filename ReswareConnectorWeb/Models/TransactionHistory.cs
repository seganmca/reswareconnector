using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.Models
{
    public class TransactionHistory
    {
        public string FileNumber { get; set; }

        public List<TransactionDto> Transactions { get; set; }
    }

    public class TransactionDto
    {
        public long TransactionId { get; set; }
        public TransactionTypeEnum TransactionTypeName { get; set; }
        public int TransactionType { get; set; }
        public DateTime ReceivedTime { get; set; }
        public bool IsCompleted { get; set; }
        public List<ReswareRequest> Requests { get; set; }
    }
    
    public class ReswareRequest
    {
        public TransactionTypeEnum TransactionTypeName { get; set; }
        public int TransactionType { get; set; }
        public object Data { get; set; }
        public bool IsCompleted { get; set; }
        public List<ReswareResponse> ResponseHistory { get; set; }
    }

    public class ReswareResponse
    {
        public string Message { get; set; }
        public int ResponseCode { get; set; }
        public DateTime ReceivedTime { get; set; }
    }
}
