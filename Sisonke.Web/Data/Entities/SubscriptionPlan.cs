using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class SubscriptionPlan
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int MinMembers { get; set; }

    public int? MaxMembers { get; set; }

    public decimal MonthlyPrice { get; set; }

    public decimal AnnualPrice { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
}
