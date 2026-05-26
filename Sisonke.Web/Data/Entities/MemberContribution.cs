using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MemberContribution
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid ContributionCycleId { get; set; }
    public ContributionCycle ContributionCycle { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public decimal ExpectedAmount { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal OutstandingAmount { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Unpaid;

    public DateTime? FullyPaidDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
