/*
Diagnostic only — makes no changes. Checks every enum-backed column in the
codebase (C# enum property -> EF int column) against its actual SQL Server
data type, to find any other instances of the same nvarchar/int drift already
found and fixed on MemberDependents.CoverageStatus and FuneralClaims.ClaimType.

Run this, then paste the full result back — anything with DriftStatus =
'DRIFT - NEEDS FIX' gets its own guarded conversion script, same pattern as
the two already-fixed columns.
*/

;WITH ExpectedIntColumns AS (
    SELECT * FROM (VALUES
        ('Meetings', 'Status'),
        ('FuneralClaimDocuments', 'DocumentType'),
        ('MeetingAttendances', 'Status'),
        ('ContributionRules', 'Frequency'),
        ('FuneralClaims', 'SubjectType'),
        ('FuneralClaims', 'Status'),
        ('MeetingVoteResponses', 'Choice'),
        ('ContributionCycles', 'Status'),
        ('MemberLoanGuarantors', 'Status'),
        ('MemberLoanRepayments', 'PaymentMethod'),
        ('MemberLoanRepayments', 'PaymentStatus'),
        ('MeetingVotes', 'VotingMethod'),
        ('MeetingVotes', 'Status'),
        ('MeetingVotes', 'Result'),
        ('MemberContributions', 'Status'),
        ('MemberLoans', 'LoanType'),
        ('MemberLoans', 'LoanStatus'),
        ('MemberLoans', 'DisbursementMethod'),
        ('Members', 'Status'),
        ('Members', 'GovernanceStatus'),
        ('Members', 'DefaultRole'),
        ('MemberSurplusWalletTransactions', 'TransactionType'),
        ('MemberSurplusWalletTransactions', 'SourceType'),
        ('QuestionnaireQuestions', 'StokvelType'),
        ('QuestionnaireQuestions', 'QuestionType'),
        ('Payments', 'PaymentMethod'),
        ('MemberFines', 'Status'),
        ('NotificationMessages', 'Channel'),
        ('NotificationMessages', 'Type'),
        ('NotificationMessages', 'Status'),
        ('RotationalContributionCycles', 'Status'),
        ('RotationalContributionPayments', 'PaymentStatus'),
        ('RotationalContributionPayments', 'PaymentMethod'),
        ('RotationalPayouts', 'PayoutStatus'),
        ('RotationalPayouts', 'PaymentMethod'),
        ('MemberSurplusWithdrawalRequests', 'WithdrawalStatus'),
        ('MemberSurplusWithdrawalRequests', 'PaymentMethod'),
        ('RotationalStokvelConfigurations', 'ContributionFrequency'),
        ('RotationalStokvelConfigurations', 'PayoutFrequency'),
        ('RotationalStokvelConfigurations', 'RotationOrderMethod'),
        ('RotationalStokvelConfigurations', 'LatePenaltyType'),
        ('Stokvels', 'Type'),
        ('Stokvels', 'Archetype'),
        ('StokvelBankingDetails', 'AccountType'),
        ('StokvelLoanConfigurations', 'LoanInterestType'),
        ('StokvelLoanConfigurations', 'LateRepaymentFineType'),
        ('TenantSubscriptions', 'Status'),
        ('StokvelReserveTransactions', 'TransactionType')
    ) AS x(TableName, ColumnName)
)
SELECT
    e.TableName,
    e.ColumnName,
    c.DATA_TYPE AS ActualDataType,
    CASE
        WHEN c.DATA_TYPE IS NULL THEN 'TABLE/COLUMN NOT FOUND'
        WHEN c.DATA_TYPE = 'int' THEN 'OK'
        ELSE 'DRIFT - NEEDS FIX'
    END AS DriftStatus
FROM ExpectedIntColumns e
LEFT JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_NAME = e.TableName AND c.COLUMN_NAME = e.ColumnName
ORDER BY
    CASE
        WHEN c.DATA_TYPE IS NULL THEN 0
        WHEN c.DATA_TYPE <> 'int' THEN 1
        ELSE 2
    END,
    e.TableName, e.ColumnName;
