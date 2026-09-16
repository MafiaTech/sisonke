using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class PaymentProofDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;
    public Guid ContributionPaymentSubmissionId { get; set; }
    public ContributionPaymentSubmission ContributionPaymentSubmission { get; set; } = default!;
    [MaxLength(300)] public string OriginalFileName { get; set; } = string.Empty;
    [MaxLength(100)] public string StoredFileName { get; set; } = string.Empty;
    [MaxLength(100)] public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    [MaxLength(450)] public string UploadedByUserId { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
