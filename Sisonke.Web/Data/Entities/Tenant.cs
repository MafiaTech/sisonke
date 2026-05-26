using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class Tenant
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Slug { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<Stokvel> Stokvels { get; set; } = new List<Stokvel>();

    public ICollection<TenantSubscription> TenantSubscriptions { get; set; } = new List<TenantSubscription>();
}
