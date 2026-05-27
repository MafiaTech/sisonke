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
    public DbSet<NextOfKin> NextOfKinRecords => Set<NextOfKin>();
    public DbSet<Beneficiary> Beneficiaries => Set<Beneficiary>();
    public DbSet<FineType> FineTypes => Set<FineType>();
    public DbSet<MemberFine> MemberFines => Set<MemberFine>();
    public DbSet<ContributionRule> ContributionRules => Set<ContributionRule>();
    public DbSet<ContributionCycle> ContributionCycles => Set<ContributionCycle>();
    public DbSet<MemberContribution> MemberContributions => Set<MemberContribution>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Meeting> Meetings => Set<Meeting>();
    public DbSet<MeetingAgendaItem> MeetingAgendaItems => Set<MeetingAgendaItem>();
    public DbSet<MeetingAttendance> MeetingAttendances => Set<MeetingAttendance>();
    public DbSet<QuestionnaireSection> QuestionnaireSections => Set<QuestionnaireSection>();
    public DbSet<QuestionnaireQuestion> QuestionnaireQuestions => Set<QuestionnaireQuestion>();
    public DbSet<QuestionnaireOption> QuestionnaireOptions => Set<QuestionnaireOption>();
    public DbSet<StokvelQuestionnaireAnswer> StokvelQuestionnaireAnswers => Set<StokvelQuestionnaireAnswer>();
    public DbSet<ConstitutionDocument> ConstitutionDocuments => Set<ConstitutionDocument>();
    public DbSet<ConstitutionWizardAnswer> ConstitutionWizardAnswers => Set<ConstitutionWizardAnswer>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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
    }
}
