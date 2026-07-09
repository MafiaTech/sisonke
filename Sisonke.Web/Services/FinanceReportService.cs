using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class FinanceReportService(
    ApplicationDbContext context,
    ContributionPaymentService contributionPaymentService,
    FineService fineService,
    FuneralClaimService funeralClaimService)
{
    public async Task<ReportingMvpDto> GetReportingMvpAsync(string currentUserId, Guid? requestedStokvelId, DateTime? fromDate, DateTime? toDate)
    {
        var from = (fromDate ?? DateTime.Today.AddMonths(-6)).Date;
        var to = (toDate ?? DateTime.Today).Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }

        var linkedStokvels = await GetLinkedStokvelOptionsAsync(currentUserId);
        var selected = requestedStokvelId.HasValue
            ? linkedStokvels.FirstOrDefault(option => option.StokvelId == requestedStokvelId.Value)
            : linkedStokvels.FirstOrDefault();

        var report = new ReportingMvpDto
        {
            LinkedStokvels = linkedStokvels,
            SelectedStokvelId = selected?.StokvelId,
            SelectedStokvelName = selected?.StokvelName ?? string.Empty,
            FromDate = from,
            ToDate = to,
            CanViewGroupReports = selected is not null && IsGroupReportRole(selected.Role),
            GeneratedAt = DateTime.UtcNow
        };

        if (selected is null)
        {
            return report;
        }

        report.MemberSummary = new ReportingMemberSummaryDto
        {
            MemberId = selected.MemberId,
            FullName = selected.MemberName,
            Role = selected.Role
        };

        var stokvel = await context.Stokvels.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == selected.StokvelId);
        var member = await context.Members.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == selected.MemberId);

        if (stokvel is null || member is null)
        {
            return report;
        }

        report.MemberSummary.MemberNumber = member.MemberNumber;
        report.MemberSummary.Email = member.EmailAddress;
        report.MemberSummary.CellphoneNumber = member.CellphoneNumber;

        var memberIds = report.CanViewGroupReports
            ? await context.Members.AsNoTracking()
                .Where(item => item.TenantId == stokvel.TenantId)
                .Select(item => item.Id)
                .ToListAsync()
            : [selected.MemberId];

        var memberNames = await context.Members.AsNoTracking()
            .Where(item => memberIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.FullName);

        var memberContributions = await GetContributionLinesAsync(stokvel, [selected.MemberId], from, to, memberNames);
        var reportContributions = await GetContributionLinesAsync(stokvel, memberIds, from, to, memberNames);
        var memberFines = await GetFineLinesAsync(stokvel.TenantId, [selected.MemberId], from, to, memberNames);
        var reportFines = await GetFineLinesAsync(stokvel.TenantId, memberIds, from, to, memberNames);
        var memberLoans = await GetLoanLinesAsync(selected.StokvelId, [selected.MemberId], from, to);
        var reportLoans = await GetLoanLinesAsync(selected.StokvelId, memberIds, from, to);
        var memberWallet = await GetWalletReportAsync(selected.StokvelId, [selected.MemberId], from, to, memberNames);
        var reportWallet = await GetWalletReportAsync(selected.StokvelId, memberIds, from, to, memberNames);
        var memberPayouts = await GetPayoutLinesAsync(stokvel, [selected.MemberId], from, to, memberNames);
        var reportPayouts = await GetPayoutLinesAsync(stokvel, memberIds, from, to, memberNames);
        var reportEarlyPayouts = GetEarlyPayoutReport(reportLoans);
        var attendance = await GetAttendanceReportAsync(stokvel.TenantId, memberIds, from, to, memberNames);

        report.MemberStatement = new ReportingMemberStatementDto
        {
            ExpectedContributions = memberContributions.Sum(item => item.Expected),
            ContributionsPaid = memberContributions.Sum(item => item.Paid),
            ContributionsOutstanding = memberContributions.Sum(item => item.Outstanding),
            FinesOutstanding = memberFines.Sum(item => item.Outstanding),
            LoansOutstanding = memberLoans.Sum(item => item.OutstandingBalance),
            WalletAvailableBalance = memberWallet.AvailableSurplusBalance,
            WalletSurplusEquity = memberWallet.SurplusEquityBalance,
            WalletLockedSurplus = memberWallet.LockedSurplusBalance,
            PayoutsReceived = memberPayouts.Where(item => item.Status == "Paid").Sum(item => item.Amount),
            Contributions = memberContributions,
            Fines = memberFines,
            Loans = memberLoans,
            WalletMovements = memberWallet.LedgerEntries,
            Payouts = memberPayouts
        };

        report.Contributions = GetContributionsReport(reportContributions);
        report.Loans = GetLoansReport(reportLoans);
        report.Wallet = reportWallet;
        report.EarlyPayouts = reportEarlyPayouts;
        report.Fines = new ReportingFinesReportDto
        {
            FinesIssued = reportFines.Sum(item => item.Amount),
            FinesPaid = reportFines.Sum(item => item.Paid),
            FinesOutstanding = reportFines.Sum(item => item.Outstanding),
            Fines = reportFines
        };
        report.Attendance = attendance;
        report.GroupFinancialSummary = new ReportingGroupFinancialSummaryDto
        {
            TotalContributions = report.Contributions.TotalPaid,
            TotalFines = report.Fines.FinesPaid,
            TotalLoansIssued = report.Loans.TotalIssued,
            TotalLoansOutstanding = report.Loans.OutstandingBalances,
            TotalSurplusWalletBalances = report.Wallet.AvailableSurplusBalance,
            TotalReserveWalletBalance = report.EarlyPayouts.ReserveAdjustmentAmount,
            PendingPayouts = reportPayouts.Where(item => item.Status is not "Paid").Sum(item => item.Amount),
            AvailableGroupCashView = report.Contributions.TotalPaid + report.Fines.FinesPaid + report.Wallet.AvailableSurplusBalance + report.EarlyPayouts.ReserveAdjustmentAmount - reportPayouts.Where(item => item.Status is not "Paid").Sum(item => item.Amount)
        };

        if (!report.CanViewGroupReports)
        {
            report.Contributions = GetContributionsReport(memberContributions);
            report.Loans = GetLoansReport(memberLoans);
            report.Wallet = memberWallet;
            report.EarlyPayouts = GetEarlyPayoutReport(memberLoans);
            report.Fines.Fines = memberFines;
            report.Fines.FinesIssued = memberFines.Sum(item => item.Amount);
            report.Fines.FinesPaid = memberFines.Sum(item => item.Paid);
            report.Fines.FinesOutstanding = memberFines.Sum(item => item.Outstanding);
            report.Attendance = await GetAttendanceReportAsync(stokvel.TenantId, [selected.MemberId], from, to, memberNames);
        }

        return report;
    }

    public async Task<FinanceSummaryReportDto> GetFinanceSummaryReportAsync(Guid stokvelId, int year, int month)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return new FinanceSummaryReportDto
            {
                StokvelId = stokvelId,
                Year = year,
                Month = month,
                GeneratedAt = DateTime.UtcNow
            };
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        var summary = await contributionPaymentService.GetMonthlyContributionSummaryAsync(stokvelId, year, month);
        var outstandingFinesTotal = await fineService.GetOutstandingFinesTotalByStokvelIdAsync(stokvelId);
        var outstandingFinesCount = await fineService.GetOutstandingFineCountByStokvelIdAsync(stokvelId);
        var approvedClaimsAwaitingPayout = await funeralClaimService.GetApprovedClaimsAwaitingPayoutByStokvelIdAsync(stokvelId);

        return new FinanceSummaryReportDto
        {
            StokvelId = stokvelId,
            StokvelName = stokvel?.Name ?? string.Empty,
            Year = year,
            Month = month,
            MonthName = new DateTime(year, month, 1).ToString("MMMM"),
            ActiveMemberCount = summary.ActiveMemberCount,
            MonthlyContributionAmount = summary.MonthlyContributionAmount,
            ExpectedContributions = summary.ExpectedContributions,
            ActualContributions = summary.ActualContributions,
            OutstandingContributions = summary.OutstandingContributions,
            CollectionRatePercentage = summary.CollectionRatePercentage,
            PaidCount = summary.PaidCount,
            PartiallyPaidCount = summary.PartiallyPaidCount,
            OverdueCount = summary.OverdueCount,
            UnpaidCount = summary.UnpaidCount,
            OutstandingFinesTotal = outstandingFinesTotal,
            OutstandingFinesCount = outstandingFinesCount,
            ApprovedClaimsAwaitingPayoutTotal = approvedClaimsAwaitingPayout.Sum(claim => claim.PayoutAmount ?? 0),
            ApprovedClaimsAwaitingPayoutCount = approvedClaimsAwaitingPayout.Count,
            GeneratedAt = DateTime.UtcNow
        };
    }

    public async Task<MemberFinancialStatementDto?> GetMemberFinancialStatementAsync(Guid memberId, Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return null;
        }

        var member = await context.Members
            .Where(existingMember =>
                existingMember.Id == memberId &&
                existingMember.TenantId == stokvel.TenantId)
            .OrderBy(existingMember => existingMember.CreatedAt)
            .ThenBy(existingMember => existingMember.FullName)
            .FirstOrDefaultAsync();

        if (member is null)
        {
            return null;
        }

        var contributionLines = stokvel.EnableRotation
            ? await GetRotationalContributionStatementLinesAsync(stokvelId, member.Id)
            : await GetMonthlyContributionStatementLinesAsync(stokvel.TenantId, member.Id);

        var fines = await context.MemberFines
            .Include(fine => fine.FineType)
            .Where(fine =>
                fine.MemberId == member.Id &&
                fine.TenantId == stokvel.TenantId)
            .OrderByDescending(fine => fine.FineDate)
            .ThenByDescending(fine => fine.CreatedAt)
            .ToListAsync();

        var fineLines = fines
            .Select(fine =>
            {
                var amountPaid = fine.Status == FineStatus.Paid ? fine.Amount : 0;
                var balance = fine.Status == FineStatus.Unpaid ? fine.Amount : 0;

                return new MemberFineStatementLineDto
                {
                    FineId = fine.Id,
                    FineType = fine.FineType?.Name ?? "Fine",
                    Amount = fine.Amount,
                    AmountPaid = amountPaid,
                    Balance = balance,
                    Status = fine.Status.ToString(),
                    IssuedDate = fine.FineDate,
                    Reason = fine.Reason
                };
            })
            .ToList();

        var contributionOutstanding = contributionLines
            .Where(contribution => contribution.Status is not "Paid" and not "Exempted" and not "WrittenOff")
            .Sum(contribution => contribution.Balance);
        var outstandingFines = fineLines.Sum(fine => fine.Balance);

        return new MemberFinancialStatementDto
        {
            MemberId = member.Id,
            StokvelId = stokvel.Id,
            MemberName = member.FullName,
            MemberNumber = member.MemberNumber,
            CellphoneNumber = member.CellphoneNumber,
            Email = member.EmailAddress,
            StokvelName = stokvel.Name,
            IsRotationalStokvel = stokvel.EnableRotation,
            TotalExpectedContributions = contributionLines.Sum(contribution => contribution.ExpectedAmount),
            TotalContributionPaid = contributionLines.Sum(contribution => contribution.PaidAmount),
            TotalContributionOutstanding = contributionOutstanding,
            OverdueContributionCount = contributionLines.Count(contribution => contribution.Status == "Overdue"),
            TotalOutstandingFines = outstandingFines,
            OutstandingFineCount = fineLines.Count(fine => fine.Balance > 0),
            TotalOutstandingBalance = contributionOutstanding + outstandingFines,
            GeneratedAt = DateTime.UtcNow,
            Contributions = contributionLines,
            Fines = fineLines
        };
    }

    private async Task<List<ReportingStokvelOptionDto>> GetLinkedStokvelOptionsAsync(string currentUserId)
    {
        if (string.IsNullOrWhiteSpace(currentUserId))
        {
            return [];
        }

        var memberships = await context.Members.AsNoTracking()
            .Where(member => member.ApplicationUserId == currentUserId)
            .OrderBy(member => member.CreatedAt)
            .ToListAsync();
        var tenantIds = memberships.Select(member => member.TenantId).Distinct().ToList();
        var stokvels = await context.Stokvels.AsNoTracking()
            .Where(stokvel => tenantIds.Contains(stokvel.TenantId) && stokvel.IsActive && !stokvel.IsDeleted)
            .OrderBy(stokvel => stokvel.Name)
            .ToListAsync();

        return stokvels
            .SelectMany(stokvel => memberships
                .Where(member => member.TenantId == stokvel.TenantId)
                .Select(member => new ReportingStokvelOptionDto
                {
                    StokvelId = stokvel.Id,
                    StokvelName = stokvel.Name,
                    MemberId = member.Id,
                    MemberName = member.FullName,
                    Role = member.DefaultRole
                }))
            .GroupBy(option => option.StokvelId)
            .Select(group => group
                .OrderByDescending(option => IsGroupReportRole(option.Role))
                .ThenBy(option => option.MemberName)
                .First())
            .OrderBy(option => option.StokvelName)
            .ToList();
    }

    private static bool IsGroupReportRole(SisonkeRole role) =>
        role is SisonkeRole.Creator or SisonkeRole.StokvelAdmin or SisonkeRole.Chairperson or
            SisonkeRole.Secretary or SisonkeRole.Treasurer or SisonkeRole.PlatformSuperAdmin or
            SisonkeRole.PlatformSupportAdmin;

    private async Task<List<ReportingContributionLineDto>> GetContributionLinesAsync(Stokvel stokvel, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to, IReadOnlyDictionary<Guid, string> memberNames)
    {
        if (memberIds.Count == 0)
        {
            return [];
        }

        if (stokvel.EnableRotation)
        {
            var payments = await context.RotationalContributionPayments.AsNoTracking()
                .Include(payment => payment.Cycle)
                .Where(payment =>
                    payment.StokvelId == stokvel.Id &&
                    memberIds.Contains(payment.MemberId) &&
                    payment.IsActive &&
                    payment.Cycle.CycleStartDate.Date >= from &&
                    payment.Cycle.CycleStartDate.Date <= to)
                .OrderByDescending(payment => payment.Cycle.CycleStartDate)
                .ToListAsync();

            return payments
                .Select(payment => new ReportingContributionLineDto
                {
                    MemberId = payment.MemberId,
                    MemberName = memberNames.GetValueOrDefault(payment.MemberId, "Member"),
                    PeriodDate = payment.Cycle.CycleStartDate,
                    PeriodLabel = string.IsNullOrWhiteSpace(payment.Cycle.CycleName)
                        ? payment.Cycle.CycleStartDate.ToString("MMMM yyyy")
                        : payment.Cycle.CycleName,
                    Expected = payment.ExpectedAmount,
                    Paid = payment.PaidAmount,
                    Outstanding = Math.Max(0, payment.ExpectedAmount + payment.PenaltyAmount - payment.PaidAmount),
                    Status = payment.PaymentStatus.ToString(),
                    Reference = payment.ReferenceNumber
                })
                .ToList();
        }

        var contributions = await context.MemberContributions.AsNoTracking()
            .Include(contribution => contribution.ContributionCycle)
            .Where(contribution =>
                contribution.TenantId == stokvel.TenantId &&
                memberIds.Contains(contribution.MemberId) &&
                contribution.ContributionCycle.PeriodStart.Date >= from &&
                contribution.ContributionCycle.PeriodStart.Date <= to)
            .OrderByDescending(contribution => contribution.ContributionCycle.PeriodStart)
            .Select(contribution => new
            {
                contribution.Id,
                contribution.MemberId,
                contribution.ExpectedAmount,
                contribution.PaidAmount,
                contribution.OutstandingAmount,
                contribution.Status,
                contribution.ContributionCycle.PeriodStart,
                contribution.ContributionCycle.DueDate
            })
            .ToListAsync();
        var contributionIds = contributions.Select(item => item.Id).ToList();
        var references = contributionIds.Count == 0
            ? []
            : await context.Payments.AsNoTracking()
                .Where(payment => contributionIds.Contains(payment.MemberContributionId))
                .OrderByDescending(payment => payment.PaymentDate)
                .GroupBy(payment => payment.MemberContributionId)
                .Select(group => new { ContributionId = group.Key, Reference = group.First().Reference })
                .ToListAsync();
        var referenceByContributionId = references.ToDictionary(item => item.ContributionId, item => item.Reference);

        return contributions
            .Select(contribution => new ReportingContributionLineDto
            {
                PeriodDate = contribution.PeriodStart,
                MemberId = contribution.MemberId,
                MemberName = memberNames.GetValueOrDefault(contribution.MemberId, "Member"),
                PeriodLabel = contribution.PeriodStart.ToString("MMMM yyyy"),
                Expected = contribution.ExpectedAmount,
                Paid = contribution.PaidAmount,
                Outstanding = contribution.OutstandingAmount,
                Status = GetEffectivePaymentStatus(contribution.Status, contribution.DueDate) == PaymentStatus.Late ? "Overdue" : contribution.Status.ToString(),
                Reference = referenceByContributionId.GetValueOrDefault(contribution.Id)
            })
            .ToList();
    }

    private static ReportingContributionsReportDto GetContributionsReport(List<ReportingContributionLineDto> lines)
    {
        var grouped = lines
            .GroupBy(line => new { line.MemberId, line.MemberName })
            .Select(group => new ReportingContributionMemberBreakdownDto
            {
                MemberId = group.Key.MemberId,
                MemberName = group.Key.MemberName,
                Expected = group.Sum(line => line.Expected),
                Paid = group.Sum(line => line.Paid),
                Outstanding = group.Sum(line => line.Outstanding),
                Status = group.Sum(line => line.Outstanding) <= 0 ? "Paid" : "Outstanding"
            })
            .OrderByDescending(item => item.Expected)
            .ToList();

        return new ReportingContributionsReportDto
        {
            TotalExpected = lines.Sum(line => line.Expected),
            TotalPaid = lines.Sum(line => line.Paid),
            TotalOutstanding = lines.Sum(line => line.Outstanding),
            MemberBreakdown = grouped
        };
    }

    private async Task<List<ReportingFineLineDto>> GetFineLinesAsync(Guid tenantId, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to, IReadOnlyDictionary<Guid, string> memberNames)
    {
        var fines = await context.MemberFines.AsNoTracking()
            .Include(fine => fine.FineType)
            .Where(fine =>
                fine.TenantId == tenantId &&
                memberIds.Contains(fine.MemberId) &&
                fine.FineDate.Date >= from &&
                fine.FineDate.Date <= to)
            .OrderByDescending(fine => fine.FineDate)
            .ToListAsync();

        return fines
            .Select(fine => new ReportingFineLineDto
            {
                FineDate = fine.FineDate,
                MemberName = memberNames.GetValueOrDefault(fine.MemberId, "Member"),
                Category = fine.FineType?.Name ?? "Fine",
                Reason = fine.Reason,
                Amount = fine.Amount,
                Paid = fine.Status == FineStatus.Paid ? fine.Amount : 0,
                Outstanding = fine.Status == FineStatus.Unpaid ? fine.Amount : 0,
                Status = fine.Status.ToString()
            })
            .ToList();
    }

    private async Task<List<ReportingLoanLineDto>> GetLoanLinesAsync(Guid stokvelId, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to)
    {
        var loans = await context.MemberLoans.AsNoTracking()
            .Include(loan => loan.Member)
            .Include(loan => loan.Guarantors)
            .Where(loan =>
                loan.StokvelId == stokvelId &&
                memberIds.Contains(loan.MemberId) &&
                loan.IsActive &&
                loan.RequestedAt.Date >= from &&
                loan.RequestedAt.Date <= to)
            .OrderByDescending(loan => loan.RequestedAt)
            .ToListAsync();

        return loans
            .Select(loan => new ReportingLoanLineDto
            {
                LoanId = loan.Id,
                MemberName = loan.Member.FullName,
                LoanType = loan.LoanType.ToString(),
                RequestedAmount = loan.RequestedAmount,
                ApprovedAmount = loan.ApprovedAmount ?? 0,
                OutstandingBalance = loan.OutstandingBalance,
                Status = loan.LoanStatus.ToString(),
                RequestedAt = loan.RequestedAt,
                IsWalletBacked = loan.CollateralWalletId != null || loan.CollateralLockedAmount > 0,
                GuarantorStatus = loan.Guarantors.Count == 0
                    ? "Not required"
                    : $"{loan.Guarantors.Count(item => item.Status == MemberLoanGuarantorStatus.Accepted)}/{loan.Guarantors.Count} accepted",
                EarlyPayoutGrossAmount = loan.EarlyPayoutGrossAmount,
                EarlyPayoutNetAmount = loan.EarlyPayoutNetDisbursedAmount,
                EarlyPayoutReserveAmount = loan.EarlyPayoutDiscountAmount
            })
            .ToList();
    }

    private static ReportingLoansReportDto GetLoansReport(List<ReportingLoanLineDto> loans) => new()
    {
        ActiveCount = loans.Count(loan => loan.Status is "Active" or "Overdue"),
        PendingCount = loans.Count(loan => loan.Status is "Submitted" or "PendingApproval" or "DisbursementPending"),
        ApprovedCount = loans.Count(loan => loan.Status is "Approved"),
        RejectedCount = loans.Count(loan => loan.Status is "Rejected"),
        PaidOutCount = loans.Count(loan => loan.Status is "Active" or "Overdue" or "FullyRepaid"),
        TotalIssued = loans.Sum(loan => loan.ApprovedAmount),
        OutstandingBalances = loans.Sum(loan => loan.OutstandingBalance),
        Loans = loans
    };

    private async Task<ReportingWalletReportDto> GetWalletReportAsync(Guid stokvelId, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to, IReadOnlyDictionary<Guid, string> memberNames)
    {
        var wallets = await context.MemberSurplusWallets.AsNoTracking()
            .Where(wallet => wallet.StokvelId == stokvelId && memberIds.Contains(wallet.MemberId) && wallet.IsActive)
            .ToListAsync();
        var walletIds = wallets.Select(wallet => wallet.Id).ToList();
        var ledger = walletIds.Count == 0
            ? []
            : await context.MemberSurplusWalletTransactions.AsNoTracking()
                .Where(entry =>
                    walletIds.Contains(entry.WalletId) &&
                    entry.CreatedAt.Date >= from &&
                    entry.CreatedAt.Date <= to)
                .OrderByDescending(entry => entry.CreatedAt)
                .ToListAsync();
        var ledgerLines = ledger
            .Select(entry => new ReportingWalletLedgerLineDto
            {
                Date = entry.CreatedAt,
                MemberName = memberNames.GetValueOrDefault(entry.MemberId, "Member"),
                TransactionType = entry.TransactionType.ToString(),
                SourceType = entry.SourceType.ToString(),
                Amount = entry.Amount,
                BalanceAfter = entry.BalanceAfterTransaction,
                Description = entry.Description ?? string.Empty
            })
            .ToList();

        var available = wallets.Sum(wallet => wallet.AvailableBalance);
        var surplus = wallets.Sum(wallet => wallet.SurplusEquityBalance);
        var locked = wallets.Sum(wallet => wallet.LockedSurplusEquityBalance);

        return new ReportingWalletReportDto
        {
            CoreMandatoryBalance = wallets.Sum(wallet => wallet.CoreSavingsBalance),
            SurplusEquityBalance = surplus,
            LockedSurplusBalance = locked,
            AvailableSurplusBalance = Math.Max(0, surplus - locked),
            ReconciliationStatus = available >= 0 && surplus >= locked ? "Balanced" : "Review required",
            LedgerEntries = ledgerLines
        };
    }

    private static ReportingEarlyPayoutReportDto GetEarlyPayoutReport(List<ReportingLoanLineDto> loans)
    {
        var requests = loans
            .Where(loan => loan.LoanType == MemberLoanType.AcceleratedRotationalPayout.ToString())
            .Select(loan => new ReportingEarlyPayoutLineDto
            {
                LoanId = loan.LoanId,
                MemberName = loan.MemberName,
                GrossAmount = loan.EarlyPayoutGrossAmount,
                AdjustedPayoutAmount = loan.EarlyPayoutNetAmount,
                ReserveAdjustmentAmount = loan.EarlyPayoutReserveAmount,
                ApprovalStatus = loan.Status,
                WorkflowStatus = loan.Status,
                RequestedAt = loan.RequestedAt
            })
            .ToList();

        return new ReportingEarlyPayoutReportDto
        {
            GrossPayoutAmount = requests.Sum(item => item.GrossAmount),
            AdjustedPayoutAmount = requests.Sum(item => item.AdjustedPayoutAmount),
            ReserveAdjustmentAmount = requests.Sum(item => item.ReserveAdjustmentAmount),
            Requests = requests
        };
    }

    private async Task<List<ReportingPayoutLineDto>> GetPayoutLinesAsync(Stokvel stokvel, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to, IReadOnlyDictionary<Guid, string> memberNames)
    {
        var rotationalPayouts = await context.RotationalPayouts.AsNoTracking()
            .Where(payout =>
                payout.StokvelId == stokvel.Id &&
                memberIds.Contains(payout.PayoutMemberId) &&
                payout.RequestedAt.Date >= from &&
                payout.RequestedAt.Date <= to &&
                payout.IsActive)
            .OrderByDescending(payout => payout.RequestedAt)
            .ToListAsync();
        var payouts = rotationalPayouts
            .Select(payout => new ReportingPayoutLineDto
            {
                PaidAt = payout.PaidAt,
                MemberName = memberNames.GetValueOrDefault(payout.PayoutMemberId, "Member"),
                PayoutType = "Rotational",
                Amount = payout.PayoutAmount,
                Status = payout.PayoutStatus.ToString(),
                Reference = payout.PaymentReference
            })
            .ToList();

        var claimRows = await context.FuneralClaims.AsNoTracking()
            .Where(claim =>
                claim.TenantId == stokvel.TenantId &&
                memberIds.Contains(claim.MemberId) &&
                (claim.SubmittedAt ?? claim.CreatedAt).Date >= from &&
                (claim.SubmittedAt ?? claim.CreatedAt).Date <= to)
            .OrderByDescending(claim => claim.SubmittedAt ?? claim.CreatedAt)
            .ToListAsync();
        var claims = claimRows
            .Select(claim => new ReportingPayoutLineDto
            {
                PaidAt = claim.PayoutPaidAt,
                MemberName = memberNames.GetValueOrDefault(claim.MemberId, "Member"),
                PayoutType = "Claim",
                Amount = claim.PayoutAmount ?? 0,
                Status = claim.Status.ToString(),
                Reference = claim.PayoutReference
            })
            .ToList();

        payouts.AddRange(claims);
        return payouts.OrderByDescending(item => item.PaidAt ?? DateTime.MinValue).ToList();
    }

    private async Task<ReportingAttendanceReportDto> GetAttendanceReportAsync(Guid tenantId, IReadOnlyCollection<Guid> memberIds, DateTime from, DateTime to, IReadOnlyDictionary<Guid, string> memberNames)
    {
        var attendanceRows = await context.MeetingAttendances.AsNoTracking()
            .Include(attendance => attendance.Meeting)
            .Where(attendance =>
                memberIds.Contains(attendance.MemberId) &&
                attendance.Meeting.TenantId == tenantId &&
                attendance.Meeting.MeetingDate.Date >= from &&
                attendance.Meeting.MeetingDate.Date <= to)
            .OrderByDescending(attendance => attendance.Meeting.MeetingDate)
            .ToListAsync();
        var lines = attendanceRows
            .Select(attendance => new ReportingAttendanceLineDto
            {
                MeetingId = attendance.MeetingId,
                MeetingDate = attendance.Meeting.MeetingDate,
                MeetingTitle = attendance.Meeting.Title,
                MemberName = memberNames.GetValueOrDefault(attendance.MemberId, "Member"),
                AttendanceStatus = attendance.Status.ToString(),
                IsLate = attendance.IsLate,
                ApologyStatus = attendance.Status == AttendanceStatus.Apology ? "Submitted" : "-",
                ApologyType = string.Empty
            })
            .ToList();

        var apologies = await context.MeetingApologies.AsNoTracking()
            .Include(apology => apology.Meeting)
            .Where(apology =>
                memberIds.Contains(apology.MemberId) &&
                apology.Meeting != null &&
                apology.Meeting.TenantId == tenantId &&
                apology.SubmittedAt.Date >= from &&
                apology.SubmittedAt.Date <= to)
            .ToListAsync();
        foreach (var line in lines)
        {
            var match = apologies.FirstOrDefault(item => item.MeetingId == line.MeetingId && memberNames.GetValueOrDefault(item.MemberId, "Member") == line.MemberName);
            if (match is not null)
            {
                line.ApologyStatus = match.Status;
                line.ApologyType = match.ApologyType;
            }
        }

        return new ReportingAttendanceReportDto
        {
            MeetingCount = lines.Select(line => line.MeetingTitle + line.MeetingDate.ToString("O")).Distinct().Count(),
            PresentCount = lines.Count(line => line.AttendanceStatus == AttendanceStatus.Present.ToString()),
            AbsentCount = lines.Count(line => line.AttendanceStatus == AttendanceStatus.Absent.ToString()),
            ApologyCount = lines.Count(line => line.AttendanceStatus == AttendanceStatus.Apology.ToString() || line.ApologyStatus is not "-" and not ""),
            Lines = lines
        };
    }

    private async Task<List<ContributionArrearsLineDto>> GetMonthlyContributionStatementLinesAsync(Guid tenantId, Guid memberId)
    {
        var contributions = await context.MemberContributions
            .Include(contribution => contribution.ContributionCycle)
            .Where(contribution =>
                contribution.MemberId == memberId &&
                contribution.TenantId == tenantId)
            .OrderByDescending(contribution => contribution.ContributionCycle.PeriodStart)
            .ThenByDescending(contribution => contribution.CreatedAt)
            .ToListAsync();

        var contributionIds = contributions.Select(contribution => contribution.Id).ToList();
        var payments = contributionIds.Count == 0
            ? []
            : await context.Payments
                .Where(payment => contributionIds.Contains(payment.MemberContributionId))
                .OrderByDescending(payment => payment.PaymentDate)
                .ThenByDescending(payment => payment.CreatedAt)
                .ToListAsync();
        var latestPaymentByContributionId = payments
            .GroupBy(payment => payment.MemberContributionId)
            .ToDictionary(group => group.Key, group => group.First());

        return contributions
            .Select(contribution =>
            {
                latestPaymentByContributionId.TryGetValue(contribution.Id, out var latestPayment);
                var effectiveStatus = GetEffectivePaymentStatus(contribution.Status, contribution.ContributionCycle.DueDate);

                return new ContributionArrearsLineDto
                {
                    ContributionId = contribution.Id,
                    Year = contribution.ContributionCycle.PeriodStart.Year,
                    Month = contribution.ContributionCycle.PeriodStart.Month,
                    MonthYear = contribution.ContributionCycle.PeriodStart.ToString("MMMM yyyy"),
                    ExpectedAmount = contribution.ExpectedAmount,
                    PaidAmount = contribution.PaidAmount,
                    Balance = contribution.OutstandingAmount,
                    DueDate = contribution.ContributionCycle.DueDate,
                    Status = effectiveStatus == PaymentStatus.Late ? "Overdue" : effectiveStatus.ToString(),
                    Reference = latestPayment?.Reference
                };
            })
            .ToList();
    }

    private async Task<List<ContributionArrearsLineDto>> GetRotationalContributionStatementLinesAsync(Guid stokvelId, Guid memberId)
    {
        var payments = await context.RotationalContributionPayments
            .AsNoTracking()
            .Include(payment => payment.Cycle)
            .Where(payment =>
                payment.StokvelId == stokvelId &&
                payment.MemberId == memberId &&
                payment.IsActive)
            .OrderByDescending(payment => payment.Cycle.CycleNumber)
            .ThenByDescending(payment => payment.Cycle.CycleStartDate)
            .ThenByDescending(payment => payment.CreatedAt)
            .ToListAsync();

        return payments
            .Select(payment =>
            {
                var cycle = payment.Cycle;
                var cycleStart = cycle?.CycleStartDate ?? DateTime.Today;
                var balance = Math.Max(0, payment.ExpectedAmount + payment.PenaltyAmount - payment.PaidAmount);

                return new ContributionArrearsLineDto
                {
                    ContributionId = payment.Id,
                    Year = cycleStart.Year,
                    Month = cycleStart.Month,
                    MonthYear = string.IsNullOrWhiteSpace(cycle?.CycleName)
                        ? cycleStart.ToString("MMMM yyyy")
                        : cycle.CycleName,
                    ExpectedAmount = payment.ExpectedAmount,
                    PaidAmount = payment.PaidAmount,
                    Balance = balance,
                    DueDate = cycle?.ContributionDueDate,
                    Status = GetRotationalPaymentStatementStatus(payment.PaymentStatus, cycle?.ContributionDueDate),
                    Reference = payment.ReferenceNumber
                };
            })
            .ToList();
    }

    private static PaymentStatus GetEffectivePaymentStatus(PaymentStatus status, DateTime dueDate)
    {
        if (dueDate < DateTime.Today && status is PaymentStatus.Unpaid or PaymentStatus.PartiallyPaid)
        {
            return PaymentStatus.Late;
        }

        return status;
    }

    private static string GetRotationalPaymentStatementStatus(ContributionPaymentStatus status, DateTime? dueDate)
    {
        if (status is ContributionPaymentStatus.Unpaid or ContributionPaymentStatus.PartiallyPaid &&
            dueDate.HasValue &&
            dueDate.Value.Date < DateTime.Today)
        {
            return "Overdue";
        }

        return status switch
        {
            ContributionPaymentStatus.PartiallyPaid => "PartiallyPaid",
            ContributionPaymentStatus.Late => "Overdue",
            ContributionPaymentStatus.Waived => "Exempted",
            _ => status.ToString()
        };
    }
}
