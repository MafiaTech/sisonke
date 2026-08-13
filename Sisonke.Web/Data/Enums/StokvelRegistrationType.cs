namespace Sisonke.Web.Data.Enums;

/// <summary>
/// The stokvel's legal registration status, captured during onboarding (Phase 5 brief, Step 1
/// "registration type") — distinct from StokvelArchetype ("stokvel category", e.g. burial society
/// vs rotational), which describes how the group operates, not its legal standing.
/// </summary>
public enum StokvelRegistrationType
{
    NotRegistered = 1,
    VoluntaryAssociation = 2,
    NonProfitOrganisation = 3,
    Trust = 4,
    Cooperative = 5,
    Other = 6
}
