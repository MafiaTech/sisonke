/*
Production-safe patch for FuneralClaims enum schema drift.

Expected application model:
  dbo.FuneralClaims.Status       int  -- FuneralClaimStatus
  dbo.FuneralClaims.SubjectType  int  -- FuneralClaimSubjectType
  dbo.FuneralClaims.ClaimType    int  -- ClaimType
  dbo.FuneralClaimDocuments.DocumentType int -- ClaimDocumentType

Purpose:
  Convert string-backed enum columns to int without dropping tables or deleting rows.

Safety:
  - Idempotent for columns already stored as int.
  - Aborts if unknown non-null enum values exist.
  - Preserves known string and numeric values.
*/

SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @schemaName sysname = N'dbo';
DECLARE @sql nvarchar(max);
DECLARE @defaultConstraintName sysname;
DECLARE @currentType sysname;

/* dbo.FuneralClaims.Status: Draft=1, Submitted=2, UnderReview=3, OnHold=4, Approved=5, Rejected=6, Paid=7, Cancelled=8 */
SELECT @currentType = TYPE_NAME(c.user_type_id)
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'Status';

IF @currentType IS NULL
BEGIN
    THROW 51100, 'dbo.FuneralClaims.Status was not found.', 1;
END;

IF @currentType <> N'int'
BEGIN
    IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
    BEGIN
        THROW 51101, 'dbo.FuneralClaims.Status has an unexpected type. Review manually before conversion.', 1;
    END;

    IF EXISTS (
        SELECT 1 FROM dbo.FuneralClaims
        WHERE Status IS NOT NULL
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), Status))) <> N''
          AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), Status)))) NOT IN
          (N'1', N'2', N'3', N'4', N'5', N'6', N'7', N'8',
           N'DRAFT', N'SUBMITTED', N'UNDERREVIEW', N'UNDER REVIEW', N'ONHOLD', N'ON HOLD',
           N'APPROVED', N'REJECTED', N'PAID', N'CANCELLED', N'CANCELED')
    )
    BEGIN
        SELECT DISTINCT Status AS UnknownStatus
        FROM dbo.FuneralClaims
        WHERE Status IS NOT NULL
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), Status))) <> N''
          AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), Status)))) NOT IN
          (N'1', N'2', N'3', N'4', N'5', N'6', N'7', N'8',
           N'DRAFT', N'SUBMITTED', N'UNDERREVIEW', N'UNDER REVIEW', N'ONHOLD', N'ON HOLD',
           N'APPROVED', N'REJECTED', N'PAID', N'CANCELLED', N'CANCELED');

        THROW 51102, 'Unknown dbo.FuneralClaims.Status values found. Resolve or map them before conversion.', 1;
    END;

    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'Status';

    IF @defaultConstraintName IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE dbo.FuneralClaims DROP CONSTRAINT ' + QUOTENAME(@defaultConstraintName) + N';';
        EXEC sp_executesql @sql;
    END;

    UPDATE dbo.FuneralClaims
    SET Status =
        CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), Status))))
            WHEN N'1' THEN N'1'
            WHEN N'DRAFT' THEN N'1'
            WHEN N'2' THEN N'2'
            WHEN N'SUBMITTED' THEN N'2'
            WHEN N'3' THEN N'3'
            WHEN N'UNDERREVIEW' THEN N'3'
            WHEN N'UNDER REVIEW' THEN N'3'
            WHEN N'4' THEN N'4'
            WHEN N'ONHOLD' THEN N'4'
            WHEN N'ON HOLD' THEN N'4'
            WHEN N'5' THEN N'5'
            WHEN N'APPROVED' THEN N'5'
            WHEN N'6' THEN N'6'
            WHEN N'REJECTED' THEN N'6'
            WHEN N'7' THEN N'7'
            WHEN N'PAID' THEN N'7'
            WHEN N'8' THEN N'8'
            WHEN N'CANCELLED' THEN N'8'
            WHEN N'CANCELED' THEN N'8'
            ELSE N'1'
        END;

    ALTER TABLE dbo.FuneralClaims ALTER COLUMN Status int NOT NULL;
    ALTER TABLE dbo.FuneralClaims ADD CONSTRAINT DF_FuneralClaims_Status DEFAULT (1) FOR Status;
END;

/* dbo.FuneralClaims.SubjectType: Member=1, Dependent=2 */
SELECT @currentType = TYPE_NAME(c.user_type_id)
FROM sys.columns c
JOIN sys.tables t ON t.object_id = c.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'SubjectType';

