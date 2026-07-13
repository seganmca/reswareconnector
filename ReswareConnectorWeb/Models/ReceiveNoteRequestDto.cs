using ReceiveNoteServiceNS;
using System.ComponentModel.DataAnnotations;

namespace ReswareConnectorWeb.Models
{
    public class ReceiveNoteRequestDto
    {
        public int? CoordinatorTypeID { get; set; }

        public int CurativeID { get; set; }

        public ReceiveCurativeTypeEnum CurativeType { get; set; }

        public ReceiveNoteDocumentDto[]? Documents { get; set; }

        [StringLength(100)]
        public string? FileNumber { get; set; }

        public string? NoteBody { get; set; }

        [StringLength(500)]
        public string? NoteSubject { get; set; }

        public int ToCoordinatorID { get; set; }
    }

    public class ReceiveNoteDocumentDto
    {
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int DocumentTypeID { get; set; }

        [StringLength(500)]
        public string? SourceFile { get; set; }
        [StringLength(255)]
        public string? FileName { get; set; }

        public bool? InternalOnly { get; set; }
    }
}
