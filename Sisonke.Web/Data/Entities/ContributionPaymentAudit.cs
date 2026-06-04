namespace Sisonke.Web.Data.Entities;

public class ContributionPaymentAudit
{
    public Guid Id { get; set; }

    public Guid ContributionPaymentId { get; set; }
    public MemberContribution? ContributionPayment { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    public string Action { get; set; } = string.Empty;

    public decimal? PreviousAmountPaid { get; set; }

    public decimal? NewAmountPaid { get; set; }

    public string? PreviousStatus { get; set; }

    public string? NewStatus { get; set; }

    public string? PaymentReference { get; set; }

    public string? Notes { get; set; }

    public Guid CapturedByMemberId { get; set; }
    public Member? CapturedByMember { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
