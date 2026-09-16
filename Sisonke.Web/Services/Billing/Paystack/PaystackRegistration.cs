using Polly;

namespace Sisonke.Web.Services.Billing.Paystack;

public static class PaystackRegistration
{
    public static IServiceCollection AddPaystackBilling(this IServiceCollection services, PaystackOptions options)
    {
        if (!options.IsEnabled)
        {
            // Keep shared subscription services resolvable without registering a live gateway.
            services.AddScoped<IBillingProvider, DisabledBillingProvider>();
            return services;
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
            throw new InvalidOperationException(
                "Paystack is enabled but Paystack:SecretKey is not configured. Set Paystack__SecretKey " +
                "via Azure App Service configuration or Key Vault, or disable Paystack with Paystack__Enabled=false.");

        services.AddHttpClient<PaystackBillingProvider>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.SecretKey);
            })
            // Preserve the existing protection against bearer-secret header logging.
            .RemoveAllLoggers()
            .AddPolicyHandler(Polly.Extensions.Http.HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TaskCanceledException>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));
        services.AddScoped<IBillingProvider>(sp => sp.GetRequiredService<PaystackBillingProvider>());
        services.AddScoped<ISubscriptionPaymentProvider, PaystackPaymentSetupProvider>();
        return services;
    }
}
