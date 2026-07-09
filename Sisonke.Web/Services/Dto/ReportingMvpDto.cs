using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services.Dto;

public sealed class ReportingMvpDto
{
    public List<ReportingStokvelOptionDto> LinkedStokvels { get; set; } = [];
    public Guid? SelectedStokvelId { get; set; }
    public string SelectedStokvelName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public bool CanViewGroupReports { get; set; }
    public bool FinancialProductsAllowed { get; set; }
    public bool EarlyPayoutEnabled { get; set; }
    public bool IsBurialStokvel { get; set; }
    public ReportingMemberSummaryDto? MemberSummary { get; set; }
    public ReportingMemberStatementDto MemberStatement { get; set; } = new();
    public ReportingContributionsReportDto Contributions { get; set; } = new();
    public ReportingLoansReportDto Loans { get; set; } = new();
    public ReportingWalletReportDto Wallet { get; set; } = new();
    public ReportingEarlyPayoutReportDto EarlyPayouts { get; set; } = new();
    public ReportingFinesReportDto Fines { get; set; } = new();
    public ReportingAttendanceReportDto Attendance { get; set; } = new();
    public ReportingBurialReportDto Burial { get; set; } = new();
    public ReportingGroupFinancialSummaryDto GroupFinancialSummary { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ReportingStokvelOptionDto
{
    public Guid StokvelId { get; set; }
    public string StokvelName { get; set; } = string.Empty;
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public SisonkeRole Role { get; set; }
}

public sealed class ReportingMemberSummaryDto
{
    public Guid MemberId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string MemberNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? CellphoneNumber { get; set; }
    public SisonkeRole Role { get; set; }
}

public sealed class ReportingMemberStatementDto
{
    public decimal ExpectedContributions { get; set; }
    public decimal ContributionsPaid { get; set; }
    public decimal ContributionsOutstanding { get; set; }
    public decimal FinesOutstanding { get; set; }
    public decimal LoansOutstanding { get; set; }
    public decimal WalletAvailableBalance { get; set; }
    public decimal WalletSurplusEquity { get; set; }
    public decimal WalletLockedSurplus { get; set; }
    public decimal PayoutsReceived { get; set; }
    public List<ReportingContributionLineDto> Contributions { get; set; } = [];
    public List<ReportingFineLineDto> Fines { get; set; } = [];
    public List<ReportingLoanLineDto> Loans { get; set; } = [];
    public List<ReportingWalletLedgerLineDto> WalletMovements { get; set; } = [];
    public List<ReportingPayoutLineDto> Payouts { get; set; } = [];
}

public sealed class ReportingContributionsReportDto
{
    public decimal TotalExpected { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<ReportingContributionMemberBreakdownDto> MemberBreakdown { get; set; } = [];
}

public sealed class ReportingContributionMemberBreakdownDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ReportingContributionLineDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime PeriodDate { get; set; }
    public string PeriodLabel { get; set; } = string.Empty;
    public decimal Expected { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public sealed class ReportingLoansReportDto
{
    public int ActiveCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int PaidOutCount { get; set; }
    public decimal TotalIssued { get; set; }
    public decimal OutstandingBalances { get; set; }
    public List<ReportingLoanLineDto> Loans { get; set; } = [];
}

public sealed class ReportingLoanLineDto
{
    public Guid LoanId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string LoanType { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal OutstandingBalance { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public bool IsWalletBacked { get; set; }
    public string GuarantorStatus { get; set; } = string.Empty;
    public decimal EarlyPayoutGrossAmount { get; set; }
    public decimal EarlyPayoutNetAmount { get; set; }
    public decimal EarlyPayoutReserveAmount { get; set; }
}

public sealed class ReportingWalletReportDto
{
    public decimal CoreMandatoryBalance { get; set; }
    public decimal SurplusEquityBalance { get; set; }
    public decimal LockedSurplusBalance { get; set; }
    public decimal AvailableSurplusBalance { get; set; }
    public string ReconciliationStatus { get; set; } = string.Empty;
    public List<ReportingWalletLedgerLineDto> LedgerEntries { get; set; } = [];
}

public sealed class ReportingWalletLedgerLineDto
{
    public DateTime Date { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string TransactionType { get; set; } = string.Empty;
    public string SourceType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class ReportingEarlyPayoutReportDto
{
    public decimal GrossPayoutAmount { get; set; }
    public decimal AdjustedPayoutAmount { get; set; }
    public decimal ReserveAdjustmentAmount { get; set; }
    public List<ReportingEarlyPayoutLineDto> Requests { get; set; } = [];
}

public sealed class ReportingEarlyPayoutLineDto
{
    public Guid LoanId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal GrossAmount { get; set; }
    public decimal AdjustedPayoutAmount { get; set; }
    public decimal ReserveAdjustmentAmount { get; set; }
    public string ApprovalStatus { get; set; } = string.Empty;
    public string WorkflowStatus { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
}

public sealed class ReportingFinesReportDto
{
    public decimal FinesIssued { get; set; }
    public decimal FinesPaid { get; set; }
    public decimal FinesOutstanding { get; set; }
    public List<ReportingFineLineDto> Fines { get; set; } = [];
}

public sealed class ReportingFineLineDto
{
    public DateTime FineDate { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Paid { get; set; }
    public decimal Outstanding { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ReportingAttendanceReportDto
{
    public int MeetingCount { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int ApologyCount { get; set; }
    public List<ReportingAttendanceLineDto> Lines { get; set; } = [];
}

public sealed class ReportingAttendanceLineDto
{
    public Guid MeetingId { get; set; }
    public DateTime MeetingDate { get; set; }
    public string MeetingTitle { get; set; } = string.Empty;
    public string MemberName { get; set; } = string.Empty;
    public string AttendanceStatus { get; set; } = string.Empty;
    public bool IsLate { get; set; }
    public string ApologyStatus { get; set; } = string.Empty;
    public string ApologyType { get; set; } = string.Empty;
}

public sealed class ReportingBurialReportDto
{
    public int CoveredLivesActive { get; set; }
    public int CoveredLivesPending { get; set; }
    public int CoveredLivesRejected { get; set; }
    public int CoveredLivesRemoved { get; set; }
    public int ClaimsSubmitted { get; set; }
    public int ClaimsUnderReview { get; set; }
    public int ClaimsApprovedAwaitingPayout { get; set; }
    public int ClaimsPaid { get; set; }
    public decimal ApprovedAwaitingPayoutAmount { get; set; }
    public decimal PaidPayoutAmount { get; set; }
    public List<ReportingCoveredLifeLineDto> CoveredLives { get; set; } = [];
    public List<ReportingBurialClaimLineDto> Claims { get; set; } = [];
}

public sealed class ReportingCoveredLifeLineDto
{
    public string MemberName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ReportingBurialClaimLineDto
{
    public string MemberName { get; set; } = string.Empty;
    public string DeceasedFullName { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string SubjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
}

public sealed class ReportingPayoutLineDto
{
    public DateTime? PaidAt { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string PayoutType { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Reference { get; set; }
}

public sealed class ReportingGroupFinancialSummaryDto
{
    public decimal TotalContributions { get; set; }
    public decimal TotalFines { get; set; }
    public decimal TotalLoansIssued { get; set; }
    public decimal TotalLoansOutstanding { get; set; }
    public decimal TotalSurplusWalletBalances { get; set; }
    public decimal TotalReserveWalletBalance { get; set; }
    public decimal PendingPayouts { get; set; }
    public decimal AvailableGroupCashView { get; set; }
}
