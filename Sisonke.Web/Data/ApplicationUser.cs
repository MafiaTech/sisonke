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
}

