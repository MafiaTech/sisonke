using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class MemberDependent
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Relationship { get; set; } = string.Empty;

    public DateTime? DateOfBirth { get; set; }

    [MaxLength(30)]
    public string? IdNumber { get; set; }

    [MaxLength(30)]
    public string? CellphoneNumber { get; set; }

    public bool IsActive { get; set; } = true;

    public DependentCoverageStatus CoverageStatus { get; set; } = DependentCoverageStatus.Active;

    public bool IsDeceased { get; set; }

    public DateTime? DeceasedDate { get; set; }

    public DateTime? DeathReportedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
