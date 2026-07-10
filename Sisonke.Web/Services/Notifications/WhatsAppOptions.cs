namespace Sisonke.Web.Services.Notifications;

public class WhatsAppOptions
{
    public bool Enabled { get; set; }

    public string MinutesPublishedTemplateName { get; set; } = "sisonke_minutes_published";
}
