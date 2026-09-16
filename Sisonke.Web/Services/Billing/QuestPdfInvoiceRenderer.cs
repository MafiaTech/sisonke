using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// COMPLIANCE FLAG: layout/wording is a functional placeholder, not reviewed SARS-compliant tax
/// invoice copy — see InvoicingOptions and brief rule 10. Requires sign-off before real use.
/// </summary>
public sealed class QuestPdfInvoiceRenderer(InvoicingOptions options) : IInvoicePdfRenderer
{
    public byte[] RenderInvoice(SubscriptionInvoice invoice, IReadOnlyList<SubscriptionInvoiceLine> lines, Stokvel stokvel)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text(options.VatRegistered ? "Tax Invoice" : "Invoice").FontSize(18).Bold();
                    column.Item().Text(options.CompanyName);
                    if (options.VatRegistered && !string.IsNullOrWhiteSpace(options.VatNumber))
                    {
                        column.Item().Text($"VAT No: {options.VatNumber}");
                    }
                });

                page.Content().Column(column =>
                {
                    column.Spacing(8);

                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Invoice: {invoice.InvoiceNumber}");
                        row.RelativeItem().AlignRight().Text($"Issued: {(invoice.IssuedAt ?? invoice.CreatedAt):yyyy-MM-dd}");
                    });
                    column.Item().Text($"Billed to: {stokvel.Name}");
                    column.Item().Text($"Period: {invoice.PeriodStart:yyyy-MM-dd} – {invoice.PeriodEnd:yyyy-MM-dd}");

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Text("Description").Bold();
                            header.Cell().AlignRight().Text("Qty").Bold();
                            header.Cell().AlignRight().Text("Unit price").Bold();
                            header.Cell().AlignRight().Text("Total").Bold();
                        });

                        foreach (var line in lines.OrderBy(l => l.SortOrder))
                        {
                            table.Cell().Text(line.Description);
                            table.Cell().AlignRight().Text(line.Quantity.ToString());
                            table.Cell().AlignRight().Text(FormatMoney(line.UnitPrice, invoice.Currency));
                            table.Cell().AlignRight().Text(FormatMoney(line.LineTotal, invoice.Currency));
                        }
                    });

                    column.Item().AlignRight().Column(totals =>
                    {
                        totals.Item().Text($"Subtotal: {FormatMoney(invoice.SubTotal, invoice.Currency)}");
                        if (options.VatRegistered)
                        {
                            totals.Item().Text($"VAT ({options.VatRatePercent:0.##}%): {FormatMoney(invoice.VatAmount, invoice.Currency)}");
                        }
                        totals.Item().Text($"Total: {FormatMoney(invoice.Total, invoice.Currency)}").Bold();
                    });
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Gateway transaction fees for member collections are billed separately and never appear on this invoice.");
                });
            });
        });

        return document.GeneratePdf();
    }

    public byte[] RenderReceipt(SubscriptionPayment payment, SubscriptionInvoice? invoice, Stokvel stokvel)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Text("Payment Receipt").FontSize(18).Bold();
                    column.Item().Text(options.CompanyName);
                });

                page.Content().Column(column =>
                {
                    column.Spacing(8);
                    column.Item().Text($"Receipt for: {stokvel.Name}");
                    column.Item().Text($"Date: {(payment.PaidAt ?? payment.CreatedAt):yyyy-MM-dd}");
                    if (invoice is not null)
                    {
                        column.Item().Text($"Invoice: {invoice.InvoiceNumber}");
                    }
                    column.Item().Text($"Reference: {payment.ProviderReference}");
                    column.Item().Text($"Amount paid: {FormatMoney(payment.Amount, payment.Currency)}").Bold();
                });
            });
        });

        return document.GeneratePdf();
    }

    private static string FormatMoney(decimal amount, string currency) => $"{currency} {amount:N2}";
}
