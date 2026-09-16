using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Services.Billing;
using Sisonke.Web.Services.Billing.Paystack;

namespace Sisonke.Web.Tests.Billing;

public sealed class PaystackRegistrationTests
{
    private const string TestCredential = "unit-test-only-not-a-real-credential";

    [Theory]
    [InlineData(null, "")]
    [InlineData(false, "")]
    [InlineData(false, TestCredential)]
    public async Task ProductionHostStartsWithoutLiveGatewayWhenDisabled(bool? enabled, string secret)
    {
        var options = new PaystackOptions { Enabled = enabled, SecretKey = secret };
        var builder = CreateBuilder(options);
        builder.Services.AddPaystackBilling(options);
        using var host = builder.Build();
        await host.StartAsync();
        using var scope = host.Services.CreateScope();
        Assert.IsType<DisabledBillingProvider>(scope.ServiceProvider.GetRequiredService<IBillingProvider>());
        Assert.Null(scope.ServiceProvider.GetService<PaystackBillingProvider>());
        Assert.Empty(scope.ServiceProvider.GetServices<ISubscriptionPaymentProvider>());
        await host.StopAsync();
    }

    [Theory]
    [InlineData(true, "", "")]
    [InlineData(true, " ", "")]
    [InlineData(true, "\t", "")]
    [InlineData(null, "", "test-public-key")]
    public void EnabledOrPartiallyConfiguredProviderRequiresSecret(bool? enabled, string secret, string publicKey)
    {
        var options = new PaystackOptions { Enabled = enabled, SecretKey = secret, PublicKey = publicKey };
        var builder = CreateBuilder(options);
        var error = Assert.Throws<InvalidOperationException>(() => builder.Services.AddPaystackBilling(options));
        Assert.Contains("Paystack__SecretKey", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(true)]
    public void ConfiguredProviderRetainsLiveRegistration(bool? enabled)
    {
        var options = new PaystackOptions { Enabled = enabled, SecretKey = TestCredential };
        var builder = CreateBuilder(options);
        builder.Services.AddPaystackBilling(options);
        using var host = builder.Build();
        using var scope = host.Services.CreateScope();
        Assert.IsType<PaystackBillingProvider>(scope.ServiceProvider.GetRequiredService<IBillingProvider>());
        var setup = Assert.IsType<PaystackPaymentSetupProvider>(Assert.Single(scope.ServiceProvider.GetServices<ISubscriptionPaymentProvider>()));
        Assert.True(setup.IsConfigured);
    }

    [Fact]
    public void ExplicitConfigurationSwitchOverridesExistingCredentials()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Paystack:Enabled"] = "false",
            ["Paystack:SecretKey"] = TestCredential
        }).Build();
        var options = configuration.GetSection("Paystack").Get<PaystackOptions>()!;
        Assert.False(options.IsEnabled);
        var setup = new PaystackPaymentSetupProvider(new DisabledBillingProvider(), options,
            new SubscriptionPaymentOptions(), NullLogger<PaystackPaymentSetupProvider>.Instance);
        Assert.False(setup.IsConfigured);
    }

    [Fact]
    public async Task DisabledGatewayNeverReportsSuccessfulFinancialOperations()
    {
        IBillingProvider provider = new DisabledBillingProvider();
        Task[] operations =
        [
            provider.EnsureCustomerAsync(Guid.NewGuid(), "test@example.com", "Test", null),
            provider.EnsurePlanAsync("plan", "Plan", 100, "monthly"),
            provider.StartCardAuthorisationAsync("customer", "test@example.com", 100, "https://example.com"),
            provider.StartCardAuthorisationAsync("customer", "test@example.com", 100, "https://example.com", "reference"),
            provider.VerifyAuthorisationAsync("reference"),
            provider.RefundTransactionAsync("reference"),
            provider.CreateSubscriptionAsync("customer", "plan", "authorization", DateTime.UtcNow),
            provider.CancelSubscriptionAsync("subscription", "token"),
            provider.GetManagementLinkAsync("subscription"),
            provider.ChargeAuthorisationAsync("authorization", "test@example.com", 100, "reference")
        ];
        foreach (var operation in operations)
            await Assert.ThrowsAsync<HttpRequestException>(() => operation);
    }

    [Theory]
    [InlineData(false, StatusCodes.Status404NotFound)]
    [InlineData(true, StatusCodes.Status401Unauthorized)]
    public async Task WebhookRemainsClosedWithoutEnabledProviderAndValidSignature(bool enabled, int expected)
    {
        var endpoint = typeof(PaystackOptions).Assembly.GetType("PaystackWebhookEndpoint")!;
        var handler = endpoint.GetMethod("HandleAsync")!;
        // No database is supplied: disabled/unauthenticated calls must return before accessing it.
        var task = (Task<IResult>)handler.Invoke(null,
            [new DefaultHttpContext(), null,
             new PaystackOptions { Enabled = enabled, SecretKey = TestCredential },
             NullLoggerFactory.Instance, CancellationToken.None])!;
        var result = await task;
        Assert.Equal(expected, Assert.IsAssignableFrom<IStatusCodeHttpResult>(result).StatusCode);
    }

    private static HostApplicationBuilder CreateBuilder(PaystackOptions options)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = Environments.Production,
            DisableDefaults = true
        });
        builder.Services.AddLogging();
        builder.Services.AddSingleton(options);
        builder.Services.AddSingleton(new SubscriptionPaymentOptions());
        return builder;
    }
}
