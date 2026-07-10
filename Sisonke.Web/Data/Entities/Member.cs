using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class Member
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [MaxLength(450)]
    public string? ApplicationUserId { get; set; }

    [MaxLength(50)]
    public string MemberNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string CellphoneNumber { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? EmailAddress { get; set; }

    public bool EmailEnabled { get; set; } = true;

    public bool WebPushEnabled { get; set; } = true;

    [MaxLength(30)]
    public string? IdNumber { get; set; }

    public DateTime JoiningDate { get; set; }

    public MemberStatus Status { get; set; } = MemberStatus.Active;

    public MemberGovernanceStatus GovernanceStatus { get; set; } = MemberGovernanceStatus.Active;

    public DateTime? GovernanceStatusChangedAt { get; set; }

    [MaxLength(500)]
    public string? GovernanceStatusReason { get; set; }

    public DateTime? LastWarningIssuedAt { get; set; }

    public DateTime? SuspendedAt { get; set; }

    public DateTime? ExpelledAt { get; set; }

    public SisonkeRole DefaultRole { get; set; } = SisonkeRole.Member;

    [MaxLength(150)]
    public string? ResidentialArea { get; set; }

    public bool IsInCoolingPeriod { get; set; }

    public DateTime? CoolingPeriodEndDate { get; set; }

    public bool IsDeceased { get; set; }

    public DateTime? DeceasedDate { get; set; }

    public DateTime? DeathReportedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<NextOfKin> NextOfKinRecords { get; set; } = new List<NextOfKin>();

    public ICollection<Beneficiary> Beneficiaries { get; set; } = new List<Beneficiary>();
}
