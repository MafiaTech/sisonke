using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Data;

/// <summary>Shared monthly-contribution presentation; never changes stored financial values.</summary>
public static class ContributionStatus
{
    public static bool NoPaymentDue(decimal expected, decimal outstanding) => expected <= 0 && outstanding <= 0;

    public static PaymentStatus Effective(PaymentStatus status, decimal outstanding, DateTime dueDate)
    {
        if (status is PaymentStatus.Exempted or PaymentStatus.WrittenOff or PaymentStatus.Reversed) return status;
        if (outstanding <= 0) return PaymentStatus.Paid;
        return dueDate.Date < DateTime.Today && status is PaymentStatus.Unpaid or PaymentStatus.PartiallyPaid
            ? PaymentStatus.Late : status;
    }

    public static string Label(PaymentStatus status, decimal expected, decimal outstanding, DateTime dueDate)
    {
        if (NoPaymentDue(expected, outstanding)) return "No payment due";
        return Effective(status, outstanding, dueDate) switch
        {
            PaymentStatus.Late => "Overdue",
            PaymentStatus.PartiallyPaid => "Partial",
            var effective => effective.ToString()
        };
    }

    public static string MemberMessage(decimal expected, decimal outstanding, bool hasOverdueContributions) =>
        hasOverdueContributions ? "You have outstanding contributions from earlier or overdue periods."
        : NoPaymentDue(expected, outstanding) ? "No payment due"
        : outstanding <= 0 ? "You are up to date with your contributions. Thank you!"
        : "You have an outstanding contribution for this month.";

    public static string BadgeClass(PaymentStatus status, decimal expected, decimal outstanding, DateTime dueDate) =>
        "sisonke-badge sisonke-badge-" + (NoPaymentDue(expected, outstanding) ? "gray" : Effective(status, outstanding, dueDate) switch
        {
            PaymentStatus.Paid => "green", PaymentStatus.PartiallyPaid => "gold", PaymentStatus.Late => "red", _ => "gray"
        });
}
