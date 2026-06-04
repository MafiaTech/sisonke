namespace Sisonke.Web.Data.Entities;

public class ClaimPayoutAudit
{
    public Guid Id { get; set; }

    public Guid FuneralClaimId { get; set; }
    public FuneralClaim? FuneralClaim { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel? Stokvel { get; set; }

    public string Action { get; set; } = string.Empty;

    public decimal? PreviousPayoutAmount { get; set; }

    public decimal? NewPayoutAmount { get; set; }

    public string? PreviousStatus { get; set; }

    public string? NewStatus { get; set; }

    public string? PayoutReference { get; set; }

    public string? Notes { get; set; }

    public Guid CapturedByMemberId { get; set; }
    public Member? CapturedByMember { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
