namespace Sisonke.Web.Models;

// ── Shared ──────────────────────────────────────────────────

public class WorkspaceUserContext
{
    public string  FullName     { get; set; } = string.Empty;
    public string  Initials     { get; set; } = string.Empty;
    public string  Role         { get; set; } = string.Empty;
    public string  RoleLabel    { get; set; } = string.Empty;
    public string? StokvelName  { get; set; }
    public string? StokvelType  { get; set; }
    public int     UnreadCount  { get; set; }
}

// ── Member ───────────────────────────────────────────────────

public class MemberWorkspaceViewModel
{
    public WorkspaceUserContext  User              { get; set; } = new();
    public string                GreetingTime      { get; set; } = "Good morning";
    public decimal               TotalContributed  { get; set; }
    public int                   TotalPaymentsMade { get; set; }
    public DateTime              MemberSince       { get; set; }
    public int                   RotationPosition  { get; set; }
    public int                   RotationTotal     { get; set; }
    public decimal               MonthlyAmount     { get; set; }
    public bool                  IsCurrentCyclePaid{ get; set; }
    public DateTime?             LastPaymentDate   { get; set; }
    public decimal               OutstandingBalance{ get; set; }
    public int                   MonthsPaid        { get; set; }
    public decimal               OutstandingFines  { get; set; }
    public string?               MyHostMonth       { get; set; }
    public string?               MemberNumber      { get; set; }
    public string?               PhoneNumber       { get; set; }
    public string?               Email             { get; set; }
    public string?               FicaStatus        { get; set; }
    public string?               BankLast4         { get; set; }
    public string?               NextOfKinName     { get; set; }
    public string?               BeneficiaryName   { get; set; }
    public string                ConstitutionStatus{ get; set; } = "Not Created";

    public List<WsMonthlyPayment>  RecentPayments   { get; set; } = new();
    public List<WsRotationEntry>   RotationSchedule { get; set; } = new();
    public List<WsUpcomingMeeting> UpcomingMeetings { get; set; } = new();
    public List<WsAnnouncement>    Announcements    { get; set; } = new();
    public List<WsFineItem>        MyFines          { get; set; } = new();
    public List<WsMemberDocument>  MyDocuments      { get; set; } = new();
}

// ── Admin (Office Bearer) ────────────────────────────────────

public class AdminWorkspaceViewModel
{
    public WorkspaceUserContext  User                  { get; set; } = new();
    public string                AdminRoleLabel        { get; set; } = string.Empty;
    public decimal               GroupBalance          { get; set; }
    public string                PackageName           { get; set; } = string.Empty;
    public decimal               MonthlyFee            { get; set; }
    public int                   MemberCount           { get; set; }
    public int                   MemberLimit           { get; set; }
    public decimal               CollectionRatePercent { get; set; }
    public int                   MembersPaid           { get; set; }
    public decimal               OutstandingFines      { get; set; }
    public int                   OpenFinesCount        { get; set; }
    public int                   PendingApprovalsCount { get; set; }
    public string                ConstitutionStatus    { get; set; } = string.Empty;
    public DateTime?             LastConstitutionUpdate{ get; set; }
    public string?               NextHostMember        { get; set; }
    public string?               NextHostMonth         { get; set; }
    public string?               NextHostAlert         { get; set; }

    public List<WsPendingApproval>   PendingApprovals  { get; set; } = new();
    public List<WsMemberSummaryRow>  MemberRows        { get; set; } = new();
    public List<WsTransgression>     Transgressions    { get; set; } = new();
    public List<WsUpcomingMeeting>   UpcomingMeetings  { get; set; } = new();
}

// ── Platform Admin ────────────────────────────────────────────

public class PlatformWorkspaceViewModel
{
    public WorkspaceUserContext  User                    { get; set; } = new();
    public decimal               MonthlyRecurringRevenue { get; set; }
    public int                   TotalActiveStokvels     { get; set; }
    public int                   TotalPendingStokvels    { get; set; }
    public int                   TotalRegisteredUsers    { get; set; }
    public int                   OpenSupportTickets      { get; set; }
    public int                   UrgentSupportTickets    { get; set; }
    public bool                  AllSystemsOperational   { get; set; } = true;
    public int                   BasicSubscriptions      { get; set; }
    public int                   StandardSubscriptions   { get; set; }
    public int                   ProSubscriptions        { get; set; }
    public int                   NetworkSubscriptions    { get; set; }

