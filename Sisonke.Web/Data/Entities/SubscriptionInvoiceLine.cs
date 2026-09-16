using System.ComponentModel.DataAnnotations;

namespace Sisonke.Web.Data.Entities;

public class SubscriptionInvoiceLine
{
    public Guid Id { get; set; }

    public Guid SubscriptionInvoiceId { get; set; }
    public SubscriptionInvoice SubscriptionInvoice { get; set; } = default!;

    [Required]
    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    public decimal LineTotal { get; set; }

    /// <summary>References FeatureCodes when the line represents a metered/add-on feature.</summary>
    [MaxLength(60)]
    public string? FeatureCode { get; set; }

    public int SortOrder { get; set; }
}
