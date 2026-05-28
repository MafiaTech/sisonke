using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class Member
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

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

    [MaxLength(30)]
    public string? IdNumber { get; set; }

    public DateTime JoiningDate { get; set; }

    public MemberStatus Status { get; set; } = MemberStatus.Active;

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
