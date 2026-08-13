/*
Production-safe patch for FuneralClaims.ClaimType schema drift.

Expected application model:
  Sisonke.Web.Data.Enums.ClaimType
  Funeral = 1, MemberDeath = 2, BeneficiaryDeath = 3, EmergencySupport = 4, Other = 5

Purpose:
  Convert dbo.FuneralClaims.ClaimType from nvarchar/string storage to int
  without dropping the table or deleting rows. Confirmed drift via:
    SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'FuneralClaims' AND COLUMN_NAME IN ('ClaimType','SubjectType','Status');
  which returned ClaimType = nvarchar (SubjectType and Status are already int).

  This is the same class of drift already fixed once for
  MemberDependents.CoverageStatus (see 20260709-fix-memberdependents-coveragestatus.sql) —
  this script mirrors that one's structure and safety checks exactly.

Safety:
  - No table drop.
  - No row delete.
  - Aborts if unknown non-null ClaimType values exist.
  - Preserves known string and numeric values.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @schemaName sysname = N'dbo';
DECLARE @tableName sysname = N'FuneralClaims';
DECLARE @columnName sysname = N'ClaimType';
DECLARE @currentType sysname;
DECLARE @sql nvarchar(max);
DECLARE @defaultConstraintName sysname;

SELECT @currentType = TYPE_NAME(c.user_type_id)
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName
  AND t.name = @tableName
  AND c.name = @columnName;

IF @currentType IS NULL
BEGIN
    THROW 51000, 'dbo.FuneralClaims.ClaimType was not found.', 1;
END;

IF @currentType = N'int'
BEGIN
    PRINT 'dbo.FuneralClaims.ClaimType is already int. No conversion required.';
    COMMIT TRANSACTION;
    RETURN;
END;

IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
BEGIN
    THROW 51001, 'dbo.FuneralClaims.ClaimType has an unexpected type. Review manually before conversion.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.FuneralClaims
    WHERE ClaimType IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType))) <> N''
      AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType)))) NOT IN
      (
          N'1', N'2', N'3', N'4', N'5',
          N'FUNERAL', N'MEMBERDEATH', N'BENEFICIARYDEATH', N'EMERGENCYSUPPORT', N'OTHER'
      )
)
BEGIN
    SELECT DISTINCT ClaimType AS UnknownClaimType
    FROM dbo.FuneralClaims
    WHERE ClaimType IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType))) <> N''
      AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType)))) NOT IN
      (
          N'1', N'2', N'3', N'4', N'5',
          N'FUNERAL', N'MEMBERDEATH', N'BENEFICIARYDEATH', N'EMERGENCYSUPPORT', N'OTHER'
      );

    THROW 51002, 'Unknown ClaimType values found. Resolve or map them before conversion.', 1;
END;

SELECT @defaultConstraintName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName
  AND t.name = @tableName
  AND c.name = @columnName;

IF @defaultConstraintName IS NOT NULL
BEGIN
    SET @sql = N'ALTER TABLE dbo.FuneralClaims DROP CONSTRAINT ' + QUOTENAME(@defaultConstraintName) + N';';
    EXEC sp_executesql @sql;
END;

UPDATE dbo.FuneralClaims
SET ClaimType =
    CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType))))
        WHEN N'1' THEN N'1'
        WHEN N'FUNERAL' THEN N'1'
        WHEN N'2' THEN N'2'
        WHEN N'MEMBERDEATH' THEN N'2'
        WHEN N'3' THEN N'3'
        WHEN N'BENEFICIARYDEATH' THEN N'3'
        WHEN N'4' THEN N'4'
        WHEN N'EMERGENCYSUPPORT' THEN N'4'
        WHEN N'5' THEN N'5'
        WHEN N'OTHER' THEN N'5'
        ELSE N'1'
    END;

ALTER TABLE dbo.FuneralClaims
ALTER COLUMN ClaimType int NOT NULL;

ALTER TABLE dbo.FuneralClaims
ADD CONSTRAINT DF_FuneralClaims_ClaimType DEFAULT (1) FOR ClaimType;

COMMIT TRANSACTION;

SELECT
    ClaimType,
    COUNT(*) AS RowCount
FROM dbo.FuneralClaims
GROUP BY ClaimType
ORDER BY ClaimType;
