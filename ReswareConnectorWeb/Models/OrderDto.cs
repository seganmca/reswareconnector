using ActionEventServiceNS;
using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using SearchDataServiceNS;

namespace ReswareConnectorWeb.Models
{
    public class OrderDto
    {
        public ReceiveNoteRequestDto? NoteData { get; set; }
        public ReceiveSearchDataDataDto? SearchData { get; set; }
        public ReceiveActionEventData? ActionEventData { get; set; }
        public bool SendNoteData { get; set; }
        public bool SendSearchData { get; set; }
        public bool SendActionEventData { get; set; }
        public long? FileID { get; set; }
    }
}
