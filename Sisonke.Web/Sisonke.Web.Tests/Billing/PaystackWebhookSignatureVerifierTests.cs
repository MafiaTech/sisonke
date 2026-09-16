using System.Security.Cryptography;
using System.Text;
using Sisonke.Web.Services.Billing.Paystack;

namespace Sisonke.Web.Tests.Billing;

public class PaystackWebhookSignatureVerifierTests
{
    private const string Secret = "whsec_test_secret";
    private const string Body = """{"event":"charge.success","data":{"reference":"abc123"}}""";

    [Fact]
    public void ValidSignature_IsAccepted()
    {
        var signature = ComputeSignature(Body, Secret);

        Assert.True(PaystackWebhookSignatureVerifier.IsValid(Body, signature, Secret));
    }

    [Fact]
    public void TamperedBody_IsRejected()
    {
        var signature = ComputeSignature(Body, Secret);
        var tamperedBody = Body.Replace("abc123", "abc124");

        Assert.False(PaystackWebhookSignatureVerifier.IsValid(tamperedBody, signature, Secret));
    }

    [Fact]
    public void WrongSecret_IsRejected()
    {
        var signature = ComputeSignature(Body, "a-different-secret");

        Assert.False(PaystackWebhookSignatureVerifier.IsValid(Body, signature, Secret));
    }

    [Fact]
    public void MissingHeader_IsRejected()
    {
        Assert.False(PaystackWebhookSignatureVerifier.IsValid(Body, null, Secret));
        Assert.False(PaystackWebhookSignatureVerifier.IsValid(Body, string.Empty, Secret));
    }

    [Fact]
    public void MissingIntegrationSecretKey_IsRejected()
    {
        var signature = ComputeSignature(Body, Secret);

        Assert.False(PaystackWebhookSignatureVerifier.IsValid(Body, signature, string.Empty));
    }

    private static string ComputeSignature(string body, string secret)
    {
        var hash = HMACSHA512.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(body));
        return Convert.ToHexStringLower(hash);
    }
}
