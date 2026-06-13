-- ============================================================
-- sp_GetStokvelDashboardSummary
-- Returns a single-row dashboard summary for a given tenant.
-- Read-only. Safe to call from reporting/dashboard layers.
--
-- Enum reference (C# Sisonke.Web.Data.Enums):
--   MemberStatus         : Active=2, Deceased=6
--   MemberGovernanceStatus: Active=1
--   ContributionCycleStatus: Open=2
--   FuneralClaimStatus   : Paid=7, Rejected=6, Cancelled=8
--   MeetingStatus        : Planned=0, Scheduled=1
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetStokvelDashboardSummary]
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        -- Member counts
        (
            SELECT COUNT(*)
            FROM [Members]
            WHERE TenantId = @TenantId
              AND [Status] <> 6   -- not Deceased
        ) AS TotalMembers,

        (
            SELECT COUNT(*)
            FROM [Members]
            WHERE TenantId = @TenantId
              AND [Status] = 2    -- Active
              AND GovernanceStatus = 1  -- Active governance
        ) AS ActiveMembers,

        -- Contribution totals across open cycles
        ISNULL((
            SELECT SUM(mc.ExpectedAmount)
            FROM [MemberContributions] mc
            INNER JOIN [ContributionCycles] cc ON mc.ContributionCycleId = cc.Id
            WHERE mc.TenantId = @TenantId
              AND cc.TenantId = @TenantId
              AND cc.[Status] = 2  -- Open
        ), 0) AS TotalContributionsExpected,

        ISNULL((
            SELECT SUM(mc.PaidAmount)
            FROM [MemberContributions] mc
            INNER JOIN [ContributionCycles] cc ON mc.ContributionCycleId = cc.Id
            WHERE mc.TenantId = @TenantId
              AND cc.TenantId = @TenantId
              AND cc.[Status] = 2  -- Open
        ), 0) AS TotalContributionsPaid,

        ISNULL((
            SELECT SUM(mc.OutstandingAmount)
            FROM [MemberContributions] mc
            INNER JOIN [ContributionCycles] cc ON mc.ContributionCycleId = cc.Id
            WHERE mc.TenantId = @TenantId
              AND cc.TenantId = @TenantId
              AND cc.[Status] = 2  -- Open
        ), 0) AS TotalContributionsOutstanding,

        -- Open (unresolved) funeral claims
        (
            SELECT COUNT(*)
            FROM [FuneralClaims]
            WHERE TenantId = @TenantId
              AND [Status] NOT IN (6, 7, 8)  -- not Rejected, Paid, Cancelled
        ) AS OpenClaims,

        -- Upcoming meetings (today or future, not yet completed/cancelled)
        (
            SELECT COUNT(*)
            FROM [Meetings]
            WHERE TenantId = @TenantId
              AND MeetingDate >= CAST(GETUTCDATE() AS DATE)
              AND [Status] IN (0, 1)  -- Planned or Scheduled
        ) AS UpcomingMeetings;
END;
GO
