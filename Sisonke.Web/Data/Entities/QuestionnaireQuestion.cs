using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class QuestionnaireQuestion
{
    public Guid Id { get; set; }

    public Guid QuestionnaireSectionId { get; set; }
    public QuestionnaireSection QuestionnaireSection { get; set; } = default!;

    public StokvelType? StokvelType { get; set; }

    [Required]
    [MaxLength(500)]
    public string QuestionText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? HelpText { get; set; }

    public QuestionType QuestionType { get; set; } = QuestionType.Text;

    public bool IsRequired { get; set; } = true;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<QuestionnaireOption> Options { get; set; } = new List<QuestionnaireOption>();
}