IF @currentType IS NULL
BEGIN
    THROW 51103, 'dbo.FuneralClaims.SubjectType was not found.', 1;
END;

IF @currentType <> N'int'
BEGIN
    IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
    BEGIN
        THROW 51104, 'dbo.FuneralClaims.SubjectType has an unexpected type. Review manually before conversion.', 1;
    END;

    IF EXISTS (
        SELECT 1 FROM dbo.FuneralClaims
        WHERE SubjectType IS NOT NULL
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), SubjectType))) <> N''
          AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), SubjectType)))) NOT IN (N'1', N'2', N'MEMBER', N'DEPENDENT')
    )
    BEGIN
        SELECT DISTINCT SubjectType AS UnknownSubjectType
        FROM dbo.FuneralClaims
        WHERE SubjectType IS NOT NULL
          AND LTRIM(RTRIM(CONVERT(nvarchar(100), SubjectType))) <> N''
          AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), SubjectType)))) NOT IN (N'1', N'2', N'MEMBER', N'DEPENDENT');

        THROW 51105, 'Unknown dbo.FuneralClaims.SubjectType values found. Resolve or map them before conversion.', 1;
    END;

    SELECT @defaultConstraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'SubjectType';

    IF @defaultConstraintName IS NOT NULL
    BEGIN
        SET @sql = N'ALTER TABLE dbo.FuneralClaims DROP CONSTRAINT ' + QUOTENAME(@defaultConstraintName) + N';';
        EXEC sp_executesql @sql;
    END;

    UPDATE dbo.FuneralClaims
    SET SubjectType =
        CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), SubjectType))))
            WHEN N'1' THEN N'1'
            WHEN N'MEMBER' THEN N'1'
            WHEN N'2' THEN N'2'
            WHEN N'DEPENDENT' THEN N'2'
            ELSE N'1'
        END;

    ALTER TABLE dbo.FuneralClaims ALTER COLUMN SubjectType int NOT NULL;
END;

/* dbo.FuneralClaims.ClaimType: Funeral=1 */
IF COL_LENGTH(N'dbo.FuneralClaims', N'ClaimType') IS NOT NULL
BEGIN
    SELECT @currentType = TYPE_NAME(c.user_type_id)
    FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'ClaimType';

    IF @currentType <> N'int'
    BEGIN
        IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
        BEGIN
            THROW 51106, 'dbo.FuneralClaims.ClaimType has an unexpected type. Review manually before conversion.', 1;
        END;

        IF EXISTS (
            SELECT 1 FROM dbo.FuneralClaims
            WHERE ClaimType IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType))) <> N''
              AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType)))) NOT IN (N'1', N'FUNERAL')
        )
        BEGIN
            SELECT DISTINCT ClaimType AS UnknownClaimType
            FROM dbo.FuneralClaims
            WHERE ClaimType IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType))) <> N''
              AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), ClaimType)))) NOT IN (N'1', N'FUNERAL');

            THROW 51107, 'Unknown dbo.FuneralClaims.ClaimType values found. Resolve or map them before conversion.', 1;
        END;

        SELECT @defaultConstraintName = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = @schemaName AND t.name = N'FuneralClaims' AND c.name = N'ClaimType';

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
                ELSE N'1'
            END;

        ALTER TABLE dbo.FuneralClaims ALTER COLUMN ClaimType int NOT NULL;
        ALTER TABLE dbo.FuneralClaims ADD CONSTRAINT DF_FuneralClaims_ClaimType DEFAULT (1) FOR ClaimType;
    END;
END;

