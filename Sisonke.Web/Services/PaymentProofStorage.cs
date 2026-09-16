using Microsoft.AspNetCore.Components.Forms;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Services;

/// <summary>Private files, with metadata in EF, following the existing document storage pattern.</summary>
public sealed class PaymentProofStorage(IWebHostEnvironment environment)
{
    public const long MaxFileSize = 10 * 1024 * 1024;

    public async Task<PaymentProofDocument> StoreAsync(Guid tenantId, string userId, IBrowserFile file)
    {
        if (file.Size <= 0 || file.Size > MaxFileSize)
            throw new InvalidOperationException("Proof must be between 1 byte and 10 MB.");
        var name = file.Name.Replace('\\', '/').Split('/').Last();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 300 || name.Any(char.IsControl))
            throw new InvalidOperationException("Invalid proof filename.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var contentType = extension switch
        {
            ".pdf" => "application/pdf",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            _ => throw new InvalidOperationException("Only PDF, JPG/JPEG and PNG proofs are supported.")
        };
        if (!string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Proof content type does not match its extension.");

        await using var input = file.OpenReadStream(MaxFileSize);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(chunk)) > 0)
        {
            if (buffer.Length + read > MaxFileSize)
                throw new InvalidOperationException("Proof exceeds the 10 MB limit.");
            await buffer.WriteAsync(chunk.AsMemory(0, read));
        }
        var bytes = buffer.ToArray();
        if (bytes.LongLength != file.Size || !HasSignature(bytes, contentType))
            throw new InvalidOperationException("Proof file contents do not match the declared format or size.");

        var document = new PaymentProofDocument
        {
            TenantId = tenantId, OriginalFileName = name, StoredFileName = $"{Guid.NewGuid():N}{extension}",
            ContentType = contentType, FileSize = bytes.Length, UploadedByUserId = userId
        };
        var path = GetPath(document);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, bytes);
        return document;
    }

    public Stream OpenRead(PaymentProofDocument document) => File.OpenRead(GetPath(document));

    private string GetPath(PaymentProofDocument document)
    {
        // Only server-generated names are accepted, including when loading persisted metadata.
        var name = document.StoredFileName;
        if (name != Path.GetFileName(name) || name.Contains('/') || name.Contains('\\') ||
            !Guid.TryParseExact(Path.GetFileNameWithoutExtension(name), "N", out _) ||
            Path.GetExtension(name) is not (".pdf" or ".jpg" or ".jpeg" or ".png"))
            throw new InvalidOperationException("Invalid proof storage identifier.");
        return Path.Combine(environment.ContentRootPath, "App_Data", "payment-proofs", document.TenantId.ToString("N"), name);
    }

    private static bool HasSignature(byte[] bytes, string type) => type switch
    {
        "application/pdf" => bytes.AsSpan().StartsWith("%PDF-"u8),
        "image/png" => bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255,
        _ => false
    };
}
