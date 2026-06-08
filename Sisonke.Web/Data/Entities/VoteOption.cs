using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class VoteOption
{
    public Guid Id { get; set; }

    public Guid VoteMotionId { get; set; }
    public VoteMotion? VoteMotion { get; set; }

    [Required]
    [MaxLength(150)]
    public string OptionText { get; set; } = string.Empty;

    public int SortOrder { get; set; }
}
