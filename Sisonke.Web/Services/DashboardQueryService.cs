using System.Data;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;

namespace Sisonke.Web.Services;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public sealed class StokvelDashboardSummary
{
    public int TotalMembers { get; set; }
    public int ActiveMembers { get; set; }
    public decimal TotalContributionsExpected { get; set; }
    public decimal TotalContributionsPaid { get; set; }
    public decimal TotalContributionsOutstanding { get; set; }
    public int OpenClaims { get; set; }
    public int UpcomingMeetings { get; set; }
}

public sealed class MemberContributionStatusRow
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberNumber { get; set; } = string.Empty;
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ExpectedAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int StatusCode { get; set; }
}

public sealed class OutstandingContributionRow
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string MemberNumber { get; set; } = string.Empty;
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime DueDate { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int StatusCode { get; set; }
}

// ── Service ───────────────────────────────────────────────────────────────────

/// <summary>
/// Read-only reporting service for dashboard and contribution queries.
/// On SQL Server (Azure) calls stored procedures for performance.
/// On SQLite (local dev) falls back to equivalent LINQ queries.
/// </summary>
public sealed class DashboardQueryService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DashboardQueryService> _logger;

    public DashboardQueryService(ApplicationDbContext context, ILogger<DashboardQueryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private bool IsSqlServer =>
        string.Equals(_context.Database.ProviderName,
            "Microsoft.EntityFrameworkCore.SqlServer",
            StringComparison.OrdinalIgnoreCase);

    // ── GetStokvelDashboardSummary ────────────────────────────────────────────

    public async Task<StokvelDashboardSummary> GetStokvelDashboardSummaryAsync(Guid tenantId)
    {
        if (IsSqlServer)
        {
            try
            {
                return await GetDashboardSummaryViaSpAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "sp_GetStokvelDashboardSummary failed for tenant {TenantId}; falling back to LINQ.", tenantId);
            }
        }

        return await GetDashboardSummaryViaLinqAsync(tenantId);
    }

    private async Task<StokvelDashboardSummary> GetDashboardSummaryViaSpAsync(Guid tenantId)
    {
        var conn = _context.Database.GetDbConnection();
        var wasOpen = conn.State == ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();

        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetStokvelDashboardSummary";
            cmd.CommandType = CommandType.StoredProcedure;
            AddGuidParam(cmd, "@TenantId", tenantId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
                return new StokvelDashboardSummary();

            return new StokvelDashboardSummary
            {
                TotalMembers = reader.GetInt32(reader.GetOrdinal("TotalMembers")),
                ActiveMembers = reader.GetInt32(reader.GetOrdinal("ActiveMembers")),
                TotalContributionsExpected = reader.GetDecimal(reader.GetOrdinal("TotalContributionsExpected")),
                TotalContributionsPaid = reader.GetDecimal(reader.GetOrdinal("TotalContributionsPaid")),
                TotalContributionsOutstanding = reader.GetDecimal(reader.GetOrdinal("TotalContributionsOutstanding")),
                OpenClaims = reader.GetInt32(reader.GetOrdinal("OpenClaims")),
                UpcomingMeetings = reader.GetInt32(reader.GetOrdinal("UpcomingMeetings")),
            };
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }
    }

    private async Task<StokvelDashboardSummary> GetDashboardSummaryViaLinqAsync(Guid tenantId)
    {
        var totalMembers = await _context.Members
            .CountAsync(m => m.TenantId == tenantId && m.Status != MemberStatus.Deceased);

        var activeMembers = await _context.Members
            .CountAsync(m => m.TenantId == tenantId
                          && m.Status == MemberStatus.Active
                          && m.GovernanceStatus == MemberGovernanceStatus.Active);

        var openCycleIds = await _context.ContributionCycles
            .Where(cc => cc.TenantId == tenantId && cc.Status == ContributionCycleStatus.Open)
            .Select(cc => cc.Id)
            .ToListAsync();

        decimal expectedTotal = 0, paidTotal = 0, outstandingTotal = 0;

        if (openCycleIds.Count > 0)
        {
            var sums = await _context.MemberContributions
                .Where(mc => mc.TenantId == tenantId && openCycleIds.Contains(mc.ContributionCycleId))
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Expected = g.Sum(mc => mc.ExpectedAmount),
                    Paid = g.Sum(mc => mc.PaidAmount),
                    Outstanding = g.Sum(mc => mc.OutstandingAmount),
                })
                .FirstOrDefaultAsync();

            if (sums is not null)
            {
                expectedTotal = sums.Expected;
                paidTotal = sums.Paid;
                outstandingTotal = sums.Outstanding;
            }
        }

        var openClaims = await _context.FuneralClaims
            .CountAsync(c => c.TenantId == tenantId
                          && c.Status != FuneralClaimStatus.Paid
                          && c.Status != FuneralClaimStatus.Rejected
                          && c.Status != FuneralClaimStatus.Cancelled);

        var today = DateTime.UtcNow.Date;
        var upcomingMeetings = await _context.Meetings
            .CountAsync(m => m.TenantId == tenantId
                          && m.MeetingDate >= today
                          && (m.Status == MeetingStatus.Planned || m.Status == MeetingStatus.Scheduled));

        return new StokvelDashboardSummary
        {
            TotalMembers = totalMembers,
            ActiveMembers = activeMembers,
            TotalContributionsExpected = expectedTotal,
            TotalContributionsPaid = paidTotal,
            TotalContributionsOutstanding = outstandingTotal,
            OpenClaims = openClaims,
            UpcomingMeetings = upcomingMeetings,
        };
    }

    // ── GetMemberContributionStatus ───────────────────────────────────────────

    public async Task<List<MemberContributionStatusRow>> GetMemberContributionStatusAsync(Guid tenantId)
    {
        if (IsSqlServer)
        {
            try
            {
                return await GetContributionStatusViaSpAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "sp_GetMemberContributionStatus failed for tenant {TenantId}; falling back to LINQ.", tenantId);
            }
        }

        return await GetContributionStatusViaLinqAsync(tenantId);
    }

    private async Task<List<MemberContributionStatusRow>> GetContributionStatusViaSpAsync(Guid tenantId)
    {
        var conn = _context.Database.GetDbConnection();
        var wasOpen = conn.State == ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();

        var rows = new List<MemberContributionStatusRow>();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetMemberContributionStatus";
            cmd.CommandType = CommandType.StoredProcedure;
            AddGuidParam(cmd, "@TenantId", tenantId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new MemberContributionStatusRow
                {
                    MemberId = reader.GetGuid(reader.GetOrdinal("MemberId")),
                    MemberName = reader.GetString(reader.GetOrdinal("MemberName")),
                    MemberNumber = reader.GetString(reader.GetOrdinal("MemberNumber")),
                    CycleId = reader.GetGuid(reader.GetOrdinal("CycleId")),
                    CycleName = reader.GetString(reader.GetOrdinal("CycleName")),
                    PeriodStart = reader.GetDateTime(reader.GetOrdinal("PeriodStart")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    ExpectedAmount = reader.GetDecimal(reader.GetOrdinal("ExpectedAmount")),
                    PaidAmount = reader.GetDecimal(reader.GetOrdinal("PaidAmount")),
                    OutstandingAmount = reader.GetDecimal(reader.GetOrdinal("OutstandingAmount")),
                    StatusCode = reader.GetInt32(reader.GetOrdinal("StatusCode")),
                });
            }
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }

        return rows;
    }

    private async Task<List<MemberContributionStatusRow>> GetContributionStatusViaLinqAsync(Guid tenantId)
    {
        var openCycleIds = await _context.ContributionCycles
            .Where(cc => cc.TenantId == tenantId && cc.Status == ContributionCycleStatus.Open)
            .Select(cc => cc.Id)
            .ToListAsync();

        if (openCycleIds.Count == 0)
            return new List<MemberContributionStatusRow>();

        return await _context.MemberContributions
            .Where(mc => mc.TenantId == tenantId && openCycleIds.Contains(mc.ContributionCycleId))
            .Join(_context.Members,
                mc => mc.MemberId,
                m => m.Id,
                (mc, m) => new { mc, m })
            .Join(_context.ContributionCycles,
                x => x.mc.ContributionCycleId,
                cc => cc.Id,
                (x, cc) => new MemberContributionStatusRow
                {
                    MemberId = x.m.Id,
                    MemberName = x.m.FullName,
                    MemberNumber = x.m.MemberNumber,
                    CycleId = cc.Id,
                    CycleName = cc.Name,
                    PeriodStart = cc.PeriodStart,
                    DueDate = cc.DueDate,
                    ExpectedAmount = x.mc.ExpectedAmount,
                    PaidAmount = x.mc.PaidAmount,
                    OutstandingAmount = x.mc.OutstandingAmount,
                    StatusCode = (int)x.mc.Status,
                })
            .OrderBy(r => r.MemberName)
            .ThenBy(r => r.PeriodStart)
            .ToListAsync();
    }

    // ── GetOutstandingContributions ───────────────────────────────────────────

    public async Task<List<OutstandingContributionRow>> GetOutstandingContributionsAsync(Guid tenantId)
    {
        if (IsSqlServer)
        {
            try
            {
                return await GetOutstandingViaSpAsync(tenantId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "sp_GetOutstandingContributions failed for tenant {TenantId}; falling back to LINQ.", tenantId);
            }
        }

        return await GetOutstandingViaLinqAsync(tenantId);
    }

    private async Task<List<OutstandingContributionRow>> GetOutstandingViaSpAsync(Guid tenantId)
    {
        var conn = _context.Database.GetDbConnection();
        var wasOpen = conn.State == ConnectionState.Open;
        if (!wasOpen) await conn.OpenAsync();

        var rows = new List<OutstandingContributionRow>();
        try
        {
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "sp_GetOutstandingContributions";
            cmd.CommandType = CommandType.StoredProcedure;
            AddGuidParam(cmd, "@TenantId", tenantId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                rows.Add(new OutstandingContributionRow
                {
                    MemberId = reader.GetGuid(reader.GetOrdinal("MemberId")),
                    MemberName = reader.GetString(reader.GetOrdinal("MemberName")),
                    MemberNumber = reader.GetString(reader.GetOrdinal("MemberNumber")),
                    CycleId = reader.GetGuid(reader.GetOrdinal("CycleId")),
                    CycleName = reader.GetString(reader.GetOrdinal("CycleName")),
                    PeriodStart = reader.GetDateTime(reader.GetOrdinal("PeriodStart")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    OutstandingAmount = reader.GetDecimal(reader.GetOrdinal("OutstandingAmount")),
                    StatusCode = reader.GetInt32(reader.GetOrdinal("StatusCode")),
                });
            }
        }
        finally
        {
            if (!wasOpen) await conn.CloseAsync();
        }

        return rows;
    }

    private async Task<List<OutstandingContributionRow>> GetOutstandingViaLinqAsync(Guid tenantId)
    {
        var relevantStatuses = new[]
        {
            ContributionCycleStatus.Open,
            ContributionCycleStatus.Closed,
        };

        var outstandingPaymentStatuses = new[]
        {
            PaymentStatus.Unpaid,
            PaymentStatus.PartiallyPaid,
            PaymentStatus.Late,
        };

        return await _context.MemberContributions
            .Where(mc => mc.TenantId == tenantId
                      && outstandingPaymentStatuses.Contains(mc.Status)
                      && mc.OutstandingAmount > 0)
            .Join(_context.Members,
                mc => mc.MemberId,
                m => m.Id,
                (mc, m) => new { mc, m })
            .Join(_context.ContributionCycles.Where(cc => relevantStatuses.Contains(cc.Status)),
                x => x.mc.ContributionCycleId,
                cc => cc.Id,
                (x, cc) => new OutstandingContributionRow
                {
                    MemberId = x.m.Id,
                    MemberName = x.m.FullName,
                    MemberNumber = x.m.MemberNumber,
                    CycleId = cc.Id,
                    CycleName = cc.Name,
                    PeriodStart = cc.PeriodStart,
                    DueDate = cc.DueDate,
                    OutstandingAmount = x.mc.OutstandingAmount,
                    StatusCode = (int)x.mc.Status,
                })
            .OrderByDescending(r => r.OutstandingAmount)
            .ThenBy(r => r.DueDate)
            .ToListAsync();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void AddGuidParam(IDbCommand cmd, string name, Guid value)
    {
        var p = cmd.CreateParameter();
        p.ParameterName = name;
        p.Value = value;
        p.DbType = DbType.Guid;
        cmd.Parameters.Add(p);
    }
}
