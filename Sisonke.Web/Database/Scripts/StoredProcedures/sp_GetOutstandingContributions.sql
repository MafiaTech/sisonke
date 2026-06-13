-- ============================================================
-- sp_GetOutstandingContributions
-- Returns members who have an outstanding (unpaid or partial)
-- balance across open or recently closed cycles for a tenant.
-- Ordered by outstanding amount descending (worst first).
-- Read-only. Safe to call from reporting/dashboard layers.
--
-- Enum reference:
--   ContributionCycleStatus: Open=2, Closed=3
--   PaymentStatus: Unpaid=1, PartiallyPaid=2, Late=4
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetOutstandingContributions]
    @TenantId UNIQUEIDENTIFIER
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.Id              AS MemberId,
        m.FullName        AS MemberName,
        m.MemberNumber    AS MemberNumber,
        cc.Id             AS CycleId,
        cc.[Name]         AS CycleName,
        cc.PeriodStart,
        cc.DueDate,
        mc.OutstandingAmount,
        mc.[Status]       AS StatusCode
    FROM [MemberContributions] mc
    INNER JOIN [Members]            m  ON mc.MemberId           = m.Id
    INNER JOIN [ContributionCycles] cc ON mc.ContributionCycleId = cc.Id
    WHERE mc.TenantId       = @TenantId
      AND cc.TenantId       = @TenantId
      AND cc.[Status]       IN (2, 3)   -- Open or Closed (recently closed)
      AND mc.[Status]       IN (1, 2, 4) -- Unpaid, PartiallyPaid, Late
      AND mc.OutstandingAmount > 0
    ORDER BY mc.OutstandingAmount DESC, cc.DueDate ASC;
END;
GO
