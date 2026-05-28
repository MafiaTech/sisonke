using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class ConstitutionDocument
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public bool IsUploadedDocument { get; set; }

    [MaxLength(300)]
    public string? OriginalFileName { get; set; }

    [MaxLength(500)]
    public string? StoredFilePath { get; set; }

    [MaxLength(100)]
    public string? ContentType { get; set; }

    public long? FileSizeBytes { get; set; }

    public int VersionNumber { get; set; } = 1;

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }
}
