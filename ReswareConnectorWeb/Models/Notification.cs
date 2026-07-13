using ReceiveNoteServiceNS;
using ReswareConnectorWeb.Enums;
using System.ComponentModel.DataAnnotations;

namespace ReswareConnectorWeb.Models
{
    public class Notification
    {
        public NotificationType NotificationType { get; set; }
        public string? Message { get; set; } = string.Empty;
    }

    public enum NotificationType
    {
        Generic = 1
    }
}
