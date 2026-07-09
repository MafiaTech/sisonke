using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Dto;

public sealed class RoleManagementViewModel
{
    public Guid StokvelId { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public bool IsAuthorized { get; set; }
    public string? DenialReason { get; set; }
    public List<RoleAssignmentRow> Members { get; set; } = [];
}

public sealed class RoleAssignmentRow
{
    public Guid MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? EmailAddress { get; set; }
    public string? ApplicationUserId { get; set; }
    public SisonkeRole Role { get; set; }
    public bool IsCurrentUser { get; set; }
}

public sealed class ConfigurationReviewViewModel
{
    public Guid StokvelId { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public bool IsLinkedMember { get; set; }
    public bool CanManage { get; set; }
    public string? DenialReason { get; set; }
    public List<ConfigurationSectionRow> Sections { get; set; } = [];
}

public sealed class ConfigurationSectionRow
{
    public string Title { get; set; } = string.Empty;
    public string? EditHref { get; set; }
    public List<ConfigurationValueRow> Values { get; set; } = [];
}

public sealed class ConfigurationValueRow
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public bool NeedsAttention { get; set; }
}

public sealed class ProductionReadinessViewModel
{
    public bool IsAuthorized { get; set; }
    public string EnvironmentName { get; set; } = string.Empty;
    public string AppVersion { get; set; } = string.Empty;
    public List<ReadinessCheckRow> Checks { get; set; } = [];
    public List<AuditLogRow> RecentActivity { get; set; } = [];
}

public sealed class ReadinessCheckRow
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public bool IsHealthy { get; set; }
}

public sealed class AuditLogRow
{
    public DateTime TimestampUtc { get; set; }
    public string ActionType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}
