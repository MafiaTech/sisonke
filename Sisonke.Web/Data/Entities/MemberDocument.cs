using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class MemberDocument
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    [Required]
    [MaxLength(80)]
    public string DocumentType { get; set; } = string.Empty;

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
