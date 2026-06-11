namespace Sisonke.Web.Helpers;

public static class SensitiveDataMasker
{
    public static string MaskIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber))
            return "Not provided";

        var clean = idNumber.Trim();

        if (clean.Length <= 6)
            return "******";

        return $"{clean[..4]}******{clean[^4..]}";
    }

    public static string MaskPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return "Not provided";

        var clean = phoneNumber.Trim();

        if (clean.Length <= 6)
            return "*** *** ****";

        return $"{clean[..3]} *** {clean[^4..]}";
    }

    public static string MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Not provided";

        var clean = email.Trim();
        var parts = clean.Split('@');

        if (parts.Length != 2)
            return "***";

        var name = parts[0];
        var domain = parts[1];

        var visibleName = name.Length <= 3
            ? name[0] + "***"
            : name[..3] + "***";

        return $"{visibleName}@{domain}";
    }
}