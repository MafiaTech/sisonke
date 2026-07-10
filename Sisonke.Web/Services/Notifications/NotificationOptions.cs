namespace Sisonke.Web.Services.Notifications;

public class NotificationOptions
{
    public int DispatchIntervalSeconds { get; set; } = 30;
    public int MaxAttempts { get; set; } = 5;
    public int ReminderIntervalMinutes { get; set; } = 60;
}
