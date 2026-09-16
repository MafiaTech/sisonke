namespace Sisonke.Web.Services.Billing.Netcash;

public interface INetcashClient
{
    bool IsConfigured { get; }
    Task<NetcashProviderResult> AuthenticateAsync(NetcashAuthenticationRequest request, CancellationToken ct = default);
    Task<NetcashProviderResult> GetAuthenticationStatusAsync(string contractReference, CancellationToken ct = default);
    Task<NetcashProviderResult> CancelAuthenticationAsync(string contractReference, CancellationToken ct = default);
}

