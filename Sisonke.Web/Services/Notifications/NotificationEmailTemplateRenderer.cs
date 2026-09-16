using System.Net;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Notifications;

public sealed class NotificationEmailTemplateRenderer(
    AppSettings appSettings,
    ILogger<NotificationEmailTemplateRenderer> logger)
{
    public string Render(
        NotificationType type,
        Guid? stokvelId,
        string entityType,
        Guid entityId,
        string subject,
        string body)
    {
        var cta = ResolveCta(type, stokvelId, entityType, entityId);
        var safeSubject = WebUtility.HtmlEncode(subject);
        var safeBody = WebUtility.HtmlEncode(ReplaceKnownRelativeLinks(body, cta.RelativePath))
            .Replace("\r\n", "<br>")
            .Replace("\n", "<br>");
        var ctaUrl = BuildAbsoluteUrl(cta.RelativePath);

        return $"""
            <p>{safeBody}</p>
            <p>
                <a href="{WebUtility.HtmlEncode(ctaUrl)}" style="display:inline-block;padding:10px 16px;background:#1b6b5f;color:#ffffff;text-decoration:none;border-radius:6px;">
                    {WebUtility.HtmlEncode(cta.Label)}
                </a>
            </p>
            <p style="color:#64748b;font-size:12px;">If the button does not open, copy and paste this link into your browser:<br>{WebUtility.HtmlEncode(ctaUrl)}</p>
            <p style="color:#64748b;font-size:12px;">{safeSubject}</p>
            """;
    }

    public string BuildAbsoluteUrl(string relativePath)
    {
        var baseUrl = !string.IsNullOrWhiteSpace(appSettings.BaseUrl)
            ? appSettings.BaseUrl
            : appSettings.PublicBaseUrl;

        var normalizedPath = string.IsNullOrWhiteSpace(relativePath)
            ? "/"
            : relativePath.Trim();

        if (!normalizedPath.StartsWith('/'))
        {
            normalizedPath = "/" + normalizedPath;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            logger.LogWarning("App:BaseUrl is not configured; notification email CTA link remains relative: {RelativePath}", normalizedPath);
            return normalizedPath;
        }

        return $"{baseUrl.TrimEnd('/')}{normalizedPath}";
    }

    private static NotificationCta ResolveCta(NotificationType type, Guid? stokvelId, string entityType, Guid entityId)
    {
        return type switch
        {
            NotificationType.ContributionPaymentProofSubmitted or NotificationType.MemberPaymentProofSubmitted when stokvelId.HasValue =>
                new NotificationCta($"/treasurer-tasks/{stokvelId.Value}#payment-proofs", "Review payment proof"),
            NotificationType.ContributionPaymentProofApproved or NotificationType.ContributionPaymentProofRejected or NotificationType.MemberPaymentProofApproved or NotificationType.MemberPaymentProofRejected =>
                new NotificationCta("/my-workspace", "View my payments"),
            NotificationType.MinutesPublished =>
                new NotificationCta($"/meeting-minutes/{entityId}", "View minutes"),
            NotificationType.MeetingReminder =>
                new NotificationCta($"/meeting/{entityId}", "Open meeting"),
            NotificationType.PaymentReminder when stokvelId.HasValue =>
                new NotificationCta($"/treasurer-tasks/{stokvelId.Value}", "Open treasurer task"),
            NotificationType.TaskAssigned =>
                BuildTaskCta(stokvelId, entityType, entityId),
            NotificationType.ChairpersonApproved or NotificationType.ChairpersonRejected =>
                BuildRequestDecisionCta(stokvelId, entityType, entityId),
            NotificationType.TrialStarted or NotificationType.TrialReminder or NotificationType.InvoiceReceiptIssued or
                NotificationType.PaymentFailed or NotificationType.SubscriptionRestricted or NotificationType.SubscriptionSuspended or
                NotificationType.PaymentRecovered or NotificationType.PlanChanged or NotificationType.SubscriptionCancelled or
                NotificationType.UsageWarning or NotificationType.LegacyMigrationReminder =>
                new NotificationCta("/subscription", "Open billing page"),
            _ when stokvelId.HasValue =>
                new NotificationCta($"/stokvel-dashboard/{stokvelId.Value}", "Open Sisonke"),
            _ =>
                new NotificationCta("/", "Open Sisonke")
        };
    }

    private static NotificationCta BuildTaskCta(Guid? stokvelId, string entityType, Guid entityId)
    {
        if (string.Equals(entityType, "FuneralClaim", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationCta($"/claim/{entityId}", "Open task");
        }

        if (string.Equals(entityType, "RotationalPayout", StringComparison.OrdinalIgnoreCase) && stokvelId.HasValue)
        {
            return new NotificationCta($"/rotational/management/{stokvelId.Value}", "Open task");
        }

        if (string.Equals(entityType, "Meeting", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationCta($"/meeting/{entityId}", "Open task");
        }

        return stokvelId.HasValue
            ? new NotificationCta($"/stokvel-dashboard/{stokvelId.Value}", "Open task")
            : new NotificationCta("/", "Open task");
    }

    private static NotificationCta BuildRequestDecisionCta(Guid? stokvelId, string entityType, Guid entityId)
    {
        if (string.Equals(entityType, "FuneralClaim", StringComparison.OrdinalIgnoreCase))
        {
            return new NotificationCta($"/claim/{entityId}", "Open request");
        }

        if (string.Equals(entityType, "RotationalPayout", StringComparison.OrdinalIgnoreCase) && stokvelId.HasValue)
        {
            return new NotificationCta($"/rotational/management/{stokvelId.Value}", "Open request");
        }

        return stokvelId.HasValue
            ? new NotificationCta($"/stokvel-dashboard/{stokvelId.Value}", "Open request")
            : new NotificationCta("/", "Open request");
    }

    private static string ReplaceKnownRelativeLinks(string body, string relativePath)
    {
        var normalizedPath = relativePath.StartsWith('/') ? relativePath : "/" + relativePath;
        return body.Replace(normalizedPath, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
    }

    private sealed record NotificationCta(string RelativePath, string Label);
}
