namespace Sisonke.Web.Helpers;

public static class SouthAfricanLocations
{
    public static List<string> GetProvinces()
    {
        return
        [
            "Eastern Cape",
            "Free State",
            "Gauteng",
            "KwaZulu-Natal",
            "Limpopo",
            "Mpumalanga",
            "Northern Cape",
            "North West",
            "Western Cape"
        ];
    }

    public static List<string> GetTownsByProvince(string? province)
    {
        if (string.IsNullOrWhiteSpace(province))
        {
            return [];
        }

        return province switch
        {
            "Eastern Cape" =>
            [
                "East London",
                "Gqeberha",
                "Mthatha",
                "Queenstown",
                "King William's Town",
                "Butterworth"
            ],
            "Free State" =>
            [
                "Bloemfontein",
                "Welkom",
                "Bethlehem",
                "Kroonstad",
                "Sasolburg"
            ],
            "Gauteng" =>
            [
                "Johannesburg",
                "Pretoria",
                "Soweto",
                "Midrand",
                "Centurion",
                "Sandton",
                "Germiston",
                "Benoni",
                "Boksburg",
                "Vereeniging"
            ],
            "KwaZulu-Natal" =>
            [
                "Durban",
                "Pietermaritzburg",
                "Newcastle",
                "Richards Bay",
                "Empangeni",
                "Ladysmith",
                "Port Shepstone"
            ],
            "Limpopo" =>
            [
                "Polokwane",
                "Tzaneen",
                "Thohoyandou",
                "Lephalale",
                "Mokopane",
                "Makhado"
            ],
            "Mpumalanga" =>
            [
                "Mbombela",
                "Witbank",
                "Middelburg",
                "Secunda",
                "Ermelo",
                "Barberton"
            ],
            "Northern Cape" =>
            [
                "Kimberley",
                "Upington",
                "Springbok",
                "Kuruman",
                "De Aar"
            ],
            "North West" =>
            [
                "Mahikeng",
                "Rustenburg",
                "Potchefstroom",
                "Klerksdorp",
                "Brits",
                "Lichtenburg"
            ],
            "Western Cape" =>
            [
                "Cape Town",
                "Stellenbosch",
                "Paarl",
                "George",
                "Worcester",
                "Mossel Bay",
                "Knysna"
            ],
            _ => []
        };
    }
}
