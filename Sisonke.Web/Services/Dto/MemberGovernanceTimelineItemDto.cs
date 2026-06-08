namespace Sisonke.Web.Services.Dto;

public class MemberGovernanceTimelineItemDto
{
    public DateTime EventDate { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? Reference { get; set; }

    public string? Source { get; set; }

    public string BadgeStyle { get; set; } = "neutral";

    public string? LinkUrl { get; set; }
}