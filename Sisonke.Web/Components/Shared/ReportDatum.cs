namespace Sisonke.Web.Components.Shared;

public sealed record ReportDatum(
    string Label,
    decimal Value,
    string Color,
    string? Detail = null);

