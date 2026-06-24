using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Sisonke.Web.Data;

namespace Sisonke.Web.Services;

public class SisonkeEmailSender : IEmailSender<ApplicationUser>, IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<SisonkeEmailSender> _logger;

    public bool IsSmtpConfigured => !string.IsNullOrWhiteSpace(_settings.SmtpHost);

    public SisonkeEmailSender(EmailSettings settings, IWebHostEnvironment env, ILogger<SisonkeEmailSender> logger)
    {
        _settings = settings;
        _env = env;
        _logger = logger;
    }

    // ── IEmailSender<ApplicationUser> ───────────────────────────────────────

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var subject = "Confirm your Sisonke account";
        var body = BuildEmailBody(
            heading: "Confirm your email address",
            previewText: "You're almost there — confirm your email to activate your Sisonke account.",
            bodyHtml: $@"
                <p style='margin:0 0 16px;color:#3D3530;font-size:15px;line-height:1.6;'>
                    Welcome to Sisonke! Please confirm your email address by clicking the button below.
                    Your account will be fully activated once confirmed.
                </p>
                <a href='{confirmationLink}'
                   style='display:inline-block;background:#2D6A4F;color:#ffffff;text-decoration:none;
                          padding:14px 32px;border-radius:8px;font-size:15px;font-weight:600;margin:8px 0 24px;'>
                    Confirm My Email
                </a>
                <p style='margin:0;color:#7A6E69;font-size:13px;'>
                    If you did not create a Sisonke account, you can safely ignore this email.
                    This link expires in 24 hours.
                </p>",
            linkUrl: confirmationLink,
            linkText: "Confirm My Email");

        return SendAsync(email, subject, body, confirmationLink);
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var subject = "Reset your Sisonke password";
        var body = BuildEmailBody(
            heading: "Reset your password",
            previewText: "We received a request to reset your Sisonke password.",
            bodyHtml: $@"
                <p style='margin:0 0 16px;color:#3D3530;font-size:15px;line-height:1.6;'>
                    We received a request to reset the password for your Sisonke account.
                    Click the button below to set a new password.
                </p>
                <a href='{resetLink}'
                   style='display:inline-block;background:#2D6A4F;color:#ffffff;text-decoration:none;
                          padding:14px 32px;border-radius:8px;font-size:15px;font-weight:600;margin:8px 0 24px;'>
                    Reset My Password
                </a>
                <p style='margin:0;color:#7A6E69;font-size:13px;'>
                    If you did not request a password reset, you can safely ignore this email.
                    This link expires in 1 hour. Do not share this link with anyone.
                </p>",
            linkUrl: resetLink,
            linkText: "Reset My Password");

        return SendAsync(email, subject, body, resetLink);
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var subject = "Your Sisonke password reset code";
        var body = BuildEmailBody(
            heading: "Password reset code",
            previewText: "Your Sisonke password reset code.",
            bodyHtml: $@"
                <p style='margin:0 0 16px;color:#3D3530;font-size:15px;line-height:1.6;'>
                    Use the code below to reset your Sisonke account password.
                </p>
                <div style='background:#F5F0EA;border-radius:8px;padding:20px 24px;margin:16px 0 24px;
                            text-align:center;font-family:monospace;font-size:28px;font-weight:700;
                            letter-spacing:6px;color:#2D6A4F;'>
                    {resetCode}
                </div>
                <p style='margin:0;color:#7A6E69;font-size:13px;'>
                    This code expires in 15 minutes. If you did not request a reset, ignore this email.
                </p>",
            linkUrl: null,
            linkText: null);

        return SendAsync(email, subject, body);
    }

    // ── IEmailSender (non-generic) ────────────────────────────────────────

    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => SendAsync(email, subject, htmlMessage);

    // ── Internal send logic ────────────────────────────────────────────────

    private async Task SendAsync(string to, string subject, string htmlBody, string? consoleLink = null)
    {
        if (!IsSmtpConfigured)
        {
            LogFallback(to, subject, consoleLink ?? "— see html body —");
            return;
        }

        try
        {
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort);
            client.EnableSsl = _settings.UseSsl;
            client.DeliveryMethod = SmtpDeliveryMethod.Network;
            client.Timeout = 20_000;

            if (!string.IsNullOrWhiteSpace(_settings.Username))
                client.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

            using var msg = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            msg.To.Add(to);

            await client.SendMailAsync(msg);
            _logger.LogInformation("Email sent to {To} — Subject: {Subject}", to, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {To} — Subject: {Subject}", to, subject);
            throw;
        }
    }

    private void LogFallback(string to, string subject, string link)
    {
        if (_env.IsDevelopment())
        {
            _logger.LogInformation(
                "EMAIL (SMTP not configured — dev mode)" +
                "\n  To      : {To}" +
                "\n  Subject : {Subject}" +
                "\n  Link    : {Link}",
                to, subject, link);
        }
        else
        {
            _logger.LogWarning(
                "Email NOT sent — SMTP is not configured." +
                " To: {To} | Subject: {Subject}", to, subject);
        }
    }

    // ── HTML email template ────────────────────────────────────────────────

    private static string BuildEmailBody(string heading, string previewText, string bodyHtml, string? linkUrl, string? linkText)
    {
        return $@"<!DOCTYPE html>
<html lang='en'>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width,initial-scale=1'>
  <title>{heading}</title>
</head>
<body style='margin:0;padding:0;background:#F5F0EA;font-family:""DM Sans"",Arial,sans-serif;'>
  <!-- Preview text -->
  <span style='display:none;max-height:0;overflow:hidden;'>{previewText}</span>

  <table width='100%' cellpadding='0' cellspacing='0' border='0' style='background:#F5F0EA;padding:40px 16px;'>
    <tr><td align='center'>
      <table width='100%' style='max-width:560px;' cellpadding='0' cellspacing='0'>

        <!-- Logo header -->
        <tr>
          <td align='center' style='padding-bottom:24px;'>
            <div style='display:inline-flex;align-items:center;gap:10px;'>
              <div style='width:36px;height:36px;background:#2D6A4F;border-radius:8px;
                          display:inline-flex;align-items:center;justify-content:center;
                          color:#ffffff;font-weight:700;font-size:18px;'>S</div>
              <span style='font-size:20px;font-weight:700;color:#2D3A2E;'>Sisonke</span>
            </div>
          </td>
        </tr>

        <!-- Card -->
        <tr>
          <td style='background:#ffffff;border-radius:16px;padding:40px 40px 32px;
                     box-shadow:0 2px 12px rgba(45,42,40,0.08);'>
            <h1 style='margin:0 0 20px;font-size:22px;font-weight:700;color:#2D3A2E;'>{heading}</h1>
            {bodyHtml}
          </td>
        </tr>

        <!-- Footer -->
        <tr>
          <td align='center' style='padding-top:24px;color:#9E9189;font-size:12px;'>
            <p style='margin:0;'>© Sisonke · Stokvel Management Platform</p>
            <p style='margin:4px 0 0;'>This email was sent to you because you have a Sisonke account.</p>
          </td>
        </tr>

      </table>
    </td></tr>
  </table>
</body>
</html>";
    }
}
