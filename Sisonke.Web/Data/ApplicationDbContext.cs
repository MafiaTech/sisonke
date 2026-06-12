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
    }
}
