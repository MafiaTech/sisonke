using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Entities;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Dto;

namespace Sisonke.Web.Services;

public class ContributionPaymentService(ApplicationDbContext context, MemberAccessService memberAccessService, AuditLogService auditLogService)
{
    public async Task<List<MemberContribution>> EnsureMonthlyContributionRecordsAsync(
        Guid stokvelId,
        int year,
        int month)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return [];
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        var periodStart = new DateTime(year, month, 1);
        var periodEnd = periodStart.AddMonths(1).AddDays(-1);
        var rule = await context.ContributionRules
            .Where(contributionRule =>
                contributionRule.TenantId == stokvel.TenantId &&
                contributionRule.IsActive)
            .OrderByDescending(contributionRule => contributionRule.EffectiveFrom)
            .ThenByDescending(contributionRule => contributionRule.CreatedAt)
            .FirstOrDefaultAsync();

        var dueDay = Math.Clamp(rule?.DueDayOfMonth ?? 7, 1, DateTime.DaysInMonth(year, month));
        var cycle = await context.ContributionCycles
            .Where(existingCycle =>
                existingCycle.TenantId == stokvel.TenantId &&
                existingCycle.PeriodStart == periodStart)
            .OrderBy(existingCycle => existingCycle.CreatedAt)
            .FirstOrDefaultAsync();

        if (cycle is null)
        {
            cycle = new ContributionCycle
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                Name = $"{periodStart:MMMM yyyy}",
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                DueDate = new DateTime(year, month, dueDay),
                Status = ContributionCycleStatus.Open,
                CreatedAt = DateTime.UtcNow
            };

