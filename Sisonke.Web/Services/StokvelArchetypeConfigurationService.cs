using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

public class StokvelArchetypeConfigurationService
{
    public void ApplyDefaults(Stokvel stokvel, StokvelArchetype archetype)
    {
        stokvel.Archetype = archetype;

        stokvel.EnableClaims = false;
        stokvel.EnableDependents = false;
        stokvel.EnableRotation = false;
        stokvel.EnableLending = false;
        stokvel.EnableInventory = false;
        stokvel.EnableInvestmentTracking = false;
        stokvel.EnableEducationPayouts = false;
        stokvel.EnableTravelPlanning = false;
        stokvel.EnableSocialEvents = false;

        switch (archetype)
        {
            case StokvelArchetype.BurialSociety:
                stokvel.EnableClaims = true;
                stokvel.EnableDependents = true;
                break;
            case StokvelArchetype.Rotational:
                stokvel.EnableRotation = true;
                break;
            case StokvelArchetype.Grocery:
                stokvel.EnableInventory = true;
                break;
            case StokvelArchetype.InvestmentClub:
            case StokvelArchetype.SavingsClub:
            case StokvelArchetype.Education:
            case StokvelArchetype.Borrowing:
            case StokvelArchetype.Travel:
            case StokvelArchetype.SocialClub:
            default:
                break;
        }
    }
}