    public List<WsStokvelRow>    RecentStokvels { get; set; } = new();
    public List<WsAuditLogEntry> AuditLog       { get; set; } = new();
    public List<WsSupportTicket> UrgentTickets  { get; set; } = new();
    public List<WsSystemStatus>  SystemStatuses { get; set; } = new();
}

// ── Support Agent ────────────────────────────────────────────

public class SupportWorkspaceViewModel
{
    public WorkspaceUserContext  User           { get; set; } = new();
    public int                   OpenTickets    { get; set; }
    public int                   UrgentTickets  { get; set; }
    public int                   ResolvedToday  { get; set; }
    public decimal               CsatScore      { get; set; }
    public int                   EscalatedCount { get; set; }

    public List<WsSupportTicket> MyTickets      { get; set; } = new();
    public List<WsSupportTicket> ResolvedList   { get; set; } = new();
    public List<WsRecentLookup>  RecentLookups  { get; set; } = new();
}

// ── Supporting types ─────────────────────────────────────────

public class WsMonthlyPayment
{
    public string  Month  { get; set; } = string.Empty;
    public string  Status { get; set; } = "Future"; // Paid | Due | Future
    public decimal Amount { get; set; }
}

public class WsRotationEntry
{
    public int    Position   { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string HostMonth  { get; set; } = string.Empty;
    public string Status     { get; set; } = string.Empty;
    public bool   IsMe       { get; set; }
}

public class WsUpcomingMeeting
{
    public Guid     Id             { get; set; }
    public string   Title          { get; set; } = string.Empty;
    public DateTime Date           { get; set; }
    public string?  Venue          { get; set; }
    public bool     CanSendApology { get; set; }
    public bool     ApologySent    { get; set; }
}

public class WsAnnouncement
{
    public string   Title     { get; set; } = string.Empty;
    public string   Body      { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool     IsRead    { get; set; }
}

public class WsFineItem
{
    public string   Reason { get; set; } = string.Empty;
    public DateTime Date   { get; set; }
    public decimal  Amount { get; set; }
    public bool     IsPaid { get; set; }
}

public class WsMemberDocument
{
    public string FileName    { get; set; } = string.Empty;
    public string FileType    { get; set; } = string.Empty;
    public string FileSize    { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = "#";
}

public class WsPendingApproval
{
    public string Description { get; set; } = string.Empty;
    public string ActionUrl   { get; set; } = "#";
    public string ActionLabel { get; set; } = "Review";
}

public class WsMemberSummaryRow
{
    public string   MemberName           { get; set; } = string.Empty;
    public string   RoleLabel            { get; set; } = string.Empty;
    public string   FicaStatus           { get; set; } = string.Empty;
    public string   ContributionStatus   { get; set; } = string.Empty;
    public decimal? OutstandingFine      { get; set; }
    public string   ProfileUrl           { get; set; } = "#";
    public bool     IsPendingApproval    { get; set; }
}

public class WsTransgression
{
    public string  MemberName { get; set; } = string.Empty;
    public string  Rule       { get; set; } = string.Empty;
    public decimal Amount     { get; set; }
    public string  Status     { get; set; } = string.Empty;
}

public class WsStokvelRow
{
    public string StokvelName { get; set; } = string.Empty;
    public string OwnerName   { get; set; } = string.Empty;
    public string Type        { get; set; } = string.Empty;
    public string Package     { get; set; } = string.Empty;
    public int    MemberCount { get; set; }
    public int    MemberLimit { get; set; }
    public string Status      { get; set; } = string.Empty;
    public string ManageUrl   { get; set; } = "#";
}

public class WsAuditLogEntry
{
    public string   Description { get; set; } = string.Empty;
    public string   Actor       { get; set; } = string.Empty;
    public DateTime CreatedAt   { get; set; }
}

public class WsSupportTicket
{
    public int      Id               { get; set; }
    public string   Title            { get; set; } = string.Empty;
    public string   StokvelName      { get; set; } = string.Empty;
    public string   UserName         { get; set; } = string.Empty;
    public string   Priority         { get; set; } = "Normal";
    public DateTime CreatedAt        { get; set; }
    public string?  Resolution       { get; set; }
    public decimal? ResolutionHours  { get; set; }
    public string?  Rating           { get; set; }
}

public class WsSystemStatus
{
    public string ServiceName { get; set; } = string.Empty;
    public string Status      { get; set; } = string.Empty; // Operational | Degraded | Down
}

public class WsRecentLookup
{
    public string Name     { get; set; } = string.Empty;
    public string SubLabel { get; set; } = string.Empty;
    public string ViewUrl  { get; set; } = "#";
}
