using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Sisonke.Web.Helpers;

/// <summary>
/// Validates a South African ID number: 13 digits, valid YYMMDD birth date,
/// citizenship digit 0 or 1, and passing Luhn checksum.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SaIdNumberAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var id = (value as string)?.Trim();

        if (string.IsNullOrWhiteSpace(id))
            return ValidationResult.Success; // [Required] handles the empty case

        if (!id.All(char.IsDigit))
            return new ValidationResult("ID number must contain digits only.");

        if (id.Length != 13)
            return new ValidationResult("ID number must be exactly 13 digits.");

        if (id.All(c => c == '0'))
            return new ValidationResult("Enter a valid South African ID number.");

        // Validate YYMMDD birth date (positions 0–5)
        var yy = int.Parse(id.Substring(0, 2));
        var mm = int.Parse(id.Substring(2, 2));
        var dd = int.Parse(id.Substring(4, 2));
        var year = yy <= (DateTime.Today.Year % 100) ? 2000 + yy : 1900 + yy;

        if (!IsValidDate(year, mm, dd))
            return new ValidationResult("Enter a valid South African ID number.");

        // Citizenship digit (position 10): 0 = SA citizen, 1 = permanent resident
        if (id[10] != '0' && id[10] != '1')
            return new ValidationResult("Enter a valid South African ID number.");

        // Standard Luhn checksum
        if (!LuhnCheck(id))
            return new ValidationResult("The ID number checksum is invalid.");

        return ValidationResult.Success;
    }

    private static bool IsValidDate(int year, int month, int day)
    {
        try { _ = new DateTime(year, month, day); return true; }
        catch { return false; }
    }

    private static bool LuhnCheck(string id)
    {
        int sum = 0;
        bool doubleIt = false;
        for (int i = id.Length - 1; i >= 0; i--)
        {
            int digit = id[i] - '0';
            if (doubleIt)
            {
                digit *= 2;
                if (digit > 9) digit -= 9;
            }
            sum += digit;
            doubleIt = !doubleIt;
        }
        return sum % 10 == 0;
    }
}

/// <summary>
/// Validates a South African cellphone number.
/// Accepts 9 digits (subscriber number), 10 digits starting with 0,
/// or +27 followed by 9 digits. Rejects all-zeros and non-digit input.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SaCellphoneAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var phone = (value as string)?.Trim();

        if (string.IsNullOrWhiteSpace(phone))
            return ValidationResult.Success; // [Required] handles the empty case

        // Strip recognised prefixes to get the 9-digit subscriber number
        string digits;
        if (phone.StartsWith("+27"))
            digits = phone.Substring(3);
        else if (phone.StartsWith("0"))
            digits = phone.Substring(1);
        else
            digits = phone;

        if (!digits.All(char.IsDigit))
            return new ValidationResult("Enter a valid South African cellphone number.");

        if (digits.Length != 9)
            return new ValidationResult("Use 9 digits after +27, for example 712345678.");

        if (digits.All(c => c == '0'))
            return new ValidationResult("Cellphone number cannot be all zeros.");

        return ValidationResult.Success;
    }
}

/// <summary>
/// Validates an email address and requires the domain to contain a dot,
/// rejecting addresses like Test@test that the built-in [EmailAddress] passes.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SaEmailWithDomainAttribute : ValidationAttribute
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var email = (value as string)?.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return ValidationResult.Success; // [Required] handles the empty case

        if (!EmailRegex.IsMatch(email))
            return new ValidationResult("Enter a valid email address.");

        return ValidationResult.Success;
    }
}
