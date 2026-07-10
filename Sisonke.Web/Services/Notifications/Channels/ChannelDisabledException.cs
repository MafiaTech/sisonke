namespace Sisonke.Web.Services.Notifications.Channels;

/// <summary>
/// Thrown by a channel sender when the channel is deliberately turned off (e.g. WhatsApp
/// pending Meta template approval). The dispatcher treats this as Cancelled, not Failed.
/// </summary>
public sealed class ChannelDisabledException(string message) : Exception(message);
