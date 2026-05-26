using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class Payment
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid MemberContributionId { get; set; }
    public MemberContribution MemberContribution { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Today;

    [MaxLength(100)]
    public string? Reference { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.EFT;

    [MaxLength(100)]
    public string? CapturedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
