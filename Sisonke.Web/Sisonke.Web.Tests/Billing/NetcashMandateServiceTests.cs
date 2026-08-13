using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Services.Billing.Netcash;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

public sealed class NetcashMandateServiceTests : IDisposable
{
    private const string FullAccountNumber = "1234567890123456";
    private const string IdentityNumber = "9001015009087";
    private readonly SqliteTestDatabase database = new();
    private readonly TestDbContextFactory dbFactory;
    private readonly FakeNetcashClient client = new();
    private readonly CaptureLogger logger = new();
    private readonly FakeTimeProvider clock = new(new DateTimeOffset(2026, 8, 13, 8, 0, 0, TimeSpan.Zero));
    private readonly NetcashMandateService sut;

    public NetcashMandateServiceTests()
    {
        dbFactory = new(database);
        sut = new(dbFactory, new MemberAccessService(database.CreateContext()), new StokvelOperationLock(),
            client, ConfiguredOptions(), clock, logger);
    }

    [Fact]
    public async Task ValidSetup_PersistsOnlySafeMetadataAndConsent_WithoutCreatingPayment()
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = new(true, "000", "Pending", "NC000000000001", "Awaiting bank authentication.");

        var result = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());

        Assert.True(result.Success);
        Assert.Equal(MandateStatus.Pending, result.MandateStatus);
        Assert.False(result.PaymentReady);
        Assert.NotNull(client.LastAuthenticationRequest);
        Assert.Equal(249m, client.LastAuthenticationRequest!.CollectionAmount);
        Assert.Equal(clock.GetUtcNow().UtcDateTime.AddDays(60).Date,
            client.LastAuthenticationRequest.FirstCollectionDate.ToDateTime(TimeOnly.MinValue));

        await using var verify = database.CreateContext();
        var subscription = await verify.OrganisationSubscriptions.Include(value => value.PaymentMethods).SingleAsync();
        var method = Assert.Single(subscription.PaymentMethods);
        Assert.Equal(SubscriptionProvider.Netcash, subscription.Provider);
        Assert.Equal(SubscriptionPaymentMethodType.DebiCheck, method.PaymentMethodType);
        Assert.Equal("NC000000000001", method.ProviderMandateReference);
        Assert.Equal("Bank account ending 3456", method.MaskedDisplay);
        Assert.Equal(BillingTermsVersion.Current, subscription.TermsVersion);
        Assert.Equal(seeded.AdminId, subscription.TermsAcceptedByUserId);
        Assert.NotNull(subscription.TermsAcceptedAt);
        Assert.Empty(await verify.SubscriptionPayments.ToListAsync());
        Assert.DoesNotContain(FullAccountNumber, PersistedStrings(subscription));
        Assert.DoesNotContain(IdentityNumber, PersistedStrings(subscription));
        Assert.Equal(SubscriptionStatus.Trialing, subscription.Status);
    }

    [Fact]
    public async Task UnauthorizedStokvelSetup_IsRejectedBeforeProviderCall()
    {
        var first = await SeedAsync();
        var second = await SeedAsync();

        var result = await sut.SubmitAsync(second.StokvelId, first.AdminId, ValidRequest());

        Assert.False(result.Success);
        Assert.Equal(0, client.AuthenticateCalls);
    }

    [Fact]
    public async Task SetupRequiresSisonkeSubscription_EvenWhenStokvelAndOfficeBearerExist()
    {
        await using var context = database.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = $"admin-{Guid.NewGuid():N}";
        await context.SaveChangesAsync();

        var result = await sut.SubmitAsync(stokvel.Id, admin.ApplicationUserId, ValidRequest());

        Assert.False(result.Success);
        Assert.Contains("subscription plan", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, client.AuthenticateCalls);
        Assert.Empty(await context.SubscriptionPaymentMethods.ToListAsync());
    }

    [Fact]
    public async Task MissingConfigurationAndTimeout_FailSafely()
    {
        var seeded = await SeedAsync();
        client.IsConfiguredValue = false;
        var missing = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());
        Assert.False(missing.Success);
        Assert.Contains("configuration", missing.Message, StringComparison.OrdinalIgnoreCase);

        client.IsConfiguredValue = true;
        client.AuthenticationResult = NetcashProviderResult.Failed("timeout", "Netcash did not respond in time. Please try again.");
        var timeout = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());
        Assert.False(timeout.Success);
        Assert.Equal(MandateStatus.Failed, timeout.MandateStatus);
        Assert.DoesNotContain(logger.Messages, value => value.Contains(FullAccountNumber, StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Messages, value => value.Contains(IdentityNumber, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Pending", MandateStatus.Pending, false)]
    [InlineData("Accepted", MandateStatus.Active, true)]
    [InlineData("Rejected", MandateStatus.Failed, false)]
    [InlineData("Cancelled", MandateStatus.Revoked, false)]
    [InlineData("Expired", MandateStatus.Expired, false)]
    [InlineData("FutureProviderState", MandateStatus.Pending, false)]
    public async Task DocumentedStatuses_MapConservatively(string providerStatus, MandateStatus expected, bool ready)
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = new(true, "000", providerStatus, $"NC{Guid.NewGuid():N}", "Status returned.");

        var result = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());

        Assert.Equal(expected, result.MandateStatus);
        Assert.Equal(ready, result.PaymentReady);
    }

    [Fact]
    public async Task ProviderRejection_PersistsFailedAndDoesNotBecomeReady()
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = NetcashProviderResult.Failed("203", "The DebiCheck authentication request failed.");

        var result = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());

        Assert.False(result.Success);
        Assert.Equal(MandateStatus.Failed, result.MandateStatus);
        await using var verify = database.CreateContext();
        Assert.Equal(MandateStatus.Failed, (await verify.SubscriptionPaymentMethods.SingleAsync()).MandateStatus);
    }

    [Fact]
    public async Task DuplicateSubmit_DoesNotCreateAnotherProviderRequestOrReference()
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = new(true, "000", "Pending", "NC000000000002", "Pending.");

        var first = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());
        var second = await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.Equal(1, client.AuthenticateCalls);
        await using var verify = database.CreateContext();
        Assert.Equal(1, await verify.SubscriptionPaymentMethods.CountAsync());
    }

    [Fact]
    public async Task Refresh_UsesStoredReferenceAndUpdatesMandateState()
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = new(true, "000", "Pending", "NC000000000003", "Pending.");
        await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());
        client.StatusResult = new(true, "000", "Accepted", "NC000000000003", "Accepted.");

        var refreshed = await sut.RefreshAsync(seeded.StokvelId, seeded.AdminId);

        Assert.True(refreshed.Success);
        Assert.True(refreshed.PaymentReady);
        Assert.Equal("NC000000000003", client.LastStatusReference);
        await using var verify = database.CreateContext();
        var method = await verify.SubscriptionPaymentMethods.SingleAsync();
        Assert.Equal(MandateStatus.Active, method.MandateStatus);
        Assert.NotNull(method.MandateActivatedAt);
    }

    [Fact]
    public async Task Cancellation_IsSubmittedThenTraceConfirmsRevoked_AndIsIdempotent()
    {
        var seeded = await SeedAsync();
        client.AuthenticationResult = new(true, "000", "Accepted", "NC000000000004", "Accepted.");
        await sut.SubmitAsync(seeded.StokvelId, seeded.AdminId, ValidRequest());
        client.CancellationResult = new(true, "000", "CancellationSubmitted", "NC000000000004", "Submitted.");

        var cancelled = await sut.CancelAsync(seeded.StokvelId, seeded.AdminId);
        var duplicate = await sut.CancelAsync(seeded.StokvelId, seeded.AdminId);

        Assert.True(cancelled.Success);
        Assert.Equal(MandateStatus.Pending, cancelled.MandateStatus);
        Assert.False(cancelled.PaymentReady);
        Assert.False(duplicate.Success);
        Assert.Equal(1, client.CancelCalls);

        client.StatusResult = new(true, "000", "Cancelled", "NC000000000004", "Cancelled.");
        var traced = await sut.RefreshAsync(seeded.StokvelId, seeded.AdminId);
        Assert.Equal(MandateStatus.Revoked, traced.MandateStatus);
        Assert.False(traced.PaymentReady);
    }

    [Fact]
    public void UserFacingResultModels_ExposeNoProviderReferenceOrBankFields()
    {
        var properties = typeof(NetcashMandateActionResult).GetProperties().Select(value => value.Name).ToList();
        Assert.DoesNotContain(properties, value => value.Contains("Reference", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, value => value.Contains("Account", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(properties, value => value.Contains("Token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ConfigurationAndCorrelation_RequireSafeDeterministicInputs()
    {
        var subscriptionId = Guid.NewGuid();
        Assert.Equal(NetcashMandateService.BuildAccountReference(subscriptionId),
            NetcashMandateService.BuildAccountReference(subscriptionId));
        Assert.Equal(22, NetcashMandateService.BuildAccountReference(subscriptionId).Length);
        Assert.StartsWith("SK", NetcashMandateService.BuildAccountReference(subscriptionId), StringComparison.Ordinal);

        var options = ConfiguredOptions();
        Assert.True(options.IsConfigured);
        options.RequestTimeoutSeconds = 179;
        Assert.False(options.IsConfigured);
        options.RequestTimeoutSeconds = 180;
        options.ServiceUrl = "http://ws.netcash.co.za/NIWS/NIWS_NIF.svc";
        Assert.False(options.IsConfigured);
    }

    private async Task<(Guid StokvelId, string AdminId)> SeedAsync()
    {
        await using var context = database.CreateContext();
        var stokvel = TestData.CreateStokvel(context);
        var admin = TestData.CreateStokvelMember(context, stokvel, role: SisonkeRole.StokvelAdmin);
        admin.ApplicationUserId = $"admin-{Guid.NewGuid():N}";
        var plan = new SubscriptionPlan
        {
            Id = Guid.NewGuid(), Code = $"plan-{Guid.NewGuid():N}", Name = "Growing Society",
            MonthlyPrice = 249m, IsActive = true, DisplayOrder = 1
        };
        context.SubscriptionPlans.Add(plan);
        var subscription = TestData.CreateOrganisationSubscription(context, stokvel, plan.Id, SubscriptionStatus.Trialing);
        subscription.TrialOptedIn = true;
        subscription.TrialStartedAt = clock.GetUtcNow().UtcDateTime;
        subscription.TrialEndsAt = clock.GetUtcNow().UtcDateTime.AddDays(60);
        await context.SaveChangesAsync();
        return (stokvel.Id, admin.ApplicationUserId!);
    }

    private static NetcashDebiCheckMandateRequest ValidRequest() => new(
        "Test Account Holder", true, IdentityNumber, "Test Account Holder", "250655",
        FullAccountNumber, NetcashBankAccountType.Current, "0821234567", "billing@example.test",
        true, BillingTermsVersion.Current);

    private static NetcashOptions ConfiguredOptions() => new()
    {
        Enabled = true,
        DebitOrderServiceKey = "test-service-key",
        DebiCheckMandateTemplateId = "test-template",
        RequestTimeoutSeconds = 180
    };

    private static IEnumerable<string> PersistedStrings(OrganisationSubscription subscription) =>
        subscription.GetType().GetProperties().Where(value => value.PropertyType == typeof(string))
            .Select(value => value.GetValue(subscription) as string)
            .Concat(subscription.PaymentMethods.SelectMany(method => method.GetType().GetProperties()
                .Where(value => value.PropertyType == typeof(string)).Select(value => value.GetValue(method) as string)))
            .Where(value => value is not null)!;

    public void Dispose() => database.Dispose();

    private sealed class FakeNetcashClient : INetcashClient
    {
        public bool IsConfiguredValue { get; set; } = true;
        public bool IsConfigured => IsConfiguredValue;
        public int AuthenticateCalls { get; private set; }
        public int CancelCalls { get; private set; }
        public NetcashAuthenticationRequest? LastAuthenticationRequest { get; private set; }
        public string? LastStatusReference { get; private set; }
        public NetcashProviderResult AuthenticationResult { get; set; } = new(true, "000", "Pending", "NC-DEFAULT", "Pending.");
        public NetcashProviderResult StatusResult { get; set; } = new(true, "000", "Pending", "NC-DEFAULT", "Pending.");
        public NetcashProviderResult CancellationResult { get; set; } = new(true, "000", "CancellationSubmitted", "NC-DEFAULT", "Submitted.");

        public Task<NetcashProviderResult> AuthenticateAsync(NetcashAuthenticationRequest request, CancellationToken ct = default)
        {
            AuthenticateCalls++;
            LastAuthenticationRequest = request;
            return Task.FromResult(AuthenticationResult);
        }

        public Task<NetcashProviderResult> GetAuthenticationStatusAsync(string contractReference, CancellationToken ct = default)
        {
            LastStatusReference = contractReference;
            return Task.FromResult(StatusResult);
        }

        public Task<NetcashProviderResult> CancelAuthenticationAsync(string contractReference, CancellationToken ct = default)
        {
            CancelCalls++;
            return Task.FromResult(CancellationResult);
        }
    }

    private sealed class CaptureLogger : ILogger<NetcashMandateService>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => Messages.Add(formatter(state, exception));
    }
}
