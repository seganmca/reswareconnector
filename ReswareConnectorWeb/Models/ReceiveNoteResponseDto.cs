using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReswareConnectorWeb.Models
{
    public class ReceiveNoteResponseDto : ResponseBase
    {
        public int ResponseCode { get { return (int)ResponseCodeName; } }
        public ReceiveNoteResponseCode ResponseCodeName { get; set; }
        public ReceiveNoteResponseDto()
        {
            TransactionTypeName = TransactionTypeEnum.NoteDocument;
            TransactionType = (int)TransactionTypeEnum.NoteDocument;
        }
    }
}
