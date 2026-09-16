namespace Sisonke.Web.Data.Entities;

/// <summary>
/// One row per calendar year. Backs InvoiceNumberGenerator's gapless SIS-INV-{year}-{seq}
/// numbering. Not a SQL Server SEQUENCE/EF HiLo — this app also runs on SQLite (dev/tests),
/// which supports neither; see IInvoiceNumberGenerator for the portable alternative.
/// </summary>
public class InvoiceNumberCounter
{
    public int Year { get; set; }

    public int NextNumber { get; set; } = 1;
}
