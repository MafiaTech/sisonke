using System.Globalization;

namespace Sisonke.Web.Services;

public static class WholeRandAmount
{
    public static bool IsDigits(string value) => value.All(c => c is >= '0' and <= '9');

    public static bool TryParse(string value, out decimal amount)
    {
        amount = 0;
        return value.Length is > 0 and <= 16 && IsDigits(value) &&
            decimal.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out amount) && amount > 0;
    }
}
