using System.Runtime.Serialization;
using System.ServiceModel;

namespace Sisonke.Web.Services.Billing.Netcash;

[ServiceContract(Namespace = "http://tempuri.org/")]
internal interface INetcashNiwsContract
{
    [OperationContract(Name = "DebiCheckAuthenticate", Action = "http://tempuri.org/INIWS_NIF/DebiCheckAuthenticate", ReplyAction = "http://tempuri.org/INIWS_NIF/DebiCheckAuthenticateResponse")]
    [return: MessageParameter(Name = "DebiCheckAuthenticateResult")]
    Task<NiwsDebiCheckAuthenticateResponse?> DebiCheckAuthenticateAsync(
        string ServiceKey,
        string AccountReference,
        string DebiCheckMandateTemplateId,
        bool IsIdNumber,
        string DebtorIdentification,
        string AccountName,
        string BankAccountName,
        string BranchCode,
        string BankAccountNumber,
        NiwsBankAccountType BankAccountType,
        string MobileNumber,
        string EmailAddress,
        decimal CollectionAmount,
        bool FirstCollectionDiffers,
        decimal? FirstCollectionAmount,
        string FirstCollectionDate,
        NiwsCollectionDayCode collectionDayCode);

    [OperationContract(Name = "DebiCheckAuthenticationCurrentStatus", Action = "http://tempuri.org/INIWS_NIF/DebiCheckAuthenticationCurrentStatus", ReplyAction = "http://tempuri.org/INIWS_NIF/DebiCheckAuthenticationCurrentStatusResponse")]
    [return: MessageParameter(Name = "DebiCheckAuthenticationCurrentStatusResult")]
    Task<NiwsDebiCheckStatusResponse?> DebiCheckAuthenticationCurrentStatusAsync(
        string ServiceKey,
        string ContractReference);

    [OperationContract(Name = "DebiCheckCancelAuthentication", Action = "http://tempuri.org/INIWS_NIF/DebiCheckCancelAuthentication", ReplyAction = "http://tempuri.org/INIWS_NIF/DebiCheckCancelAuthenticationResponse")]
    [return: MessageParameter(Name = "DebiCheckCancelAuthenticationResult")]
    Task<string?> DebiCheckCancelAuthenticationAsync(
        string ServiceKey,
        string ContractReference,
        NiwsCancellationReason reasonCode);
}

internal static class NiwsContractNamespaces
{
    public const string Data = "http://schemas.datacontract.org/2004/07/NC.DG.TMS.C.WCF.NIWS";
}

[DataContract(Name = "NIWSResponseContainer", Namespace = NiwsContractNamespaces.Data)]
internal class NiwsResponseContainer
{
    [DataMember(Order = 0)] public string? ErrorCode { get; set; }
}

[DataContract(Name = "DebiCheckAuthenticateResponse", Namespace = NiwsContractNamespaces.Data)]
internal sealed class NiwsDebiCheckAuthenticateResponse : NiwsResponseContainer
{
    [DataMember(Order = 1)] public string? BankResponseCode { get; set; }
    [DataMember(Order = 2)] public string? BankservResponseCode { get; set; }
    [DataMember(Order = 3)] public string? ClientResponseCode { get; set; }
    [DataMember(Order = 4)] public string? ContractReference { get; set; }
    [DataMember(Order = 5)] public string[]? Messages { get; set; }
    [DataMember(Order = 6)] public string? Status { get; set; }
}

[DataContract(Name = "DebiCheckAuthenticationCurrentStatusResponse", Namespace = NiwsContractNamespaces.Data)]
internal sealed class NiwsDebiCheckStatusResponse : NiwsResponseContainer
{
    [DataMember(Order = 1)] public string? CancellationReason { get; set; }
    [DataMember(Order = 2)] public string? ContractReference { get; set; }
    [DataMember(Order = 3)] public string? DateCancelled { get; set; }
    [DataMember(Order = 4)] public string? Status { get; set; }
    [DataMember(Order = 5)] public string? UpdateDate { get; set; }
    [DataMember(Order = 6)] public string? BankservResponse { get; set; }
    [DataMember(Order = 7)] public string? BankResponse { get; set; }
    [DataMember(Order = 8)] public string? ClientResponse { get; set; }
}

[DataContract(Name = "MandateOptions.BankAccountType", Namespace = NiwsContractNamespaces.Data)]
internal enum NiwsBankAccountType
{
    [EnumMember(Value = "Current")] Current = 1,
    [EnumMember(Value = "Savings")] Savings = 2,
    [EnumMember(Value = "Transmission")] Transmission = 3
}

[DataContract(Name = "DebiCheckOptions.CollectionFrequencyDayCodes", Namespace = NiwsContractNamespaces.Data)]
internal enum NiwsCollectionDayCode
{
    [EnumMember(Value = "MNTH_01")] Month01 = 22,
    [EnumMember(Value = "MNTH_02")] Month02 = 23,
    [EnumMember(Value = "MNTH_03")] Month03 = 24,
    [EnumMember(Value = "MNTH_04")] Month04 = 25,
    [EnumMember(Value = "MNTH_05")] Month05 = 26,
    [EnumMember(Value = "MNTH_06")] Month06 = 27,
    [EnumMember(Value = "MNTH_07")] Month07 = 28,
    [EnumMember(Value = "MNTH_08")] Month08 = 29,
    [EnumMember(Value = "MNTH_09")] Month09 = 30,
    [EnumMember(Value = "MNTH_10")] Month10 = 31,
    [EnumMember(Value = "MNTH_11")] Month11 = 32,
    [EnumMember(Value = "MNTH_12")] Month12 = 33,
    [EnumMember(Value = "MNTH_13")] Month13 = 34,
    [EnumMember(Value = "MNTH_14")] Month14 = 35,
    [EnumMember(Value = "MNTH_15")] Month15 = 36,
    [EnumMember(Value = "MNTH_16")] Month16 = 37,
    [EnumMember(Value = "MNTH_17")] Month17 = 38,
    [EnumMember(Value = "MNTH_18")] Month18 = 39,
    [EnumMember(Value = "MNTH_19")] Month19 = 40,
    [EnumMember(Value = "MNTH_20")] Month20 = 41,
    [EnumMember(Value = "MNTH_21")] Month21 = 42,
    [EnumMember(Value = "MNTH_22")] Month22 = 43,
    [EnumMember(Value = "MNTH_23")] Month23 = 44,
    [EnumMember(Value = "MNTH_24")] Month24 = 45,
    [EnumMember(Value = "MNTH_25")] Month25 = 46,
    [EnumMember(Value = "MNTH_26")] Month26 = 47,
    [EnumMember(Value = "MNTH_27")] Month27 = 48,
    [EnumMember(Value = "MNTH_28")] Month28 = 49,
    [EnumMember(Value = "MNTH_29")] Month29 = 50,
    [EnumMember(Value = "MNTH_30")] Month30 = 51,
    [EnumMember(Value = "MNTH_31")] Month31 = 52
}

[DataContract(Name = "DebiCheckOptions.CancellationReasonCodes", Namespace = NiwsContractNamespaces.Data)]
internal enum NiwsCancellationReason
{
    // CUST is documented for cancellation requested by the customer and is stable in the live WSDL.
    [EnumMember(Value = "CUST")] CustomerRequested = 4
}
