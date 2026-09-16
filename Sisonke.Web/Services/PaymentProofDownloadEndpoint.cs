using System.Security.Claims;

namespace Sisonke.Web.Services;

public static class PaymentProofDownloadEndpoint
{
    public static async Task<IResult> HandleAsync(Guid stokvelId, Guid documentId, HttpContext http,
        MemberPaymentSubmissionService submissions)
    {
        var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
        http.Response.Headers.CacheControl = "no-store, private";
        http.Response.Headers["X-Content-Type-Options"] = "nosniff";
        http.Response.Headers["Content-Security-Policy"] = "sandbox; default-src 'none'";
        try
        {
            var proof = await submissions.OpenProofAsync(userId, stokvelId, documentId);
            return Results.File(proof.Stream, proof.ContentType, proof.FileName, enableRangeProcessing: false);
        }
        catch (UnauthorizedAccessException) { return Results.NotFound(); }
        catch (InvalidOperationException) { return Results.NotFound(); }
        catch (FileNotFoundException) { return Results.NotFound(); }
        catch (DirectoryNotFoundException) { return Results.NotFound(); }
    }
}
