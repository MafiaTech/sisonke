-- ============================================================
-- sp_GetMemberContributionStatus
-- Returns contribution status per member per open cycle for
-- a given tenant. Ordered by member name then cycle start.
-- Read-only. Safe to call from reporting/dashboard layers.
--
-- Enum reference:
--   ContributionCycleStatus: Open=2
--   PaymentStatus: Unpaid=1, PartiallyPaid=2, Paid=3,
--                  Late=4, Exempted=5, WrittenOff=6, Reversed=7
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetMemberContributionStatus]
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.Id           AS MemberId,
        m.FullName     AS MemberName,
        m.MemberNumber AS MemberNumber,
        cc.Id          AS CycleId,
        cc.[Name]      AS CycleName,
        cc.PeriodStart,
        cc.DueDate,
        mc.ExpectedAmount,
        mc.PaidAmount,
        mc.OutstandingAmount,
        mc.[Status]    AS StatusCode
    FROM [MemberContributions] mc
    INNER JOIN [Members]            m  ON mc.MemberId           = m.Id
    INNER JOIN [ContributionCycles] cc ON mc.ContributionCycleId = cc.Id
    WHERE mc.TenantId  = @TenantId
      AND cc.TenantId  = @TenantId
      AND cc.[Status]  = 2  -- Open cycles only
    ORDER BY m.FullName ASC, cc.PeriodStart ASC;
END;
GO
