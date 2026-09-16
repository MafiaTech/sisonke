using System.ComponentModel.DataAnnotations;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data.Entities;

public class SubscriptionInvoice
{
    public Guid Id { get; set; }

    public Guid OrganisationSubscriptionId { get; set; }
    public OrganisationSubscription OrganisationSubscription { get; set; } = default!;

    /// <summary>Human-readable sequential number, e.g. SIS-INV-2026-000123.</summary>
    [Required]
    [MaxLength(40)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public SubscriptionInvoiceStatus Status { get; set; } = SubscriptionInvoiceStatus.Draft;

    public DateTime PeriodStart { get; set; }

    public DateTime PeriodEnd { get; set; }

    public decimal SubTotal { get; set; }

    public decimal VatAmount { get; set; }

    public decimal Total { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "ZAR";

    public DateTime? IssuedAt { get; set; }

    public DateTime? DueAt { get; set; }

    public DateTime? PaidAt { get; set; }

    [MaxLength(100)]
    public string? ProviderInvoiceCode { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<SubscriptionInvoiceLine> Lines { get; set; } = new List<SubscriptionInvoiceLine>();

    public ICollection<SubscriptionPayment> Payments { get; set; } = new List<SubscriptionPayment>();
}
