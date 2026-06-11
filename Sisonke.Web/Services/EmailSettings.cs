namespace Sisonke.Web.Services;

public class EmailSettings
{
    public string FromAddress { get; set; } = "noreply@sisonkestokvel.co.za";
    public string FromName { get; set; } = "Sisonke";
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public bool UseSsl { get; set; } = true;
}
