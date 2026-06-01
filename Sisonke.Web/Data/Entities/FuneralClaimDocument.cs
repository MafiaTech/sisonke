using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class FuneralClaimDocument
{
    public Guid Id { get; set; }

    public Guid FuneralClaimId { get; set; }
    public FuneralClaim FuneralClaim { get; set; } = default!;

    public ClaimDocumentType DocumentType { get; set; }

    [Required]
    [MaxLength(300)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string StoredFilePath { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long FileSizeBytes { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