/* dbo.FuneralClaimDocuments.DocumentType: DeathCertificate=1, BI1663=2, IdCopy=3, ProofOfMembership=4, ProofOfPayment=5, Other=99 */
IF COL_LENGTH(N'dbo.FuneralClaimDocuments', N'DocumentType') IS NOT NULL
BEGIN
    SELECT @currentType = TYPE_NAME(c.user_type_id)
    FROM sys.columns c
    JOIN sys.tables t ON t.object_id = c.object_id
    JOIN sys.schemas s ON s.schema_id = t.schema_id
    WHERE s.name = @schemaName AND t.name = N'FuneralClaimDocuments' AND c.name = N'DocumentType';

    IF @currentType <> N'int'
    BEGIN
        IF @currentType NOT IN (N'nvarchar', N'varchar', N'nchar', N'char')
        BEGIN
            THROW 51108, 'dbo.FuneralClaimDocuments.DocumentType has an unexpected type. Review manually before conversion.', 1;
        END;

        IF EXISTS (
            SELECT 1 FROM dbo.FuneralClaimDocuments
            WHERE DocumentType IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), DocumentType))) <> N''
              AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), DocumentType)))) NOT IN
              (N'1', N'2', N'3', N'4', N'5', N'99',
               N'DEATHCERTIFICATE', N'DEATH CERTIFICATE', N'BI1663', N'IDCOPY', N'ID COPY',
               N'PROOFOFMEMBERSHIP', N'PROOF OF MEMBERSHIP', N'PROOFOFPAYMENT', N'PROOF OF PAYMENT', N'OTHER')
        )
        BEGIN
            SELECT DISTINCT DocumentType AS UnknownDocumentType
            FROM dbo.FuneralClaimDocuments
            WHERE DocumentType IS NOT NULL
              AND LTRIM(RTRIM(CONVERT(nvarchar(100), DocumentType))) <> N''
              AND UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), DocumentType)))) NOT IN
              (N'1', N'2', N'3', N'4', N'5', N'99',
               N'DEATHCERTIFICATE', N'DEATH CERTIFICATE', N'BI1663', N'IDCOPY', N'ID COPY',
               N'PROOFOFMEMBERSHIP', N'PROOF OF MEMBERSHIP', N'PROOFOFPAYMENT', N'PROOF OF PAYMENT', N'OTHER');

            THROW 51109, 'Unknown dbo.FuneralClaimDocuments.DocumentType values found. Resolve or map them before conversion.', 1;
        END;

        SELECT @defaultConstraintName = dc.name
        FROM sys.default_constraints dc
        JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = @schemaName AND t.name = N'FuneralClaimDocuments' AND c.name = N'DocumentType';

        IF @defaultConstraintName IS NOT NULL
        BEGIN
            SET @sql = N'ALTER TABLE dbo.FuneralClaimDocuments DROP CONSTRAINT ' + QUOTENAME(@defaultConstraintName) + N';';
            EXEC sp_executesql @sql;
        END;

        UPDATE dbo.FuneralClaimDocuments
        SET DocumentType =
            CASE UPPER(LTRIM(RTRIM(CONVERT(nvarchar(100), DocumentType))))
                WHEN N'1' THEN N'1'
                WHEN N'DEATHCERTIFICATE' THEN N'1'
                WHEN N'DEATH CERTIFICATE' THEN N'1'
                WHEN N'2' THEN N'2'
                WHEN N'BI1663' THEN N'2'
                WHEN N'3' THEN N'3'
                WHEN N'IDCOPY' THEN N'3'
                WHEN N'ID COPY' THEN N'3'
                WHEN N'4' THEN N'4'
                WHEN N'PROOFOFMEMBERSHIP' THEN N'4'
                WHEN N'PROOF OF MEMBERSHIP' THEN N'4'
                WHEN N'5' THEN N'5'
                WHEN N'PROOFOFPAYMENT' THEN N'5'
                WHEN N'PROOF OF PAYMENT' THEN N'5'
                WHEN N'99' THEN N'99'
                WHEN N'OTHER' THEN N'99'
                ELSE N'99'
            END;

        ALTER TABLE dbo.FuneralClaimDocuments ALTER COLUMN DocumentType int NOT NULL;
    END;
END;

COMMIT TRANSACTION;

SELECT
    Status,
    COUNT(*) AS RowCount
FROM dbo.FuneralClaims
GROUP BY Status
ORDER BY Status;

SELECT
    SubjectType,
    COUNT(*) AS RowCount
FROM dbo.FuneralClaims
GROUP BY SubjectType
ORDER BY SubjectType;

IF COL_LENGTH(N'dbo.FuneralClaims', N'ClaimType') IS NOT NULL
BEGIN
    SELECT ClaimType, COUNT(*) AS RowCount
    FROM dbo.FuneralClaims
    GROUP BY ClaimType
    ORDER BY ClaimType;
END;

IF COL_LENGTH(N'dbo.FuneralClaimDocuments', N'DocumentType') IS NOT NULL
BEGIN
    SELECT DocumentType, COUNT(*) AS RowCount
    FROM dbo.FuneralClaimDocuments
    GROUP BY DocumentType
    ORDER BY DocumentType;
END;
