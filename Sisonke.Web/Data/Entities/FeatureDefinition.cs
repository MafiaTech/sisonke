using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class FeatureDefinition
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(60)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string Category { get; set; } = string.Empty;

    public FeatureDataType DataType { get; set; }

    /// <summary>
    /// String-encoded default: "true"/"false" for Boolean, an integer (or empty for unlimited)
    /// for Numeric, the enum member name for Enum.
    /// </summary>
    [MaxLength(50)]
    public string DefaultValue { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public ICollection<PlanFeature> PlanFeatures { get; set; } = new List<PlanFeature>();
}
