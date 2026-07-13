using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;

namespace ReswareConnectorWeb.Models
{
    public class OrderResponseDto : ResponseBase
    {
        public string? ErrorMessage { get; set; }
        public List<ValidationError>? ValidationErrors { get; set; } = null;
        public ReceiveNoteResponseDto? NoteDataResponse { get; set; } = null;
        public ReceiveSearchDataResponseDto? SearchDataResponse { get; set; } = null;
        public ReceiveActionEventResponseDto? ActionEventResponse { get; set; } = null;
        public OrderResponseDto()
        {
            TransactionTypeName = TransactionTypeEnum.Order;
            TransactionType = (int)TransactionTypeEnum.Order;
        }
    }
}
