using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using SearchDataServiceNS;
using System.DirectoryServices.Protocols;

namespace ReswareConnectorWeb.Models
{

    public class ReceiveSearchDataResponseDto : ResponseBase
    {
        public int ResponseCode { get { return (int)ResponseCodeName; } }
        public ReceiveSearchDataResponseCode ResponseCodeName { get; set; }

        public ReceiveSearchDataResponseDto() 
        {
            TransactionTypeName = TransactionTypeEnum.SearchData;
            TransactionType = (int)TransactionTypeEnum.SearchData;
        }
    }
}
