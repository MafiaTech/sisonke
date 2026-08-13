using System.ServiceModel;

namespace Sisonke.Web.Services.Billing.Netcash;

/// <summary>
/// Typed SOAP 1.1 client for the official Netcash NIWS_NIF DebiCheck operations.
/// Contract: https://api.netcash.co.za/inbound-payments/dc/debicheck-tt1-synchronous/
/// Trace: https://api.netcash.co.za/inbound-payments/dc/dctrace/
/// Cancel: https://api.netcash.co.za/inbound-payments/dc/debicheckcancelauthentication/
/// </summary>
public sealed class NetcashClient(NetcashOptions options, ILogger<NetcashClient> logger) : INetcashClient
{
    public bool IsConfigured => options.IsConfigured;

    public async Task<NetcashProviderResult> AuthenticateAsync(NetcashAuthenticationRequest request, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return NetcashProviderResult.Failed("not_configured", "Netcash DebiCheck setup is not configured.");
        }

        return await InvokeAsync(async channel =>
        {
            var response = await channel.DebiCheckAuthenticateAsync(
                options.DebitOrderServiceKey!, request.AccountReference,
                options.DebiCheckMandateTemplateId!, request.IsSouthAfricanId,
                request.DebtorIdentification, request.AccountHolderName, request.BankAccountName,
                request.BranchCode, request.BankAccountNumber, (NiwsBankAccountType)request.BankAccountType,
                request.MobileNumber, request.EmailAddress, request.CollectionAmount,
                false, null, request.FirstCollectionDate.ToString("yyyyMMdd"),
                ToMonthlyCollectionDay(request.MonthlyCollectionDay)).WaitAsync(ct);

            if (response is null)
            {
                return NetcashProviderResult.Failed("malformed_response", "Netcash returned an invalid response.");
            }

            var success = string.Equals(response.ErrorCode, "000", StringComparison.Ordinal);
            return new(success, response.ErrorCode, response.Status, response.ContractReference,
                success ? SafeStatusMessage(response.Status) : SafeErrorMessage(response.ErrorCode));
        }, "authenticate", ct);
    }

    public Task<NetcashProviderResult> GetAuthenticationStatusAsync(string contractReference, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return Task.FromResult(NetcashProviderResult.Failed("not_configured", "Netcash DebiCheck status is not configured."));
        }

        return InvokeAsync(async channel =>
        {
            var response = await channel.DebiCheckAuthenticationCurrentStatusAsync(
                options.DebitOrderServiceKey!, contractReference).WaitAsync(ct);
            if (response is null)
            {
                return NetcashProviderResult.Failed("malformed_response", "Netcash returned an invalid status response.");
            }

            var success = string.Equals(response.ErrorCode, "000", StringComparison.Ordinal);
            return new(success, response.ErrorCode, response.Status, response.ContractReference,
                success ? SafeStatusMessage(response.Status) : SafeErrorMessage(response.ErrorCode));
        }, "trace", ct);
    }

    public Task<NetcashProviderResult> CancelAuthenticationAsync(string contractReference, CancellationToken ct = default)
    {
        if (!IsConfigured)
        {
            return Task.FromResult(NetcashProviderResult.Failed("not_configured", "Netcash DebiCheck cancellation is not configured."));
        }

        return InvokeAsync(async channel =>
        {
            var raw = await channel.DebiCheckCancelAuthenticationAsync(
                options.DebitOrderServiceKey!, contractReference, NiwsCancellationReason.CustomerRequested).WaitAsync(ct);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return NetcashProviderResult.Failed("malformed_response", "Netcash returned an invalid cancellation response.");
            }

            var code = raw.Split('|', 2, StringSplitOptions.TrimEntries)[0];
            var success = string.Equals(code, "000", StringComparison.Ordinal);
            return new(success, code, success ? "CancellationSubmitted" : null, contractReference,
                success ? "Mandate cancellation was submitted to the bank." : SafeErrorMessage(code));
        }, "cancel", ct);
    }

    private async Task<NetcashProviderResult> InvokeAsync(
        Func<INetcashNiwsContract, Task<NetcashProviderResult>> operation,
        string operationName,
        CancellationToken ct)
    {
        var timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
        var binding = new BasicHttpBinding(BasicHttpSecurityMode.Transport)
        {
            OpenTimeout = timeout,
            CloseTimeout = TimeSpan.FromSeconds(30),
            SendTimeout = timeout,
            ReceiveTimeout = timeout,
            MaxReceivedMessageSize = 1_048_576
        };
        var factory = new ChannelFactory<INetcashNiwsContract>(binding, new EndpointAddress(options.ServiceUrl));
        var channel = factory.CreateChannel();
        var client = (IClientChannel)channel;

        try
        {
            return await operation(channel);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Netcash {Operation} timed out.", operationName);
            return NetcashProviderResult.Failed("timeout", "Netcash did not respond in time. Please try again.");
        }
        catch (TimeoutException)
        {
            logger.LogWarning("Netcash {Operation} timed out.", operationName);
            return NetcashProviderResult.Failed("timeout", "Netcash did not respond in time. Please try again.");
        }
        catch (CommunicationException ex)
        {
            logger.LogWarning("Netcash {Operation} transport failed with {ExceptionType}.", operationName, ex.GetType().Name);
            return NetcashProviderResult.Failed("provider_unavailable", "Netcash is temporarily unavailable. Please try again.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError("Netcash {Operation} returned an unexpected {ExceptionType}.", operationName, ex.GetType().Name);
            return NetcashProviderResult.Failed("provider_error", "Netcash could not process the request. Please try again.");
        }
        finally
        {
            try
            {
                if (client.State == CommunicationState.Faulted) client.Abort(); else client.Close();
                factory.Close();
            }
            catch (CommunicationException)
            {
                client.Abort();
                factory.Abort();
            }
        }
    }

    private static NiwsCollectionDayCode ToMonthlyCollectionDay(int day)
    {
        if (day is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(day));
        return (NiwsCollectionDayCode)(21 + day);
    }

    private static string SafeStatusMessage(string? status) => status?.Trim().ToUpperInvariant() switch
    {
        "ACCEPTED" => "The DebiCheck mandate was accepted.",
        "REJECTED" => "The bank rejected the DebiCheck mandate.",
        "CANCELLED" or "CANCELED" => "The DebiCheck mandate was cancelled.",
        "EXPIRED" => "The DebiCheck authentication expired.",
        _ => "The DebiCheck request was submitted and is awaiting bank authentication."
    };

    private static string SafeErrorMessage(string? code) => code switch
    {
        "100" => "Netcash authentication failed. Please contact support.",
        "202" => "The Netcash mandate could not be found.",
        "203" => "The DebiCheck authentication request failed.",
        "325" => "The selected mandate template does not support real-time DebiCheck.",
        "326" => "The mandate cannot be cancelled in its current state.",
        _ => "Netcash could not process the request. Please try again."
    };
}
