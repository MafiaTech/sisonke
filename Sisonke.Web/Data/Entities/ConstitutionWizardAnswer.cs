using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class ConstitutionWizardAnswer
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = default!;

    [Required]
    [MaxLength(150)]
    public string QuestionKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(250)]
    public string QuestionText { get; set; } = string.Empty;

    public int StepNumber { get; set; }

    [MaxLength(4000)]
    public string AnswerValue { get; set; } = string.Empty;

    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow;
}
