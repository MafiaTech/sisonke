using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class Beneficiary
{
    public Guid Id { get; set; }

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(80)]
    public string Relationship { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? IdNumber { get; set; }

    [MaxLength(30)]
    public string? CellphoneNumber { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public bool IsActive { get; set; } = true;
}
