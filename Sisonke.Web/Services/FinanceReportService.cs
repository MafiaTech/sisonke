using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class FinanceReportService(
    ApplicationDbContext context,
    ContributionPaymentService contributionPaymentService,
    FineService fineService,
    FuneralClaimService funeralClaimService)
{
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
