using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Billing;

public sealed record PaymentSetupState(
    Guid StokvelId,
    Guid SubscriptionId,
    string ActorUserId,
    SubscriptionProvider Provider,
    string CorrelationReference,
    string RedirectPath);

public interface IPaymentSetupStateProtector
{
    string Protect(PaymentSetupState state);
    bool TryUnprotect(string protectedState, out PaymentSetupState? state);
}

public sealed class PaymentSetupStateProtector(IDataProtectionProvider provider) : IPaymentSetupStateProtector
{
    private readonly ITimeLimitedDataProtector protector = provider
        .CreateProtector("Sisonke.SubscriptionPaymentSetup.State.v1")
        .ToTimeLimitedDataProtector();

    public string Protect(PaymentSetupState state) =>
        protector.Protect(JsonSerializer.Serialize(state), TimeSpan.FromMinutes(30));

    public bool TryUnprotect(string protectedState, out PaymentSetupState? state)
    {
        state = null;
        try
        {
            state = JsonSerializer.Deserialize<PaymentSetupState>(protector.Unprotect(protectedState));
            return state is not null;
        }
        catch (Exception ex) when (ex is CryptographicException or JsonException)
        {
            return false;
        }
    }
}
