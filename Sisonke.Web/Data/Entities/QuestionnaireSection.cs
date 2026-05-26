using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class QuestionnaireSection
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<QuestionnaireQuestion> Questions { get; set; } = new List<QuestionnaireQuestion>();
}
