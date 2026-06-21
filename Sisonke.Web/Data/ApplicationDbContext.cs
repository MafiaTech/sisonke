using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Sisonke.Web.Data.Entities;

namespace Sisonke.Web.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Stokvel> Stokvels => Set<Stokvel>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<TenantSubscription> TenantSubscriptions => Set<TenantSubscription>();
    public DbSet<Member> Members => Set<Member>();
    public DbSet<MemberWarning> MemberWarnings => Set<MemberWarning>();
    public DbSet<NextOfKin> NextOfKinRecords => Set<NextOfKin>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<MemberDependent> MemberDependents => Set<MemberDependent>();
    public DbSet<FuneralClaim> FuneralClaims => Set<FuneralClaim>();
    public DbSet<FuneralClaimDocument> FuneralClaimDocuments => Set<FuneralClaimDocument>();
    public DbSet<FineType> FineTypes => Set<FineType>();
    public DbSet<MemberFine> MemberFines => Set<MemberFine>();
    public DbSet<ContributionRule> ContributionRules => Set<ContributionRule>();
    public DbSet<ContributionCycle> ContributionCycles => Set<ContributionCycle>();
    public DbSet<MemberContribution> MemberContributions => Set<MemberContribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<ContributionPaymentAudit> ContributionPaymentAudits => Set<ContributionPaymentAudit>();
    public DbSet<ClaimPayoutAudit> ClaimPayoutAudits => Set<ClaimPayoutAudit>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingAgendaItem> MeetingAgendaItems => Set<MeetingAgendaItem>();
    public DbSet<MeetingAttendance> MeetingAttendances => Set<MeetingAttendance>();
    public DbSet<MeetingApology> MeetingApologies => Set<MeetingApology>();
    public DbSet<MeetingMinute> MeetingMinutes => Set<MeetingMinute>();
    public DbSet<MeetingVote> MeetingVotes => Set<MeetingVote>();
    public DbSet<MeetingVoteResponse> MeetingVoteResponses => Set<MeetingVoteResponse>();
    public DbSet<VoteMotion> VoteMotions => Set<VoteMotion>();
    public DbSet<VoteOption> VoteOptions => Set<VoteOption>();
    public DbSet<MemberVote> MemberVotes => Set<MemberVote>();
    public DbSet<QuestionnaireSection> QuestionnaireSections => Set<QuestionnaireSection>();
    public DbSet<QuestionnaireQuestion> QuestionnaireQuestions => Set<QuestionnaireQuestion>();
    public DbSet<QuestionnaireOption> QuestionnaireOptions => Set<QuestionnaireOption>();
    public DbSet<StokvelQuestionnaireAnswer> StokvelQuestionnaireAnswers => Set<StokvelQuestionnaireAnswer>();
    public DbSet<ConstitutionDocument> ConstitutionDocuments => Set<ConstitutionDocument>();
    public DbSet<ConstitutionWizardAnswer> ConstitutionWizardAnswers => Set<ConstitutionWizardAnswer>();
    public DbSet<StokvelOperatingRules> StokvelOperatingRules => Set<StokvelOperatingRules>();
    public DbSet<RotationalStokvelSetting> RotationalStokvelSettings => Set<RotationalStokvelSetting>();
    public DbSet<RotationOrder> RotationOrders => Set<RotationOrder>();
    public DbSet<RotationCycle> RotationCycles => Set<RotationCycle>();
    public DbSet<CycleContribution> CycleContributions => Set<CycleContribution>();
    public DbSet<CyclePayout> CyclePayouts => Set<CyclePayout>();
    public DbSet<RotationalStokvelConfiguration> RotationalStokvelConfigurations => Set<RotationalStokvelConfiguration>();
    public DbSet<RotationalPayoutOrder> RotationalPayoutOrders => Set<RotationalPayoutOrder>();
    public DbSet<RotationalContributionCycle> RotationalContributionCycles => Set<RotationalContributionCycle>();
    public DbSet<RotationalContributionPayment> RotationalContributionPayments => Set<RotationalContributionPayment>();
    public DbSet<StokvelBankingDetails> StokvelBankingDetails => Set<StokvelBankingDetails>();
    public DbSet<RotationalPayout> RotationalPayouts => Set<RotationalPayout>();
    public DbSet<StokvelLoanConfiguration> StokvelLoanConfigurations => Set<StokvelLoanConfiguration>();
    public DbSet<MemberLoan> MemberLoans => Set<MemberLoan>();
    public DbSet<MemberLoanRepayment> MemberLoanRepayments => Set<MemberLoanRepayment>();
    public DbSet<MemberSurplusWallet> MemberSurplusWallets => Set<MemberSurplusWallet>();
    public DbSet<MemberSurplusWalletTransaction> MemberSurplusWalletTransactions => Set<MemberSurplusWalletTransaction>();
    public DbSet<MemberSurplusWithdrawalRequest> MemberSurplusWithdrawalRequests => Set<MemberSurplusWithdrawalRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        var identityV3CredentialType = typeof(IdentityUser).Assembly
            .GetTypes()
            .FirstOrDefault(type => type.Name == "IdentityUser" + "Pass" + "key`1")
            ?.MakeGenericType(typeof(string));

        if (identityV3CredentialType is not null)
        {
            builder.Ignore(identityV3CredentialType);
        }

        builder.Entity<Tenant>()
            .HasIndex(t => t.Slug)
            .IsUnique();

        builder.Entity<Tenant>()
            .Property(t => t.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<Tenant>()
            .Property(t => t.Slug)
            .HasMaxLength(150)
            .IsRequired();

        builder.Entity<Stokvel>()
            .HasOne(s => s.Tenant)
            .WithMany(t => t.Stokvels)
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Stokvel>()
            .Property(s => s.Code)
            .HasMaxLength(20);

        builder.Entity<Stokvel>()
            .HasIndex(s => s.Code);

        builder.Entity<FuneralClaim>()
            .Property(c => c.ClaimReference)
            .HasMaxLength(50);

        builder.Entity<FuneralClaim>()
            .HasIndex(c => c.ClaimReference);

        builder.Entity<TenantSubscription>()
            .HasOne(ts => ts.Tenant)
            .WithMany(t => t.TenantSubscriptions)
            .HasForeignKey(ts => ts.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TenantSubscription>()
            .HasOne(ts => ts.SubscriptionPlan)
            .WithMany(sp => sp.TenantSubscriptions)
            .HasForeignKey(ts => ts.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Member>()
            .HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Member>()
            .HasIndex(m => m.IdNumber);

        builder.Entity<Member>()
            .HasIndex(m => m.ApplicationUserId);

        builder.Entity<MemberWarning>()
            .HasOne(w => w.Member)
            .WithMany()
            .HasForeignKey(w => w.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberWarning>()
            .HasOne(w => w.Stokvel)
            .WithMany()
            .HasForeignKey(w => w.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberWarning>()
            .HasOne(w => w.Meeting)
            .WithMany()
            .HasForeignKey(w => w.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberWarning>()
            .HasIndex(w => new { w.MemberId, w.MeetingId, w.WarningType })
            .IsUnique();

        builder.Entity<NextOfKin>()
            .HasOne(n => n.Member)
            .WithMany(m => m.NextOfKinRecords)
            .HasForeignKey(n => n.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Beneficiary>()
            .HasOne(b => b.Member)
            .WithMany(m => m.Beneficiaries)
            .HasForeignKey(b => b.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MemberDependent>()
            .HasOne(d => d.Member)
            .WithMany()
            .HasForeignKey(d => d.MemberId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FuneralClaim>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FuneralClaim>()
            .HasOne(c => c.Member)
            .WithMany()
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FuneralClaim>()
            .HasOne(c => c.Dependent)
            .WithMany()
            .HasForeignKey(c => c.DependentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<FuneralClaimDocument>()
            .HasOne(d => d.FuneralClaim)
            .WithMany(c => c.Documents)
            .HasForeignKey(d => d.FuneralClaimId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<FineType>()
            .HasOne(f => f.Tenant)
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberFine>()
            .HasOne(f => f.Tenant)
            .WithMany()
            .HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberFine>()
            .HasOne(f => f.Member)
            .WithMany()
            .HasForeignKey(f => f.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberFine>()
            .HasOne(f => f.FineType)
            .WithMany()
            .HasForeignKey(f => f.FineTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionRule>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionCycle>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberContribution>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberContribution>()
            .HasOne(c => c.ContributionCycle)
            .WithMany()
            .HasForeignKey(c => c.ContributionCycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberContribution>()
            .HasOne(c => c.Member)
            .WithMany()
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.Tenant)
            .WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.MemberContribution)
            .WithMany()
            .HasForeignKey(p => p.MemberContributionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Payment>()
            .HasOne(p => p.Member)
            .WithMany()
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionPaymentAudit>()
            .HasOne(a => a.ContributionPayment)
            .WithMany()
            .HasForeignKey(a => a.ContributionPaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionPaymentAudit>()
            .HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionPaymentAudit>()
            .HasOne(a => a.Stokvel)
            .WithMany()
            .HasForeignKey(a => a.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ContributionPaymentAudit>()
            .HasOne(a => a.CapturedByMember)
            .WithMany()
            .HasForeignKey(a => a.CapturedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ClaimPayoutAudit>()
            .HasOne(a => a.FuneralClaim)
            .WithMany()
            .HasForeignKey(a => a.FuneralClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ClaimPayoutAudit>()
            .HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ClaimPayoutAudit>()
            .HasOne(a => a.Stokvel)
            .WithMany()
            .HasForeignKey(a => a.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ClaimPayoutAudit>()
            .HasOne(a => a.CapturedByMember)
            .WithMany()
            .HasForeignKey(a => a.CapturedByMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Meeting>()
            .HasOne(m => m.Tenant)
            .WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingAgendaItem>()
            .HasOne(a => a.Meeting)
            .WithMany(m => m.AgendaItems)
            .HasForeignKey(a => a.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeetingAttendance>()
            .HasOne(a => a.Meeting)
            .WithMany()
            .HasForeignKey(a => a.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeetingAttendance>()
            .HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingAttendance>()
            .HasIndex(a => new { a.MeetingId, a.MemberId })
            .IsUnique();

        builder.Entity<MeetingApology>()
            .HasOne(a => a.Meeting)
            .WithMany()
            .HasForeignKey(a => a.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeetingApology>()
            .HasOne(a => a.Member)
            .WithMany()
            .HasForeignKey(a => a.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingApology>()
            .HasIndex(a => new { a.MeetingId, a.MemberId })
            .IsUnique();

        builder.Entity<MeetingMinute>()
            .HasOne(m => m.Meeting)
            .WithMany()
            .HasForeignKey(m => m.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingMinute>()
            .HasOne(m => m.Stokvel)
            .WithMany()
            .HasForeignKey(m => m.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingMinute>()
            .HasIndex(m => m.MeetingId)
            .IsUnique();

        builder.Entity<MeetingVote>()
            .HasOne(v => v.Meeting)
            .WithMany()
            .HasForeignKey(v => v.MeetingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeetingVoteResponse>()
            .HasOne(r => r.MeetingVote)
            .WithMany(v => v.Responses)
            .HasForeignKey(r => r.MeetingVoteId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MeetingVoteResponse>()
            .HasOne(r => r.Member)
            .WithMany()
            .HasForeignKey(r => r.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MeetingVoteResponse>()
            .HasIndex(r => new { r.MeetingVoteId, r.MemberId })
            .IsUnique();

        builder.Entity<VoteMotion>()
            .HasOne(v => v.Stokvel)
            .WithMany()
            .HasForeignKey(v => v.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteMotion>()
            .HasOne(v => v.Meeting)
            .WithMany()
            .HasForeignKey(v => v.MeetingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteMotion>()
            .HasOne(v => v.AgendaItem)
            .WithMany()
            .HasForeignKey(v => v.AgendaItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<VoteOption>()
            .HasOne(o => o.VoteMotion)
            .WithMany(v => v.Options)
            .HasForeignKey(o => o.VoteMotionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<MemberVote>()
            .HasOne(v => v.VoteMotion)
            .WithMany(m => m.MemberVotes)
            .HasForeignKey(v => v.VoteMotionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberVote>()
            .HasOne(v => v.Member)
            .WithMany()
            .HasForeignKey(v => v.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberVote>()
            .HasOne(v => v.VoteOption)
            .WithMany()
            .HasForeignKey(v => v.VoteOptionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<MemberVote>()
            .HasIndex(v => new { v.VoteMotionId, v.MemberId })
            .IsUnique();

        builder.Entity<QuestionnaireQuestion>()
            .HasOne(q => q.QuestionnaireSection)
            .WithMany(s => s.Questions)
            .HasForeignKey(q => q.QuestionnaireSectionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionnaireOption>()
            .HasOne(o => o.QuestionnaireQuestion)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionnaireQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StokvelQuestionnaireAnswer>()
            .HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StokvelQuestionnaireAnswer>()
            .HasOne(a => a.QuestionnaireQuestion)
            .WithMany()
            .HasForeignKey(a => a.QuestionnaireQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StokvelQuestionnaireAnswer>()
            .HasIndex(a => new { a.TenantId, a.QuestionnaireQuestionId })
            .IsUnique();

        builder.Entity<ConstitutionDocument>()
            .HasOne(c => c.Tenant)
            .WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConstitutionWizardAnswer>()
            .HasOne(a => a.Tenant)
            .WithMany()
            .HasForeignKey(a => a.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ConstitutionWizardAnswer>()
            .HasIndex(a => new { a.TenantId, a.QuestionKey })
            .IsUnique();

        builder.Entity<StokvelOperatingRules>()
            .HasOne(r => r.Stokvel)
            .WithOne()
            .HasForeignKey<StokvelOperatingRules>(r => r.StokvelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StokvelOperatingRules>()
            .HasIndex(r => r.StokvelId)
            .IsUnique();

        // ── Performance indexes ───────────────────────────────────────────────
        // Stokvel lookup by tenant (dashboards, admin lists)
        builder.Entity<Stokvel>()
            .HasIndex(s => s.TenantId);

        // Member lookup by tenant (member lists, contribution queries)
        builder.Entity<Member>()
            .HasIndex(m => m.TenantId);

        // Contribution queries by member and by tenant+member composite
        builder.Entity<MemberContribution>()
            .HasIndex(mc => mc.MemberId);

        builder.Entity<MemberContribution>()
            .HasIndex(mc => new { mc.TenantId, mc.MemberId });

        // Claims filtered by tenant + status (dashboard outstanding claims)
        builder.Entity<FuneralClaim>()
            .HasIndex(c => new { c.TenantId, c.Status });

        // Meetings filtered by tenant + date (upcoming meetings widget)
        builder.Entity<Meeting>()
            .HasIndex(m => new { m.TenantId, m.MeetingDate });

        // ── Money precision: HasPrecision(18,2) for all decimal currency fields ─
        builder.Entity<SubscriptionPlan>()
            .Property(p => p.MonthlyPrice).HasPrecision(18, 2);
        builder.Entity<SubscriptionPlan>()
            .Property(p => p.AnnualPrice).HasPrecision(18, 2);

        builder.Entity<FineType>()
            .Property(f => f.DefaultAmount).HasPrecision(18, 2);

        builder.Entity<ContributionRule>()
            .Property(r => r.Amount).HasPrecision(18, 2);
        builder.Entity<ContributionRule>()
            .Property(r => r.LatePaymentFineAmount).HasPrecision(18, 2);

        builder.Entity<MemberFine>()
            .Property(f => f.Amount).HasPrecision(18, 2);

        builder.Entity<MemberContribution>()
            .Property(mc => mc.ExpectedAmount).HasPrecision(18, 2);
        builder.Entity<MemberContribution>()
            .Property(mc => mc.PaidAmount).HasPrecision(18, 2);
        builder.Entity<MemberContribution>()
            .Property(mc => mc.OutstandingAmount).HasPrecision(18, 2);

        builder.Entity<Payment>()
            .Property(p => p.Amount).HasPrecision(18, 2);

        builder.Entity<FuneralClaim>()
            .Property(c => c.PayoutAmount).HasPrecision(18, 2);

        builder.Entity<ContributionPaymentAudit>()
            .Property(a => a.PreviousAmountPaid).HasPrecision(18, 2);
        builder.Entity<ContributionPaymentAudit>()
            .Property(a => a.NewAmountPaid).HasPrecision(18, 2);

        builder.Entity<ClaimPayoutAudit>()
            .Property(a => a.PreviousPayoutAmount).HasPrecision(18, 2);
        builder.Entity<ClaimPayoutAudit>()
            .Property(a => a.NewPayoutAmount).HasPrecision(18, 2);

        // ── Rotational Stokvel MVP ────────────────────────────────────────────

        // RotationalStokvelSetting: one per stokvel, cascade when stokvel deleted
        builder.Entity<RotationalStokvelSetting>()
            .HasOne(s => s.Stokvel)
            .WithMany()
            .HasForeignKey(s => s.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationalStokvelSetting>()
            .HasIndex(s => s.StokvelId)
            .IsUnique();

        builder.Entity<RotationalStokvelSetting>()
            .Property(s => s.ContributionAmount).HasPrecision(18, 2);

        // RotationOrder: cascade from stokvel, restrict from member
        builder.Entity<RotationOrder>()
            .HasOne(ro => ro.Stokvel)
            .WithMany()
            .HasForeignKey(ro => ro.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationOrder>()
            .HasOne(ro => ro.Member)
            .WithMany()
            .HasForeignKey(ro => ro.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationOrder>()
            .HasIndex(ro => new { ro.StokvelId, ro.Position })
            .IsUnique();

        builder.Entity<RotationOrder>()
            .HasIndex(ro => new { ro.StokvelId, ro.MemberId })
            .IsUnique();

        // RotationCycle: cascade from stokvel, restrict from payout member
        builder.Entity<RotationCycle>()
            .HasOne(rc => rc.Stokvel)
            .WithMany()
            .HasForeignKey(rc => rc.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationCycle>()
            .HasOne(rc => rc.PayoutMember)
            .WithMany()
            .HasForeignKey(rc => rc.PayoutMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationCycle>()
            .HasIndex(rc => new { rc.StokvelId, rc.CycleNumber })
            .IsUnique();

        builder.Entity<RotationCycle>()
            .Property(rc => rc.ExpectedAmount).HasPrecision(18, 2);

        builder.Entity<RotationCycle>()
            .Property(rc => rc.ActualCollectedAmount).HasPrecision(18, 2);

        // CycleContribution: cascade from rotation cycle, restrict from member
        builder.Entity<CycleContribution>()
            .HasOne(cc => cc.RotationCycle)
            .WithMany(rc => rc.CycleContributions)
            .HasForeignKey(cc => cc.RotationCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CycleContribution>()
            .HasOne(cc => cc.Member)
            .WithMany()
            .HasForeignKey(cc => cc.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CycleContribution>()
            .HasIndex(cc => new { cc.RotationCycleId, cc.MemberId })
            .IsUnique();

        builder.Entity<CycleContribution>()
            .Property(cc => cc.AmountDue).HasPrecision(18, 2);

        builder.Entity<CycleContribution>()
            .Property(cc => cc.AmountPaid).HasPrecision(18, 2);

        // CyclePayout: cascade from rotation cycle, restrict from member
        builder.Entity<CyclePayout>()
            .HasOne(cp => cp.RotationCycle)
            .WithMany(rc => rc.CyclePayouts)
            .HasForeignKey(cp => cp.RotationCycleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<CyclePayout>()
            .HasOne(cp => cp.Member)
            .WithMany()
            .HasForeignKey(cp => cp.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<CyclePayout>()
            .HasIndex(cp => new { cp.RotationCycleId, cp.MemberId })
            .IsUnique();

        builder.Entity<CyclePayout>()
            .Property(cp => cp.Amount).HasPrecision(18, 2);

        // ── Rotational Stokvel Configuration ─────────────────────────────────
        builder.Entity<RotationalStokvelConfiguration>()
            .HasOne(c => c.Stokvel)
            .WithMany()
            .HasForeignKey(c => c.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationalStokvelConfiguration>()
            .HasIndex(c => c.StokvelId);

        builder.Entity<RotationalStokvelConfiguration>()
            .HasIndex(c => new { c.StokvelId, c.IsActive });

        builder.Entity<RotationalStokvelConfiguration>()
            .Property(c => c.ContributionAmount).HasPrecision(18, 2);

        builder.Entity<RotationalStokvelConfiguration>()
            .Property(c => c.PayoutAmount).HasPrecision(18, 2);

        builder.Entity<RotationalStokvelConfiguration>()
            .Property(c => c.LatePenaltyAmount).HasPrecision(18, 2);

        builder.Entity<RotationalStokvelConfiguration>()
            .Property(c => c.MinimumBalanceBeforePayout).HasPrecision(18, 2);

        // ── Rotational Payout Order ──────────────────────────────────────────
        builder.Entity<RotationalPayoutOrder>()
            .HasOne(order => order.Stokvel)
            .WithMany()
            .HasForeignKey(order => order.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationalPayoutOrder>()
            .HasOne(order => order.Member)
            .WithMany()
            .HasForeignKey(order => order.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuestionnaireQuestion>()
            .HasIndex(q => q.StokvelType);

        builder.Entity<QuestionnaireQuestion>()
            .HasIndex(q => q.IsActive);

        builder.Entity<QuestionnaireQuestion>()
            .HasIndex(q => new { q.QuestionnaireSectionId, q.StokvelType, q.IsActive });

        builder.Entity<RotationalPayoutOrder>()
            .HasIndex(order => order.StokvelId);

        builder.Entity<RotationalPayoutOrder>()
            .HasIndex(order => order.MemberId);

        builder.Entity<RotationalPayoutOrder>()
            .HasIndex(order => new { order.StokvelId, order.Position })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Entity<RotationalPayoutOrder>()
            .HasIndex(order => new { order.StokvelId, order.MemberId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Entity<RotationalPayoutOrder>()
            .HasIndex(order => new { order.StokvelId, order.IsActive });

        // ── Rotational Contribution Cycles ────────────────────────────────────
        builder.Entity<RotationalContributionCycle>()
            .HasOne(c => c.Stokvel)
            .WithMany()
            .HasForeignKey(c => c.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationalContributionCycle>()
            .HasOne(c => c.Configuration)
            .WithMany()
            .HasForeignKey(c => c.ConfigurationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationalContributionCycle>()
            .HasOne(c => c.PayoutOrder)
            .WithMany()
            .HasForeignKey(c => c.PayoutOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationalContributionCycle>()
            .HasOne(c => c.PayoutMember)
            .WithMany()
            .HasForeignKey(c => c.PayoutMemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => c.StokvelId);

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => new { c.StokvelId, c.IsActive });

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => new { c.StokvelId, c.CycleNumber })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => c.PayoutMemberId);

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => c.ConfigurationId);

        builder.Entity<RotationalContributionCycle>()
            .HasIndex(c => new { c.StokvelId, c.Status });

        builder.Entity<RotationalContributionCycle>()
            .Property(c => c.ContributionAmountPerMember).HasPrecision(18, 2);

        builder.Entity<RotationalContributionCycle>()
            .Property(c => c.ExpectedTotalContributionAmount).HasPrecision(18, 2);

        builder.Entity<RotationalContributionCycle>()
            .Property(c => c.ExpectedPayoutAmount).HasPrecision(18, 2);

        builder.Entity<StokvelBankingDetails>()
            .HasOne(details => details.Stokvel).WithMany()
            .HasForeignKey(details => details.StokvelId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StokvelBankingDetails>().HasIndex(details => details.StokvelId);
        builder.Entity<StokvelBankingDetails>()
            .HasIndex(details => new { details.StokvelId, details.IsActive, details.IsPrimary })
            .IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1");

        builder.Entity<RotationalPayout>()
            .HasOne(payout => payout.Stokvel).WithMany()
            .HasForeignKey(payout => payout.StokvelId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RotationalPayout>()
            .HasOne(payout => payout.Cycle).WithMany()
            .HasForeignKey(payout => payout.CycleId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RotationalPayout>()
            .HasOne(payout => payout.PayoutMember).WithMany()
            .HasForeignKey(payout => payout.PayoutMemberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<RotationalPayout>().Property(payout => payout.PayoutAmount).HasPrecision(18, 2);
        builder.Entity<RotationalPayout>().HasIndex(payout => payout.StokvelId);
        builder.Entity<RotationalPayout>().HasIndex(payout => payout.CycleId);
        builder.Entity<RotationalPayout>().HasIndex(payout => payout.PayoutMemberId);
        builder.Entity<RotationalPayout>().HasIndex(payout => new { payout.StokvelId, payout.PayoutStatus });
        builder.Entity<RotationalPayout>()
            .HasIndex(payout => new { payout.CycleId, payout.IsActive })
            .IsUnique().HasFilter("[IsActive] = 1");

        // ── Rotational Contribution Payments ─────────────────────────────────
        builder.Entity<RotationalContributionPayment>()
            .HasOne(p => p.Stokvel)
            .WithMany()
            .HasForeignKey(p => p.StokvelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RotationalContributionPayment>()
            .HasOne(p => p.Cycle)
            .WithMany()
            .HasForeignKey(p => p.CycleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationalContributionPayment>()
            .HasOne(p => p.Member)
            .WithMany()
            .HasForeignKey(p => p.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<RotationalContributionPayment>()
            .Property(p => p.ExpectedAmount).HasPrecision(18, 2);

        builder.Entity<RotationalContributionPayment>()
            .Property(p => p.PaidAmount).HasPrecision(18, 2);

        builder.Entity<RotationalContributionPayment>()
            .Property(p => p.PenaltyAmount).HasPrecision(18, 2);

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => p.StokvelId);

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => p.CycleId);

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => p.MemberId);

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => new { p.CycleId, p.MemberId })
            .IsUnique()
            .HasFilter("[IsActive] = 1");

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => new { p.StokvelId, p.CycleId });

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => new { p.StokvelId, p.PaymentStatus });

        builder.Entity<RotationalContributionPayment>()
            .HasIndex(p => p.ConfirmedByTreasurerId);

        builder.Entity<StokvelLoanConfiguration>().HasOne(x => x.Stokvel).WithMany()
            .HasForeignKey(x => x.StokvelId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<StokvelLoanConfiguration>().HasIndex(x => x.StokvelId);
        builder.Entity<StokvelLoanConfiguration>().HasIndex(x => new { x.StokvelId, x.IsActive })
            .IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<StokvelLoanConfiguration>().Property(x => x.MinLoanAmount).HasPrecision(18, 2);
        builder.Entity<StokvelLoanConfiguration>().Property(x => x.MaxLoanAmount).HasPrecision(18, 2);
        builder.Entity<StokvelLoanConfiguration>().Property(x => x.LoanInterestRate).HasPrecision(18, 2);
        builder.Entity<StokvelLoanConfiguration>().Property(x => x.LateRepaymentFineAmount).HasPrecision(18, 2);

        builder.Entity<MemberLoan>().HasOne(x => x.Stokvel).WithMany()
            .HasForeignKey(x => x.StokvelId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberLoan>().HasOne(x => x.Member).WithMany()
            .HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberLoan>().HasIndex(x => x.StokvelId);
        builder.Entity<MemberLoan>().HasIndex(x => x.MemberId);
        builder.Entity<MemberLoan>().HasIndex(x => new { x.StokvelId, x.LoanStatus });
        builder.Entity<MemberLoan>().HasIndex(x => new { x.StokvelId, x.MemberId, x.IsActive });
        builder.Entity<MemberLoan>().HasIndex(x => new { x.StokvelId, x.MemberId })
            .IsUnique().HasFilter("[IsActive] = 1 AND [LoanStatus] IN (2, 3, 4, 6, 7, 9)");
        builder.Entity<MemberLoan>().Property(x => x.RequestedAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoan>().Property(x => x.ApprovedAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoan>().Property(x => x.MonthlyRepaymentAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoan>().Property(x => x.TotalRepayableAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoan>().Property(x => x.OutstandingBalance).HasPrecision(18, 2);

        builder.Entity<MemberLoanRepayment>().HasOne(x => x.Loan).WithMany(x => x.Repayments)
            .HasForeignKey(x => x.LoanId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberLoanRepayment>().HasOne(x => x.Member).WithMany()
            .HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberLoanRepayment>().HasIndex(x => x.StokvelId);
        builder.Entity<MemberLoanRepayment>().HasIndex(x => x.LoanId);
        builder.Entity<MemberLoanRepayment>().HasIndex(x => new { x.LoanId, x.DueDate }).IsUnique();
        builder.Entity<MemberLoanRepayment>().HasIndex(x => new { x.StokvelId, x.PaymentStatus });
        builder.Entity<MemberLoanRepayment>().Property(x => x.ExpectedAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoanRepayment>().Property(x => x.PaidAmount).HasPrecision(18, 2);
        builder.Entity<MemberLoanRepayment>().Property(x => x.FineAmount).HasPrecision(18, 2);

        builder.Entity<MemberSurplusWallet>().HasOne(x => x.Member).WithMany()
            .HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberSurplusWallet>().HasIndex(x => new { x.StokvelId, x.MemberId, x.IsActive })
            .IsUnique().HasFilter("[IsActive] = 1");
        builder.Entity<MemberSurplusWallet>().Property(x => x.AvailableBalance).HasPrecision(18, 2);
        builder.Entity<MemberSurplusWallet>().Property(x => x.TotalCredits).HasPrecision(18, 2);
        builder.Entity<MemberSurplusWallet>().Property(x => x.TotalWithdrawals).HasPrecision(18, 2);

        builder.Entity<MemberSurplusWalletTransaction>().HasOne(x => x.Wallet).WithMany()
            .HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberSurplusWalletTransaction>().HasIndex(x => x.WalletId);
        builder.Entity<MemberSurplusWalletTransaction>().HasIndex(x => new { x.StokvelId, x.MemberId, x.CreatedAt });
        builder.Entity<MemberSurplusWalletTransaction>()
            .HasIndex(x => new { x.WalletId, x.SourceType, x.SourceReferenceId, x.TransactionType })
            .IsUnique().HasFilter("[SourceReferenceId] IS NOT NULL");
        builder.Entity<MemberSurplusWalletTransaction>().Property(x => x.Amount).HasPrecision(18, 2);
        builder.Entity<MemberSurplusWalletTransaction>().Property(x => x.BalanceAfterTransaction).HasPrecision(18, 2);

        builder.Entity<MemberSurplusWithdrawalRequest>().HasOne(x => x.Member).WithMany()
            .HasForeignKey(x => x.MemberId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberSurplusWithdrawalRequest>().HasOne(x => x.Wallet).WithMany()
            .HasForeignKey(x => x.WalletId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<MemberSurplusWithdrawalRequest>().HasIndex(x => x.StokvelId);
        builder.Entity<MemberSurplusWithdrawalRequest>().HasIndex(x => x.MemberId);
        builder.Entity<MemberSurplusWithdrawalRequest>().HasIndex(x => new { x.StokvelId, x.WithdrawalStatus });
        builder.Entity<MemberSurplusWithdrawalRequest>().HasIndex(x => new { x.StokvelId, x.MemberId, x.IsActive });
        builder.Entity<MemberSurplusWithdrawalRequest>().HasIndex(x => new { x.StokvelId, x.MemberId })
            .IsUnique().HasFilter("[IsActive] = 1 AND [WithdrawalStatus] IN (1, 2, 3, 5)");
        builder.Entity<MemberSurplusWithdrawalRequest>().Property(x => x.RequestedAmount).HasPrecision(18, 2);
    }
}
