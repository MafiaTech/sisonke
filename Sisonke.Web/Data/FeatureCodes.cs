namespace Sisonke.Web.Data;

/// <summary>
/// Canonical FeatureDefinition.Code values from the plan/feature catalogue. All entitlement
/// checks (Phase 2 IEntitlementService) and feature gates must reference these constants —
/// a plan-name conditional (if (plan == "Professional")) anywhere in feature code is a defect.
/// </summary>
public static class FeatureCodes
{
    // Limits
    public const string MaxMembers = "MAX_MEMBERS";
    public const string MaxAdministrators = "MAX_ADMINISTRATORS";
    public const string MaxSchemes = "MAX_SCHEMES";
    public const string StorageMb = "STORAGE_MB";

    // Core
    public const string MemberManagement = "MEMBER_MANAGEMENT";
    public const string DependentManagement = "DEPENDENT_MANAGEMENT";
    public const string TaskManagement = "TASK_MANAGEMENT";
    public const string RotationalStokvel = "ROTATIONAL_STOKVEL";
    public const string DocumentStorage = "DOCUMENT_STORAGE";

    // Governance
    public const string Meetings = "MEETINGS";
    public const string Voting = "VOTING";
    public const string AttendanceWarnings = "ATTENDANCE_WARNINGS";
    public const string AiMeetingMinutes = "AI_MEETING_MINUTES";
    /// <summary>
    /// Configuring stages, approvers, role sequence, thresholds or approval rules. Executing an
    /// already-configured approval workflow is governed by the relevant module feature instead.
    /// </summary>
    public const string ConfigurableApprovalWorkflows = "CONFIGURABLE_APPROVAL_WORKFLOWS";

    // Finance
    public const string ContributionTracking = "CONTRIBUTION_TRACKING";
    public const string LoansAndWithdrawals = "LOANS_AND_WITHDRAWALS";
    public const string FinanceSummariesArrears = "FINANCE_SUMMARIES_ARREARS";
    public const string ControlledPayouts = "CONTROLLED_PAYOUTS";
    public const string MultipleContributionStructures = "MULTIPLE_CONTRIBUTION_STRUCTURES";
    public const string SurplusWallet = "SURPLUS_WALLET";

    // Claims
    /// <summary>Own-claim viewing, creation/submission, status and basic document handling.</summary>
    public const string ClaimsBasic = "CLAIMS_BASIC";

    /// <summary>Secretary review, chairperson decision, treasurer payout and claim workflow administration.</summary>
    public const string ClaimsFullWorkflow = "CLAIMS_FULL_WORKFLOW";

    // Notifications
    public const string EmailNotifications = "EMAIL_NOTIFICATIONS";
    public const string WhatsAppNotifications = "WHATSAPP_NOTIFICATIONS";

    // Reporting
    public const string ExportExcelPdf = "EXPORT_EXCEL_PDF";
    public const string StandardReporting = "STANDARD_REPORTING";
    public const string AdvancedReporting = "ADVANCED_REPORTING";
    public const string ConsolidatedReporting = "CONSOLIDATED_REPORTING";

    // Payments
    public const string ManualPaymentRecording = "MANUAL_PAYMENT_RECORDING";
    public const string PaymentLinks = "PAYMENT_LINKS";
    public const string TreasurerReconciliationTools = "TREASURER_RECONCILIATION_TOOLS";
    public const string OnlineMemberPayments = "ONLINE_MEMBER_PAYMENTS";
    public const string AutomatedRecurringCollections = "AUTOMATED_RECURRING_COLLECTIONS";
    public const string AutomatedReconciliation = "AUTOMATED_RECONCILIATION";
    public const string FailedPaymentFollowup = "FAILED_PAYMENT_FOLLOWUP";
    public const string SettlementReports = "SETTLEMENT_REPORTS";

    // Platform
    public const string ApiAccess = "API_ACCESS";
    public const string MultipleSchemes = "MULTIPLE_SCHEMES";
    public const string FuneralParlourManagement = "FUNERAL_PARLOUR_MANAGEMENT";
    public const string CustomBranding = "CUSTOM_BRANDING";
    public const string CustomDomain = "CUSTOM_DOMAIN";
    public const string RoleCustomisation = "ROLE_CUSTOMISATION";
    public const string WhiteLabel = "WHITE_LABEL";

    // Support
    public const string DataMigrationSupport = "DATA_MIGRATION_SUPPORT";
    public const string SupportTier = "SUPPORT_TIER";

    // ── Operation classifiers ──────────────────────────────────────────────
    // Not plan-catalogue features (no FeatureDefinition/PlanFeature row) — these exist purely
    // so IEntitlementService.AuthorizeAsync has something to key the Restricted/Suspended
    // write-block and the always-allowed set on. Outside of Restricted/Suspended they always
    // evaluate as allowed, since nothing in the catalogue governs them.
    public const string OpSendBulkNotification = "OP_SEND_BULK_NOTIFICATION";
    public const string OpProcessPayout = "OP_PROCESS_PAYOUT";
    public const string OpCreateFinancialTransaction = "OP_CREATE_FINANCIAL_TRANSACTION";
    public const string OpBillingPageAccess = "OP_BILLING_PAGE_ACCESS";
    public const string OpSubscriptionPageAccess = "OP_SUBSCRIPTION_PAGE_ACCESS";
    public const string OpAccountDataExport = "OP_ACCOUNT_DATA_EXPORT";
    public const string OpInvoiceAccess = "OP_INVOICE_ACCESS";
    public const string OpSupportContact = "OP_SUPPORT_CONTACT";
    public const string OpReadOnlyRecordViewing = "OP_READ_ONLY_RECORD_VIEWING";

    /// <summary>
    /// Never gated, regardless of subscription status — checked before any status rule.
    /// Brief: "billing page, subscription page, data export, invoice access, support contact,
    /// read-only record viewing." OpAccountDataExport is deliberately distinct from
    /// ExportExcelPdf (a paid reporting feature) — this is about being able to get your own
    /// data out during a suspension, not about the premium Excel/PDF report generator.
    /// </summary>
    public static readonly IReadOnlySet<string> AlwaysAllowed = new HashSet<string>
    {
        OpBillingPageAccess,
        OpSubscriptionPageAccess,
        OpAccountDataExport,
        OpInvoiceAccess,
        OpSupportContact,
        OpReadOnlyRecordViewing
    };

    /// <summary>
    /// Blocked while Restricted or Suspended (and treated the same while PendingPaymentMethod
    /// or Expired). Brief: "adding members, creating claims, starting loans, creating meetings,
    /// sending bulk notifications, processing payouts, creating new financial transactions."
    /// Maps onto the closest existing catalogue feature where one governs that exact write
    /// (MaxMembers/ClaimsBasic/LoansAndWithdrawals/Meetings); the remaining three have no
    /// catalogue equivalent so use the Op* classifiers above.
    /// </summary>
    public static readonly IReadOnlySet<string> WriteBlocked = new HashSet<string>
    {
        MaxMembers,
        ClaimsBasic,
        ClaimsFullWorkflow,
        ConfigurableApprovalWorkflows,
        LoansAndWithdrawals,
        Meetings,
        OpSendBulkNotification,
        OpProcessPayout,
        OpCreateFinancialTransaction
    };
}
