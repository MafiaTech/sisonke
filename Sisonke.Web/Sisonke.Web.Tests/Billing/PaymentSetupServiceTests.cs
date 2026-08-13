using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Netcash;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class PaymentSetupServiceTests : IDisposable
{
    private readonly SqliteTestDatabase database = new();
    private readonly TestDbContextFactory dbFactory;
    private readonly FakeSetupProvider provider = new();
    private readonly SubscriptionPaymentSetupService sut;

    public PaymentSetupServiceTests()
    {
        dbFactory = new(database);
        var memberAccess = new MemberAccessService(database.CreateContext());
        var resolver = new SubscriptionPaymentProviderResolver([provider]);
        var protector = new PaymentSetupStateProtector(new EphemeralDataProtectionProvider());
        sut = new(dbFactory, memberAccess, resolver, protector,
            new SubscriptionPaymentOptions(), new FakeTimeProvider(DateTimeOffset.UtcNow),
            NullLogger<SubscriptionPaymentSetupService>.Instance);
    }

    [Fact]
    public void Resolver_ReturnsMatchingProvider_AndEnumValuesRemainStable()
    {
        var netcash = new NetcashPaymentSetupProvider(new NetcashOptions());
        var resolver = new SubscriptionPaymentProviderResolver([provider, netcash]);
        Assert.Same(provider, resolver.GetProvider(SubscriptionProvider.Paystack));
        Assert.Same(netcash, resolver.GetProvider(SubscriptionProvider.Netcash));
        Assert.False(netcash.IsConfigured);
        Assert.Equal(1, (int)SubscriptionProvider.Paystack);
        Assert.Equal(2, (int)SubscriptionProvider.Netcash);
    }

    [Fact]
    public void NetcashReadiness_RequiresActiveMandateReference()
    {
        var method = new SubscriptionPaymentMethod
        {
            Provider = SubscriptionProvider.Netcash,
            PaymentMethodType = SubscriptionPaymentMethodType.DebiCheck,
            MandateStatus = MandateStatus.Active,
            ProviderMandateReference = "safe-mandate-reference"
        };

        Assert.True(SubscriptionPaymentReadiness.IsReady(method));
        method.ProviderMandateReference = null;
        Assert.False(SubscriptionPaymentReadiness.IsReady(method));
    }

    [Fact]
    public async Task TrialOfficeBearer_CanSetUpPaymentMethod_AndDuplicateCallbackIsIdempotent()
    {
        var seeded = await SeedAsync();
        var start = await sut.StartAsync(seeded.StokvelId, seeded.AdminId, "billing@example.test", "Test Admin", null,
            "https://localhost/billing/payment-callback", "/subscription");

        Assert.True(start.Success);
        Assert.NotNull(provider.LastRequest);
        var protectedState = GetQueryValue(provider.LastRequest!.CallbackUrl, "state");
        var complete = await sut.CompleteAsync(protectedState, provider.LastRequest.CorrelationReference, seeded.AdminId);
        var duplicate = await sut.CompleteAsync(protectedState, provider.LastRequest.CorrelationReference, seeded.AdminId);

        Assert.True(complete.Success);
        Assert.True(duplicate.Success);
        await using var verify = database.CreateContext();
        var method = await verify.SubscriptionPaymentMethods.SingleAsync();
        Assert.Equal(SubscriptionPaymentMethodType.Card, method.PaymentMethodType);
        Assert.Equal(MandateStatus.Active, method.MandateStatus);
        Assert.Equal("safe-provider-token", method.ProviderPaymentMethodReference);
        Assert.Equal(provider.LastRequest.CorrelationReference, method.ProviderMandateReference);
        Assert.Equal(1, await verify.SubscriptionPaymentMethods.CountAsync());
    }

    [Fact]
    public async Task Setup_CannotTargetAnotherStokvel_AndInvalidCorrelationIsRejected()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        var crossTenant = await sut.StartAsync(second.StokvelId, first.AdminId, "billing@example.test", "Admin", null,
            "https://localhost/billing/payment-callback", "/subscription");
        Assert.False(crossTenant.Success);

        var valid = await sut.StartAsync(first.StokvelId, first.AdminId, "billing@example.test", "Admin", null,
            "https://localhost/billing/payment-callback", "/subscription");
        Assert.True(valid.Success);
        var protectedState = GetQueryValue(provider.LastRequest!.CallbackUrl, "state");
        var invalid = await sut.CompleteAsync(protectedState, "different-reference", first.AdminId);
        Assert.False(invalid.Success);
    }

    [Fact]
    public async Task Status_IsPaymentReady_WithoutExposingProviderReferences()
    {
        var seeded = await SeedAsync();
        await using (var context = database.CreateContext())
        {
            var subscription = await context.OrganisationSubscriptions.SingleAsync(value => value.StokvelId == seeded.StokvelId);
            context.SubscriptionPaymentMethods.Add(new()
            {
                Id = Guid.NewGuid(), OrganisationSubscriptionId = subscription.Id,
                Provider = SubscriptionProvider.Paystack, PaymentMethodType = SubscriptionPaymentMethodType.Card,
                MandateStatus = MandateStatus.Active, ProviderPaymentMethodReference = "secret-token",
                ProviderAuthorizationCode = "legacy-secret-token", MaskedDisplay = "Card ending 4242",
                IsDefault = true, IsReusable = true
            });
            await context.SaveChangesAsync();
        }

        var status = await sut.GetStatusAsync(seeded.StokvelId, seeded.AdminId);
        Assert.True(status.PaymentReady);
        Assert.Equal("Card ending 4242", status.MaskedDisplay);
        Assert.DoesNotContain(status.GetType().GetProperties(), property =>
            property.Name.Contains("Reference", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task MissingProviderConfiguration_FailsGracefully()
    {
        var seeded = await SeedAsync();
        provider.IsConfiguredValue = false;
        var result = await sut.StartAsync(seeded.StokvelId, seeded.AdminId, "billing@example.test", "Admin", null,
            "https://localhost/billing/payment-callback", "/subscription");
        Assert.False(result.Success);
        Assert.Contains("not configured", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<(Guid StokvelId, string AdminId)> SeedAsync()
    {
        await using var context = database.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var adminId = $"admin-{Guid.NewGuid():N}";
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = adminId;
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(), Code = $"plan-{Guid.NewGuid():N}", Name = "Test", MonthlyPrice = 100,
            IsActive = true, DisplayOrder = 1
        };
        context.SubscriptionPlans.Add(plan);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
        subscription.Provider = SubscriptionProvider.Paystack;
        subscription.TrialOptedIn = true;
        subscription.TrialStartedAt = DateTime.UtcNow;
        subscription.TrialEndsAt = DateTime.UtcNow.AddDays(60);
        await context.SaveChangesAsync();
        return (stokvel.Id, adminId);
    }

    private static string GetQueryValue(string url, string name)
    {
        var query = new Uri(url).Query.TrimStart('?').Split('&');
        var pair = query.Select(value => value.Split('=', 2)).Single(value => Uri.UnescapeDataString(value[0]) == name);
        return Uri.UnescapeDataString(pair[1]);
    }

    public void Dispose() => database.Dispose();

    private sealed class FakeSetupProvider : ISubscriptionPaymentProvider
    {
        public SubscriptionProvider Provider => SubscriptionProvider.Paystack;
        public string DisplayName => "Paystack";
        public bool IsConfiguredValue { get; set; } = true;
        public bool IsConfigured => IsConfiguredValue;
        public string UnavailableMessage => "Payment setup is not configured in this environment.";
        public PaymentSetupRequest? LastRequest { get; private set; }

        public Task<PaymentSetupResult> StartPaymentMethodSetupAsync(PaymentSetupRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new PaymentSetupResult(true, Provider, "https://provider.test/setup",
                "customer-safe-ref", request.CorrelationReference, null, null));
        }

        public Task<PaymentMethodStatusResult> GetPaymentMethodStatusAsync(string providerReference, CancellationToken ct = default) =>
            Task.FromResult(new PaymentMethodStatusResult(true, Provider, SubscriptionPaymentMethodType.Card,
                MandateStatus.Active, "safe-provider-token", providerReference, true,
                "Card ending 4242", "visa", "4242", 12, 2030, "Test Bank", null, null));

        public bool IsPaymentReady(SubscriptionPaymentMethod method) =>
            method.MandateStatus == MandateStatus.Active && method.IsReusable &&
            !string.IsNullOrWhiteSpace(method.ProviderPaymentMethodReference);
    }
}
