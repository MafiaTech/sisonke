using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class StokvelQuestionnaireAnswer
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    public Guid QuestionnaireQuestionId { get; set; }
    public QuestionnaireQuestion QuestionnaireQuestion { get; set; } = default!;

    [MaxLength(2000)]
    public string AnswerValue { get; set; } = string.Empty;

    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}
