using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class StokvelBankingDetails
{
    public Guid Id { get; set; }
    public Guid StokvelId { get; set; }
    public Stokvel Stokvel { get; set; } = default!;

    [Required, MaxLength(150)] public string BankName { get; set; } = string.Empty;
    [Required, MaxLength(150)] public string AccountHolderName { get; set; } = string.Empty;
    [Required, MaxLength(40)] public string AccountNumber { get; set; } = string.Empty;
    public BankAccountType AccountType { get; set; }
    [MaxLength(20)] public string? BranchCode { get; set; }
    [MaxLength(150)] public string? BranchName { get; set; }
    [MaxLength(200)] public string? PaymentReferenceFormat { get; set; }
    [MaxLength(1000)] public string? Notes { get; set; }

    public bool IsPrimary { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [MaxLength(450)] public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    [MaxLength(450)] public string? UpdatedBy { get; set; }
}
