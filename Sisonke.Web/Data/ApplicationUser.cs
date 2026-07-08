using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Sisonke.Web.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    [MaxLength(150)]
    public string? FullName { get; set; }

    [MaxLength(30)]
    public string? IdNumber { get; set; }

    [MaxLength(30)]
    public string? CellphoneNumber { get; set; }

    [MaxLength(250)]
    public string? ResidentialArea { get; set; }

    [MaxLength(50)]
    public string? ExternalAuthProvider { get; set; }

    [MaxLength(150)]
    public string? ExternalTenantId { get; set; }

    [MaxLength(150)]
    public string? ExternalObjectId { get; set; }

    [MaxLength(256)]
    public string? ExternalEmail { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public DateTime? LastLoginAt { get; set; }

    [MaxLength(45)]
    public string? LastLoginIp { get; set; }
}

