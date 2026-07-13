using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReswareConnectorWeb.Models
{
    public class ReswareTransactionUpdateMessage : ResponseBase
    {
        public int? ResponseCode { get; set; }
        public string? ResponseCodeName {  get; set; }
    }
}
