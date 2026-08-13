using Microsoft.AspNetCore.DataProtection;
using System.Security.Cryptography;

namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Signed, time-limited download tokens for invoice/receipt PDFs, using ASP.NET Core's built-in
/// Data Protection stack (already part of the framework — no new secret to manage). A token
/// encodes the document type + id and expires; the download endpoint rejects anything else.
/// </summary>
public sealed class InvoiceDownloadTokenService
{
    private const string Purpose = "Sisonke.Billing.DocumentDownload";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    private readonly ITimeLimitedDataProtector _protector;

    public InvoiceDownloadTokenService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
    }

    public string CreateToken(string documentType, Guid documentId) =>
        _protector.Protect($"{documentType}:{documentId:N}", TokenLifetime);

    public bool TryValidate(string token, string expectedDocumentType, Guid expectedDocumentId)
    {
        try
        {
            var payload = _protector.Unprotect(token);
            return string.Equals(payload, $"{expectedDocumentType}:{expectedDocumentId:N}", StringComparison.Ordinal);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
