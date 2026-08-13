namespace Sisonke.Web.Services.Billing;

/// <summary>
/// Saves generated invoice/receipt PDFs under wwwroot/uploads, matching the existing document
/// storage convention (see MemberService's member-document upload handling). Returns a
/// web-relative path (e.g. "/uploads/subscription-invoices/{stokvelId}/{invoiceNumber}.pdf"),
/// same shape as MemberDocument.StoredFilePath / FuneralClaimDocument.StoredFilePath.
/// </summary>
public sealed class BillingDocumentStorage(IWebHostEnvironment webHostEnvironment)
{
    public async Task<string> SaveAsync(string category, Guid stokvelId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var webRoot = webHostEnvironment.WebRootPath ?? Path.Combine(webHostEnvironment.ContentRootPath, "wwwroot");
        var relativeDirectory = Path.Combine("uploads", category, stokvelId.ToString());
        var absoluteDirectory = Path.Combine(webRoot, relativeDirectory);
        Directory.CreateDirectory(absoluteDirectory);

        var safeFileName = string.Join("_", fileName.Split(Path.GetInvalidFileNameChars()));
        var absolutePath = Path.Combine(absoluteDirectory, safeFileName);
        await File.WriteAllBytesAsync(absolutePath, content, ct);

        return $"/uploads/{category}/{stokvelId}/{safeFileName}".Replace('\\', '/');
    }
}
