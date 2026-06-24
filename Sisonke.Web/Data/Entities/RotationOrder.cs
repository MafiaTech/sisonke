using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class RotationOrder
{
    public Guid Id { get; set; }

    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    public Guid MemberId { get; set; }
    public Member Member { get; set; } = default!;

    public int Position { get; set; }

    public bool HasReceivedPayout { get; set; }

    public Guid? ReceivedCycleId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    [MaxLength(450)]
    public string? CreatedBy { get; set; }

    [MaxLength(450)]
    public string? UpdatedBy { get; set; }
}
