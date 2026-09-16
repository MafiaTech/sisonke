using System.Security.Cryptography;
using System.Text;

namespace Sisonke.Web.Services.Billing.Paystack;

/// <summary>
/// Verifies the x-paystack-signature header: HMAC-SHA512 of the raw request body, keyed with
/// the Paystack integration secret key, compared in constant time. Must run against the raw body captured before
/// model binding — ASP.NET Core's JSON model binder can re-serialize with different formatting
/// and break the signature.
/// </summary>
public static class PaystackWebhookSignatureVerifier
{
    public static bool IsValid(string rawBody, string? signatureHeader, string integrationSecretKey)
    {
        if (string.IsNullOrEmpty(signatureHeader) || string.IsNullOrEmpty(integrationSecretKey))
        {
            return false;
        }

        var keyBytes = Encoding.UTF8.GetBytes(integrationSecretKey);
        var bodyBytes = Encoding.UTF8.GetBytes(rawBody);
        var computedHash = HMACSHA512.HashData(keyBytes, bodyBytes);
        var computedSignature = Convert.ToHexStringLower(computedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signatureHeader.Trim().ToLowerInvariant()));
    }
}
