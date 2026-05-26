using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class QuestionnaireOption
{
    public Guid Id { get; set; }

    public Guid QuestionnaireQuestionId { get; set; }
    public QuestionnaireQuestion QuestionnaireQuestion { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string OptionText { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? OptionValue { get; set; }

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
