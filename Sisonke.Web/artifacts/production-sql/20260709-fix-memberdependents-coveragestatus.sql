/*
Production-safe patch for MemberDependents.CoverageStatus schema drift.

Expected application model:
  Sisonke.Web.Data.Enums.DependentCoverageStatus
  Pending = 1, Active = 2, Rejected = 3, Removed = 4

Purpose:
  Convert dbo.MemberDependents.CoverageStatus from nvarchar/string storage to int
  without dropping the table or deleting rows.

Safety:
  - No table drop.
  - No row delete.
  - Aborts if unknown non-null CoverageStatus values exist.
  - Preserves known string and numeric values.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @schemaName sysname = N'dbo';
DECLARE @tableName sysname = N'MemberDependents';
DECLARE @columnName sysname = N'CoverageStatus';
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
    THROW 51000, 'dbo.MemberDependents.CoverageStatus was not found.', 1;
END;

IF @currentType = N'int'
BEGIN
    PRINT 'dbo.MemberDependents.CoverageStatus is already int. No conversion required.';
    COMMIT TRANSACTION;
    RETURN;
END;

IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
BEGIN
    THROW 51001, 'dbo.MemberDependents.CoverageStatus has an unexpected type. Review manually before conversion.', 1;
END;

IF EXISTS
(
    SELECT 1
    FROM dbo.MemberDependents
    WHERE CoverageStatus IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(nvarchar(100), CoverageStatus))) <> N''
      AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), CoverageStatus)))) NOT IN
      (
          N'1', N'2', N'3', N'4',
          N'PENDING', N'ACTIVE', N'REJECTED', N'REMOVED'
      )
)
BEGIN
    SELECT DISTINCT CoverageStatus AS UnknownCoverageStatus
    FROM dbo.MemberDependents
    WHERE CoverageStatus IS NOT NULL
      AND LTRIM(RTRIM(CONVERT(nvarchar(100), CoverageStatus))) <> N''
      AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), CoverageStatus)))) NOT IN
      (
          N'1', N'2', N'3', N'4',
          N'PENDING', N'ACTIVE', N'REJECTED', N'REMOVED'
      );

    THROW 51002, 'Unknown CoverageStatus values found. Resolve or map them before conversion.', 1;
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
    SET @sql = N'ALTER TABLE dbo.MemberDependents DROP CONSTRAINT ' + QUOTENAME(@defaultConstraintName) + N';';
    EXEC sp_executesql @sql;
END;

UPDATE dbo.MemberDependents
SET CoverageStatus =
    CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), CoverageStatus))))
        WHEN N'1' THEN N'1'
        WHEN N'PENDING' THEN N'1'
        WHEN N'2' THEN N'2'
        WHEN N'ACTIVE' THEN N'2'
        WHEN N'3' THEN N'3'
        WHEN N'REJECTED' THEN N'3'
        WHEN N'4' THEN N'4'
        WHEN N'REMOVED' THEN N'4'
        ELSE N'2'
    END;

ALTER TABLE dbo.MemberDependents
ALTER COLUMN CoverageStatus int NOT NULL;

ALTER TABLE dbo.MemberDependents
ADD CONSTRAINT DF_MemberDependents_CoverageStatus DEFAULT (2) FOR CoverageStatus;

COMMIT TRANSACTION;

SELECT
    CoverageStatus,
    COUNT(*) AS RowCount
FROM dbo.MemberDependents
GROUP BY CoverageStatus
ORDER BY CoverageStatus;
