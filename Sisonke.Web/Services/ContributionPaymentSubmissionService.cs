using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Services.Notifications;

namespace Sisonke.Web.Services;

// Compatibility entry point for existing contribution callers; all workflow logic is shared.
public sealed class ContributionPaymentSubmissionService(
    IDbContextFactory<ApplicationDbContext> factory, PaymentProofStorage storage,
    AuditLogService audit, NotificationEnqueuer notifications)
    : MemberPaymentSubmissionService(factory, storage, audit, notifications);
