using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;

namespace ReswareConnectorWeb.Models
{
    public class ReceiveActionEventResponseDto : ResponseBase
    {
        public int ResponseCode { get { return (int)ResponseCodeName; } }
        public ReceiveActionEventResponseCode ResponseCodeName { get; set; }

        public ReceiveActionEventResponseDto()
        {
            TransactionTypeName = TransactionTypeEnum.ActionEvent;
            TransactionType = (int)TransactionTypeEnum.ActionEvent;
        }
    }
}
