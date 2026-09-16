using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Paystack;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class PaystackPaymentSetupProviderTests
{
    private const string Reference = "sisonke-setup-abc123";

    [Fact]
    public async Task VerifiedReusableCard_MatchingServerExpectations_BecomesReadyAndIsRefunded()
    {
        var billing = CreateVerifiedBillingProvider();
        var provider = CreateProvider(billing);

        var result = await provider.GetPaymentMethodStatusAsync(Request());

        Assert.True(result.Success);
        Assert.True(result.IsReusable);
        Assert.Equal(MandateStatus.Active, result.MandateStatus);
        Assert.Equal("AUTH_safe", result.ProviderPaymentMethodReference);
        Assert.Equal("Card ending 4242", result.MaskedDisplay);
        Assert.Equal([Reference], billing.RefundedReferences);
        Assert.Empty(billing.ChargeAttempts);
        Assert.Equal(0, billing.CreateSubscriptionCalls);
    }

    [Fact]
    public async Task NonReusableCard_DoesNotBecomeReadyOrTriggerRefund()
    {
        var billing = CreateVerifiedBillingProvider();
        billing.VerificationToReturn = billing.VerificationToReturn with { Reusable = false };
        var provider = CreateProvider(billing);

        var result = await provider.GetPaymentMethodStatusAsync(Request());

        Assert.False(result.Success);
        Assert.False(result.IsReusable);
        Assert.Empty(billing.RefundedReferences);
    }

    [Theory]
    [InlineData("wrong-reference", 100, "ZAR", "test", "card", "billing@example.test", "CUS_expected")]
    [InlineData(Reference, 101, "ZAR", "test", "card", "billing@example.test", "CUS_expected")]
    [InlineData(Reference, 100, "USD", "test", "card", "billing@example.test", "CUS_expected")]
    [InlineData(Reference, 100, "ZAR", "live", "card", "billing@example.test", "CUS_expected")]
    [InlineData(Reference, 100, "ZAR", "test", "eft", "billing@example.test", "CUS_expected")]
    [InlineData(Reference, 100, "ZAR", "test", "card", "other@example.test", "CUS_expected")]
    [InlineData(Reference, 100, "ZAR", "test", "card", "billing@example.test", "CUS_other")]
    public async Task VerificationMismatch_DoesNotBecomeReady(
        string reference, long amount, string currency, string domain, string channel,
        string email, string customerCode)
    {
        var billing = CreateVerifiedBillingProvider();
        billing.VerificationToReturn = billing.VerificationToReturn with
        {
            Reference = reference,
            AmountMinorUnits = amount,
            Currency = currency,
            Domain = domain,
            Channel = channel,
            CustomerEmail = email,
            CustomerCode = customerCode
        };
        var provider = CreateProvider(billing);

        var result = await provider.GetPaymentMethodStatusAsync(Request());

        Assert.False(result.Success);
        Assert.False(result.IsReusable);
        Assert.Null(result.ProviderPaymentMethodReference);
        Assert.Empty(billing.RefundedReferences);
    }

    [Fact]
    public async Task ProviderTimeout_FailsSafelyWithoutReadiness()
    {
        var billing = CreateVerifiedBillingProvider();
        billing.VerificationException = new TaskCanceledException("simulated timeout");
        var provider = CreateProvider(billing);

        var result = await provider.GetPaymentMethodStatusAsync(Request());

        Assert.False(result.Success);
        Assert.Equal("setup_failed", result.ErrorCode);
        Assert.DoesNotContain("timeout", result.Message!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestMode_RejectsLiveSecretKey()
    {
        var provider = new PaystackPaymentSetupProvider(
            CreateVerifiedBillingProvider(),
            new PaystackOptions { SecretKey = "sk_live_placeholder" },
            new SubscriptionPaymentOptions { TestMode = true },
            NullLogger<PaystackPaymentSetupProvider>.Instance);

        Assert.False(provider.IsConfigured);
        Assert.Contains("test key", provider.UnavailableMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static PaystackPaymentSetupProvider CreateProvider(FakeBillingProvider billing) => new(
        billing,
        new PaystackOptions
        {
            SecretKey = "sk_test_placeholder",
            Currency = "ZAR",
            CardVerificationAmountMinorUnits = 100
        },
        new SubscriptionPaymentOptions { TestMode = true },
        NullLogger<PaystackPaymentSetupProvider>.Instance);

    private static FakeBillingProvider CreateVerifiedBillingProvider() => new()
    {
        VerificationToReturn = new(
            true, Reference, 100, "ZAR", "test", "card", "AUTH_safe", true,
            "visa", "4242", 12, 2030, "Test Bank", "billing@example.test", "CUS_expected")
    };

    private static PaymentMethodStatusRequest Request() =>
        new(Reference, "CUS_expected", "billing@example.test");
}
