using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class ContributionCycle
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public DateTime DueDate { get; set; }

    public ContributionCycleStatus Status { get; set; } = ContributionCycleStatus.Open;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
