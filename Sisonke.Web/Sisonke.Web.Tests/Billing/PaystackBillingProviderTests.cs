using System.Net;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>All Paystack calls are mocked at the HttpMessageHandler level with recorded fixtures — no live API calls.</summary>
public class PaystackBillingProviderTests
{
    private const string SecretKey = "sk_test_super_secret_value_12345";

    [Fact]
    public async Task EnsureCustomerAsync_ReturnsCustomerCode()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "customer", HttpStatusCode.OK,
            """{"status":true,"message":"ok","data":{"customer_code":"CUS_abc123"}}""");

        var customerCode = await provider.EnsureCustomerAsync(Guid.NewGuid(), "chair@example.com", "Jane Chair", "0821234567");

        Assert.Equal("CUS_abc123", customerCode);
    }

    [Fact]
    public async Task EnsurePlanAsync_ReturnsPlanCode()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "plan", HttpStatusCode.OK,
            """{"status":true,"message":"ok","data":{"plan_code":"PLN_xyz789"}}""");

        var planCode = await provider.EnsurePlanAsync("GROWING", "Growing Society", 24900, "monthly");

        Assert.Equal("PLN_xyz789", planCode);
    }

    [Fact]
    public async Task StartCardAuthorisationAsync_ReturnsUrlAndReference_AndSendsAGeneratedReference()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "transaction/initialize", HttpStatusCode.OK,
            """{"status":true,"message":"ok","data":{"authorization_url":"https://checkout.paystack.com/abc","access_code":"acc_1","reference":"sisonke-auth-generated"}}""");

        var result = await provider.StartCardAuthorisationAsync("CUS_abc123", "chair@example.com", 100, "https://app.sisonke/callback");

        Assert.Equal("https://checkout.paystack.com/abc", result.AuthorisationUrl);
        Assert.Single(handler.RequestBodies);
        Assert.Contains("sisonke-auth-", handler.RequestBodies[0]);
        Assert.Contains("\"amount\":100", handler.RequestBodies[0]);
    }

    [Fact]
    public async Task VerifyAuthorisationAsync_ParsesReusableAndCardMetadata()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Get, "transaction/verify/ref-123", HttpStatusCode.OK,
            """
            {"status":true,"message":"ok","data":{"status":"success","reference":"ref-123",
            "authorization":{"authorization_code":"AUTH_xyz","reusable":true,"last4":"4242","exp_month":"12","exp_year":"2030","card_type":"visa","bank":"Test Bank"},
            "customer":{"email":"chair@example.com"}}}
            """);

        var result = await provider.VerifyAuthorisationAsync("ref-123");

        Assert.True(result.Success);
        Assert.True(result.Reusable);
        Assert.Equal("AUTH_xyz", result.AuthorizationCode);
        Assert.Equal("4242", result.Last4);
        Assert.Equal(12, result.ExpiryMonth);
        Assert.Equal(2030, result.ExpiryYear);
    }

    [Fact]
    public async Task VerifyAuthorisationAsync_NonReusable_StillParsesSuccessfully()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Get, "transaction/verify/ref-456", HttpStatusCode.OK,
            """
            {"status":true,"message":"ok","data":{"status":"success","reference":"ref-456",
            "authorization":{"authorization_code":"AUTH_once","reusable":false,"last4":"1111","exp_month":"01","exp_year":"2029","card_type":"verve"},
            "customer":{"email":"chair@example.com"}}}
            """);

        var result = await provider.VerifyAuthorisationAsync("ref-456");

        Assert.True(result.Success);
        Assert.False(result.Reusable);
    }

    [Fact]
    public async Task CreateSubscriptionAsync_ReturnsSubscriptionCodeAndEmailToken()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "subscription", HttpStatusCode.OK,
            """{"status":true,"message":"ok","data":{"subscription_code":"SUB_1","email_token":"tok_1","next_payment_date":"2026-10-03T00:00:00.000Z"}}""");

        var result = await provider.CreateSubscriptionAsync("CUS_abc123", "PLN_xyz789", "AUTH_xyz", DateTime.UtcNow.AddDays(60));

        Assert.Equal("SUB_1", result.SubscriptionCode);
        Assert.Equal("tok_1", result.EmailToken);
    }

    [Fact]
    public async Task CancelSubscriptionAsync_Succeeds_WithoutThrowing()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "subscription/disable", HttpStatusCode.OK,
            """{"status":true,"message":"Subscription disabled successfully"}""");

        var exception = await Record.ExceptionAsync(() => provider.CancelSubscriptionAsync("SUB_1", "tok_1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task FailedResponse_ThrowsPaystackApiException_WithoutLeakingBody()
    {
        var (provider, handler, _) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "customer", HttpStatusCode.BadRequest,
            """{"status":false,"message":"Email is required"}""");

        var exception = await Assert.ThrowsAsync<PaystackApiException>(
            () => provider.EnsureCustomerAsync(Guid.NewGuid(), "", "", null));

        Assert.Equal("Email is required", exception.Message);
    }

    [Fact]
    public async Task NoCallEverLogsTheSecretKeyOrCardMetadata()
    {
        var (provider, handler, logger) = CreateProvider();
        handler.SetResponse(HttpMethod.Post, "customer", HttpStatusCode.OK,
            """{"status":true,"message":"ok","data":{"customer_code":"CUS_abc123"}}""");
        handler.SetResponse(HttpMethod.Get, "transaction/verify/ref-999", HttpStatusCode.OK,
            """
            {"status":true,"message":"ok","data":{"status":"success","reference":"ref-999",
            "authorization":{"authorization_code":"AUTH_secretcode","reusable":true,"last4":"9999","exp_month":"12","exp_year":"2031","card_type":"mastercard"},
            "customer":{"email":"chair@example.com"}}}
            """);
        handler.SetResponse(HttpMethod.Post, "customer_fail", HttpStatusCode.BadRequest,
            """{"status":false,"message":"boom"}""");

        await provider.EnsureCustomerAsync(Guid.NewGuid(), "chair@example.com", "Jane Chair", "0821234567");
        await provider.VerifyAuthorisationAsync("ref-999");

        var allLogText = string.Join('\n', logger.Messages);

        Assert.DoesNotContain(SecretKey, allLogText);
        Assert.DoesNotContain("9999", allLogText); // last4
        Assert.DoesNotContain("AUTH_secretcode", allLogText); // authorization code
    }

    private static (PaystackBillingProvider Provider, FakeHttpMessageHandler Handler, RecordingLogger<PaystackBillingProvider> Logger) CreateProvider()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.paystack.co/") };
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", SecretKey);
        var logger = new RecordingLogger<PaystackBillingProvider>();

        return (new PaystackBillingProvider(httpClient, logger), handler, logger);
    }
}
