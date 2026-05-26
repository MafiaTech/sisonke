using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MemberFine
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public Guid FineTypeId { get; set; }
    public FineType FineType { get; set; } = default!;

    public decimal Amount { get; set; }

    [Required]
    [MaxLength(250)]
    public string Reason { get; set; } = string.Empty;

    public DateTime FineDate { get; set; } = DateTime.Today;

    public FineStatus Status { get; set; } = FineStatus.Unpaid;

    public DateTime? PaidDate { get; set; }

    [MaxLength(100)]
    public string? CapturedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