            context.ContributionCycles.Add(cycle);
            await context.SaveChangesAsync();
        }

        var members = await context.Members
            .Where(member =>
                member.TenantId == stokvel.TenantId &&
                member.Status == MemberStatus.Active)
            .OrderBy(member => member.FullName)
            .ToListAsync();

        var existingMemberIds = await context.MemberContributions
            .Where(memberContribution => memberContribution.ContributionCycleId == cycle.Id)
            .Select(memberContribution => memberContribution.MemberId)
            .ToListAsync();

        var expectedAmount = rule?.Amount ?? 0;
        var now = DateTime.UtcNow;
        var newContributions = members
            .Where(member => !existingMemberIds.Contains(member.Id))
            .Select(member => new MemberContribution
            {
                Id = Guid.NewGuid(),
                TenantId = stokvel.TenantId,
                ContributionCycleId = cycle.Id,
                MemberId = member.Id,
                ExpectedAmount = expectedAmount,
                PaidAmount = 0,
                OutstandingAmount = expectedAmount,
                Status = PaymentStatus.Unpaid,
                CreatedAt = now
            })
            .ToList();

        if (newContributions.Count > 0)
        {
            context.MemberContributions.AddRange(newContributions);
            await context.SaveChangesAsync();
        }

        return await GetMonthlyContributionsAsync(stokvelId, year, month);
    }

    public async Task<List<MemberContribution>> GetMonthlyContributionsAsync(
        Guid stokvelId,
        int year,
        int month)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return [];
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        var periodStart = new DateTime(year, month, 1);

        return await context.MemberContributions
            .Include(memberContribution => memberContribution.Member)
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution =>
                memberContribution.TenantId == stokvel.TenantId &&
                memberContribution.ContributionCycle.PeriodStart == periodStart)
            .OrderBy(memberContribution => memberContribution.Member.FullName)
            .ToListAsync();
    }

    public async Task<MemberContribution?> GetContributionPaymentByIdAsync(Guid contributionPaymentId)
    {
        return await context.MemberContributions
            .Include(memberContribution => memberContribution.Member)
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution => memberContribution.Id == contributionPaymentId)
            .OrderBy(memberContribution => memberContribution.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Guid?> GetStokvelIdForContributionPaymentAsync(Guid contributionPaymentId)
    {
        var contribution = await context.MemberContributions
            .Where(memberContribution => memberContribution.Id == contributionPaymentId)
            .OrderBy(memberContribution => memberContribution.CreatedAt)
            .FirstOrDefaultAsync();

        if (contribution is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == contribution.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        return stokvel?.Id;
    }

    public async Task<ContributionMonthSummaryDto> GetMonthlyContributionSummaryAsync(
        Guid stokvelId,
        int year,
        int month)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return new ContributionMonthSummaryDto();
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return new ContributionMonthSummaryDto();
        }

        var activeMemberCount = await context.Members.CountAsync(member =>
            member.TenantId == stokvel.TenantId &&
            member.Status == MemberStatus.Active);
        var rule = await context.ContributionRules
            .Where(contributionRule =>
                contributionRule.TenantId == stokvel.TenantId &&
                contributionRule.IsActive)
            .OrderByDescending(contributionRule => contributionRule.EffectiveFrom)
            .ThenByDescending(contributionRule => contributionRule.CreatedAt)
            .FirstOrDefaultAsync();
        var contributions = await GetMonthlyContributionsAsync(stokvelId, year, month);
        var generatedExpectedTotal = contributions.Sum(contribution => contribution.ExpectedAmount);
        var monthlyContributionAmount = rule?.Amount ??
            (activeMemberCount > 0 ? generatedExpectedTotal / activeMemberCount : 0);
        var expectedContributions = monthlyContributionAmount > 0
            ? monthlyContributionAmount * activeMemberCount
            : generatedExpectedTotal;
        var actualContributions = contributions.Sum(contribution => contribution.PaidAmount);
        var outstandingContributions = Math.Max(0, expectedContributions - actualContributions);
        var effectiveStatuses = contributions
            .Select(contribution => GetEffectivePaymentStatus(contribution.Status, contribution.ContributionCycle.DueDate))
            .ToList();

        return new ContributionMonthSummaryDto
        {
            ActiveMemberCount = activeMemberCount,
            MonthlyContributionAmount = monthlyContributionAmount,
            ExpectedContributions = expectedContributions,
            ActualContributions = actualContributions,
            OutstandingContributions = outstandingContributions,
            CollectionRatePercentage = expectedContributions > 0 ? actualContributions / expectedContributions * 100 : 0,
            TotalMembers = activeMemberCount,
            RecordsGenerated = contributions.Count,
            PaidCount = effectiveStatuses.Count(status => status == PaymentStatus.Paid),
            PartiallyPaidCount = effectiveStatuses.Count(status => status == PaymentStatus.PartiallyPaid),
            OverdueCount = effectiveStatuses.Count(status => status == PaymentStatus.Late),
            UnpaidCount = effectiveStatuses.Count(status => status == PaymentStatus.Unpaid),
            WaivedCount = effectiveStatuses.Count(status => status is PaymentStatus.Exempted or PaymentStatus.WrittenOff),
            TotalExpected = expectedContributions,
            TotalPaid = actualContributions,
            TotalOutstanding = outstandingContributions
        };
    }

    public async Task<List<ContributionMonthlyTrendDto>> GetContributionTrendsAsync(Guid stokvelId, int monthsBack = 6)
    {
        var stokvel = await context.Stokvels
            .AsNoTracking()
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        if (stokvel.EnableRotation)
        {
            return await GetRotationalContributionTrendsAsync(stokvelId, monthsBack);
        }

        var monthCount = Math.Max(1, monthsBack);
        var currentMonthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var startMonth = currentMonthStart.AddMonths(-(monthCount - 1));
        var trends = new List<ContributionMonthlyTrendDto>();

        for (var offset = 0; offset < monthCount; offset++)
        {
            var periodStart = startMonth.AddMonths(offset);
            var summary = await GetMonthlyContributionSummaryAsync(stokvelId, periodStart.Year, periodStart.Month);

            trends.Add(new ContributionMonthlyTrendDto
            {
                Year = periodStart.Year,
                Month = periodStart.Month,
                MonthName = periodStart.ToString("MMM yyyy"),
                ExpectedContributions = summary.ExpectedContributions,
                ActualContributions = summary.ActualContributions,
                OutstandingContributions = summary.OutstandingContributions,
                CollectionRatePercentage = summary.CollectionRatePercentage,
                PaidCount = summary.PaidCount,
                PartiallyPaidCount = summary.PartiallyPaidCount,
                OverdueCount = summary.OverdueCount,
                UnpaidCount = summary.UnpaidCount
            });
        }

        return trends;
    }

    private async Task<List<ContributionMonthlyTrendDto>> GetRotationalContributionTrendsAsync(Guid stokvelId, int cyclesBack)
    {
        var cycleCount = Math.Max(1, cyclesBack);
        var cycles = await context.RotationalContributionCycles
            .AsNoTracking()
            .Where(cycle =>
                cycle.StokvelId == stokvelId &&
                cycle.IsActive &&
                cycle.Status != RotationalCycleStatus.Draft &&
                cycle.Status != RotationalCycleStatus.Cancelled)
            .OrderByDescending(cycle => cycle.CycleNumber)
            .ThenByDescending(cycle => cycle.CycleStartDate)
            .Take(cycleCount)
            .ToListAsync();

        if (cycles.Count == 0)
        {
            return [];
        }

        var cycleIds = cycles.Select(cycle => cycle.Id).ToList();
        var payments = await context.RotationalContributionPayments
            .AsNoTracking()
            .Where(payment =>
                payment.StokvelId == stokvelId &&
                payment.IsActive &&
                cycleIds.Contains(payment.CycleId))
            .ToListAsync();

        var paymentsByCycle = payments
            .GroupBy(payment => payment.CycleId)
            .ToDictionary(group => group.Key, group => group.ToList());

        return cycles
            .OrderBy(cycle => cycle.CycleNumber)
            .ThenBy(cycle => cycle.CycleStartDate)
            .Select(cycle =>
            {
                paymentsByCycle.TryGetValue(cycle.Id, out var cyclePayments);
                cyclePayments ??= [];

                var expected = cyclePayments.Count > 0
                    ? cyclePayments.Sum(payment => payment.ExpectedAmount)
                    : cycle.ExpectedTotalContributionAmount;
                var actual = cyclePayments.Sum(payment => payment.PaidAmount);
                var outstanding = Math.Max(0, expected - actual);
                var dueDate = cycle.ContributionDueDate.Date;
                var isOverdue = dueDate < DateTime.Today;

                return new ContributionMonthlyTrendDto
                {
                    Year = cycle.CycleStartDate.Year,
                    Month = cycle.CycleStartDate.Month,
                    MonthName = string.IsNullOrWhiteSpace(cycle.CycleName)
                        ? $"Cycle {cycle.CycleNumber}"
                        : cycle.CycleName,
                    ExpectedContributions = expected,
                    ActualContributions = actual,
                    OutstandingContributions = outstanding,
                    CollectionRatePercentage = expected > 0 ? actual / expected * 100 : 0,
                    PaidCount = cyclePayments.Count(payment =>
                        payment.PaymentStatus is ContributionPaymentStatus.Paid or ContributionPaymentStatus.Waived),
                    PartiallyPaidCount = cyclePayments.Count(payment =>
                        payment.PaymentStatus == ContributionPaymentStatus.PartiallyPaid ||
                        (payment.PaymentStatus == ContributionPaymentStatus.Late && payment.PaidAmount > 0)),
                    OverdueCount = cyclePayments.Count(payment =>
                        payment.PaymentStatus == ContributionPaymentStatus.Late ||
                        (isOverdue &&
                            payment.PaymentStatus is ContributionPaymentStatus.Unpaid or ContributionPaymentStatus.PartiallyPaid)),
                    UnpaidCount = cyclePayments.Count(payment => payment.PaymentStatus == ContributionPaymentStatus.Unpaid)
                };
            })
            .ToList();
    }

    public async Task<MemberContribution?> GetMemberContributionForMonthAsync(
        Guid memberId,
        int year,
        int month)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return null;
        }

        var periodStart = new DateTime(year, month, 1);

        return await context.MemberContributions
            .Include(memberContribution => memberContribution.Member)
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution =>
                memberContribution.MemberId == memberId &&
                memberContribution.ContributionCycle.PeriodStart == periodStart)
            .OrderBy(memberContribution => memberContribution.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<List<ContributionPaymentSummary>> GetContributionsByMemberIdAsync(Guid memberId)
    {
        var contributions = await context.MemberContributions
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution => memberContribution.MemberId == memberId)
            .OrderByDescending(memberContribution => memberContribution.ContributionCycle.PeriodStart)
            .ThenByDescending(memberContribution => memberContribution.CreatedAt)
            .ToListAsync();

        if (contributions.Count == 0)
        {
            return [];
        }

        var contributionIds = contributions
            .Select(contribution => contribution.Id)
            .ToList();

        var latestPayments = await context.Payments
            .Where(payment => contributionIds.Contains(payment.MemberContributionId))
            .OrderByDescending(payment => payment.PaymentDate)
            .ThenByDescending(payment => payment.CreatedAt)
            .ToListAsync();

        var paymentsByContributionId = latestPayments
            .GroupBy(payment => payment.MemberContributionId)
            .ToDictionary(group => group.Key, group => group.First());

        return contributions
            .Select(contribution =>
            {
                paymentsByContributionId.TryGetValue(contribution.Id, out var latestPayment);

                return new ContributionPaymentSummary
                {
                    Id = contribution.Id,
                    ContributionYear = contribution.ContributionCycle.PeriodStart.Year,
                    ContributionMonth = contribution.ContributionCycle.PeriodStart.Month,
                    MonthYearLabel = contribution.ContributionCycle.PeriodStart.ToString("MMMM yyyy"),
                    ExpectedAmount = contribution.ExpectedAmount,
                    PaidAmount = contribution.PaidAmount,
                    Balance = contribution.OutstandingAmount,
                    Status = GetEffectivePaymentStatus(contribution.Status, contribution.ContributionCycle.DueDate),
                    PaidDate = contribution.FullyPaidDate ?? latestPayment?.PaymentDate,
                    PaymentReference = latestPayment?.Reference
                };
            })
            .ToList();
    }

    public async Task<ContributionPaymentSummary?> GetCurrentMonthContributionAsync(Guid memberId, Guid stokvelId)
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

        var memberBelongsToStokvel = await context.Members
            .AnyAsync(member =>
                member.Id == memberId &&
                member.TenantId == stokvel.TenantId);

        if (!memberBelongsToStokvel)
        {
            return null;
        }

        var currentYear = DateTime.Today.Year;
        var currentMonth = DateTime.Today.Month;
        var contributions = await GetContributionsByMemberIdAsync(memberId);

        return contributions.FirstOrDefault(contribution =>
            contribution.ContributionYear == currentYear &&
            contribution.ContributionMonth == currentMonth);
    }

    public async Task<List<ContributionPaymentSummary>> GetRecentContributionsByMemberIdAsync(Guid memberId, int take = 6)
    {
        var contributions = await GetContributionsByMemberIdAsync(memberId);

        return contributions
            .Take(Math.Max(1, take))
            .ToList();
    }

    public Task<int> MarkOverdueContributionsAsync(Guid stokvelId, int year, int month)
    {
        return MarkOverdueContributionsAsync(stokvelId, year, month, Guid.Empty);
    }

    public async Task<int> MarkOverdueContributionsAsync(Guid stokvelId, int year, int month, Guid capturedByMemberId)
    {
        if (year < 1 || month is < 1 or > 12)
        {
            return 0;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return 0;
        }

        var periodStart = new DateTime(year, month, 1);
        var today = DateTime.Today;
        var overdueContributions = await context.MemberContributions
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution =>
                memberContribution.TenantId == stokvel.TenantId &&
                memberContribution.ContributionCycle.PeriodStart == periodStart &&
                memberContribution.ContributionCycle.DueDate < today &&
                (memberContribution.Status == PaymentStatus.Unpaid ||
                    memberContribution.Status == PaymentStatus.PartiallyPaid))
            .ToListAsync();

        foreach (var contribution in overdueContributions)
        {
            var previousStatus = contribution.Status;
            contribution.Status = PaymentStatus.Late;
            context.ContributionPaymentAudits.Add(new ContributionPaymentAudit
            {
                Id = Guid.NewGuid(),
                ContributionPaymentId = contribution.Id,
                MemberId = contribution.MemberId,
                StokvelId = stokvel.Id,
                Action = "MarkedOverdue",
                PreviousAmountPaid = contribution.PaidAmount,
                NewAmountPaid = contribution.PaidAmount,
                PreviousStatus = GetAuditStatusText(previousStatus),
                NewStatus = "Overdue",
                CapturedByMemberId = capturedByMemberId,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (overdueContributions.Count > 0)
        {
            await context.SaveChangesAsync();
        }

        return overdueContributions.Count;
    }

    public async Task<List<MemberContribution>> GetOverdueContributionsByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return [];
        }

        var today = DateTime.Today;

        return await context.MemberContributions
            .Include(memberContribution => memberContribution.Member)
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution =>
                memberContribution.TenantId == stokvel.TenantId &&
                (memberContribution.Status == PaymentStatus.Late ||
                    (memberContribution.ContributionCycle.DueDate < today &&
                        memberContribution.Status == PaymentStatus.Unpaid) ||
                    (memberContribution.ContributionCycle.DueDate < today &&
                        memberContribution.Status == PaymentStatus.PartiallyPaid)))
            .OrderBy(memberContribution => memberContribution.ContributionCycle.DueDate)
            .ThenBy(memberContribution => memberContribution.Member.FullName)
            .ToListAsync();
    }

    public async Task<List<MemberContribution>> GetOverdueContributionsByMemberIdAsync(Guid memberId)
    {
        var today = DateTime.Today;

        return await context.MemberContributions
            .Include(memberContribution => memberContribution.Member)
            .Include(memberContribution => memberContribution.ContributionCycle)
            .Where(memberContribution =>
                memberContribution.MemberId == memberId &&
                (memberContribution.Status == PaymentStatus.Late ||
                    (memberContribution.ContributionCycle.DueDate < today &&
                        memberContribution.Status == PaymentStatus.Unpaid) ||
                    (memberContribution.ContributionCycle.DueDate < today &&
                        memberContribution.Status == PaymentStatus.PartiallyPaid)))
            .OrderBy(memberContribution => memberContribution.ContributionCycle.DueDate)
            .ThenBy(memberContribution => memberContribution.CreatedAt)
            .ToListAsync();
    }

    public async Task<decimal> GetTotalOutstandingByStokvelIdAsync(Guid stokvelId)
    {
        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.Id == stokvelId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null)
        {
            return 0;
        }

        return await context.MemberContributions
            .Where(memberContribution =>
                memberContribution.TenantId == stokvel.TenantId &&
                memberContribution.Status != PaymentStatus.Paid &&
                memberContribution.Status != PaymentStatus.Exempted &&
                memberContribution.Status != PaymentStatus.WrittenOff)
            .SumAsync(memberContribution => memberContribution.OutstandingAmount);
    }

    public async Task<MemberContribution?> CaptureContributionPaymentAsync(
        Guid memberContributionId,
        decimal amountPaid,
        string? paymentReference,
        string? notes,
        string capturedByUserId)
    {
        if (amountPaid <= 0 || string.IsNullOrWhiteSpace(capturedByUserId))
        {
            return null;
        }

        var memberContribution = await context.MemberContributions
            .Include(existingContribution => existingContribution.Member)
            .Where(existingContribution => existingContribution.Id == memberContributionId)
            .FirstOrDefaultAsync();

        if (memberContribution is null)
        {
            return null;
        }

        var stokvel = await context.Stokvels
            .Where(existingStokvel => existingStokvel.TenantId == memberContribution.TenantId)
            .OrderBy(existingStokvel => existingStokvel.CreatedAt)
            .ThenBy(existingStokvel => existingStokvel.Name)
            .FirstOrDefaultAsync();

        if (stokvel is null || !await memberAccessService.CanManagePaymentsAsync(capturedByUserId, stokvel.Id))
        {
            return null;
        }

        var capturedByMember = await memberAccessService.GetLinkedMemberForUserAsync(capturedByUserId, stokvel.Id);

        if (capturedByMember is null)
        {
            return null;
        }

        var previousAmountPaid = memberContribution.PaidAmount;
        var previousStatus = memberContribution.Status;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            TenantId = memberContribution.TenantId,
            MemberContributionId = memberContribution.Id,
            MemberId = memberContribution.MemberId,
            Amount = amountPaid,
            PaymentDate = DateTime.Today,
            Reference = paymentReference,
            CapturedByUserId = capturedByUserId,
            CreatedAt = DateTime.UtcNow
        };

        context.Payments.Add(payment);

        memberContribution.PaidAmount += amountPaid;
        memberContribution.OutstandingAmount = Math.Max(0, memberContribution.ExpectedAmount - memberContribution.PaidAmount);
        memberContribution.Status = memberContribution.OutstandingAmount <= 0
            ? PaymentStatus.Paid
            : PaymentStatus.PartiallyPaid;

        if (memberContribution.Status == PaymentStatus.Paid)
        {
            memberContribution.FullyPaidDate = DateTime.Today;
        }

        context.ContributionPaymentAudits.Add(new ContributionPaymentAudit
        {
            Id = Guid.NewGuid(),
            ContributionPaymentId = memberContribution.Id,
            MemberId = memberContribution.MemberId,
            StokvelId = stokvel.Id,
            Action = memberContribution.Status == PaymentStatus.Paid ? "MarkedPaid" : "PaymentCaptured",
            PreviousAmountPaid = previousAmountPaid,
            NewAmountPaid = memberContribution.PaidAmount,
            PreviousStatus = GetAuditStatusText(previousStatus),
            NewStatus = GetAuditStatusText(memberContribution.Status),
            PaymentReference = paymentReference,
            Notes = notes,
            CapturedByMemberId = capturedByMember.Id,
            CreatedAt = DateTime.UtcNow
        });

        await context.SaveChangesAsync();
        await auditLogService.RecordAsync(capturedByUserId, stokvel.Id, "ContributionPaymentCaptured", "MemberContribution", memberContribution.Id, $"Contribution payment captured for R {amountPaid:N2}.");

        return memberContribution;
    }

    public async Task<Dictionary<Guid, string?>> GetLatestCapturesByContributionIdsAsync(IEnumerable<Guid> contributionIds)
    {
        var ids = contributionIds.ToList();

        if (ids.Count == 0)
            return [];

        var latestAudits = await context.ContributionPaymentAudits
            .Include(audit => audit.CapturedByMember)
            .Where(audit =>
                ids.Contains(audit.ContributionPaymentId) &&
                audit.Action != "MarkedOverdue")
            .OrderByDescending(audit => audit.CreatedAt)
            .ToListAsync();

        return latestAudits
            .GroupBy(audit => audit.ContributionPaymentId)
            .ToDictionary(
                group => group.Key,
                group => group.First().CapturedByMember?.FullName);
    }

    public async Task<List<ContributionPaymentAudit>> GetAuditTrailByContributionPaymentIdAsync(Guid contributionPaymentId)
    {
        return await context.ContributionPaymentAudits
            .Include(audit => audit.ContributionPayment)
                .ThenInclude(contribution => contribution!.ContributionCycle)
            .Include(audit => audit.Member)
            .Include(audit => audit.Stokvel)
            .Include(audit => audit.CapturedByMember)
            .Where(audit => audit.ContributionPaymentId == contributionPaymentId)
            .OrderByDescending(audit => audit.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ContributionPaymentAudit>> GetAuditTrailByMemberIdAsync(Guid memberId, Guid stokvelId)
    {
        return await context.ContributionPaymentAudits
            .Include(audit => audit.ContributionPayment)
                .ThenInclude(contribution => contribution!.ContributionCycle)
            .Include(audit => audit.Member)
            .Include(audit => audit.Stokvel)
            .Include(audit => audit.CapturedByMember)
            .Where(audit =>
                audit.MemberId == memberId &&
                audit.StokvelId == stokvelId)
            .OrderByDescending(audit => audit.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ContributionPaymentAudit>> GetAuditTrailByStokvelIdAsync(Guid stokvelId, int take = 100)
    {
        return await context.ContributionPaymentAudits
            .Include(audit => audit.ContributionPayment)
                .ThenInclude(contribution => contribution!.ContributionCycle)
            .Include(audit => audit.Member)
            .Include(audit => audit.Stokvel)
            .Include(audit => audit.CapturedByMember)
            .Where(audit => audit.StokvelId == stokvelId)
            .OrderByDescending(audit => audit.CreatedAt)
            .Take(Math.Max(1, take))
            .ToListAsync();
    }

    private static PaymentStatus GetEffectivePaymentStatus(PaymentStatus status, DateTime dueDate)
    {
        if (dueDate < DateTime.Today && status is PaymentStatus.Unpaid or PaymentStatus.PartiallyPaid)
        {
            return PaymentStatus.Late;
        }

        return status;
    }

    private static string GetAuditStatusText(PaymentStatus status)
    {
        return status == PaymentStatus.Late ? "Overdue" : status.ToString();
    }
}

public sealed class ContributionPaymentSummary
{
    public Guid Id { get; set; }
    public int ContributionYear { get; set; }
    public int ContributionMonth { get; set; }
    public string MonthYearLabel { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }
    public PaymentStatus Status { get; set; }
    public DateTime? PaidDate { get; set; }
    public string? PaymentReference { get; set; }
}
