namespace Sisonke.Web.Services.Notifications.Channels;

/// <summary>
/// Thrown when a Web Push send can never succeed for this recipient — every subscription
/// returned 404/410 (and was deleted), or none existed. The dispatcher treats this as
/// Cancelled, not Failed, since retrying without a fresh subscription is pointless.
/// </summary>
public sealed class PushSubscriptionGoneException(string message) : Exception(message);
