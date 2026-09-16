using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Components;

public sealed class DbContextLifetimeTests
{
    [Fact]
    public void ScopedFactoryReproducesDisposedServiceProviderAfterScopeEnds()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddDbContext<ApplicationDbContext>(o => o.UseSqlite("Data Source=:memory:"));
        builder.Services.AddDbContextFactory<ApplicationDbContext>(o => o.UseSqlite("Data Source=:memory:"), ServiceLifetime.Scoped);
        using var host = builder.Build();
        var scope = host.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var context = factory.CreateDbContext();
        scope.Dispose();
        var error = Assert.Throws<ObjectDisposedException>(() => context.Stokvels.ToList());
        Assert.Equal("IServiceProvider", error.ObjectName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingletonFactoryAndOptionsAreIndependentOfCircuitScope(bool sqlServer)
    {
        var builder = Host.CreateApplicationBuilder();
        void Configure(DbContextOptionsBuilder options)
        {
            if (sqlServer) options.UseSqlServer("Server=localhost;Database=DesignOnly;Trusted_Connection=True;TrustServerCertificate=True");
            else options.UseSqlite("Data Source=:memory:");
        }
        builder.Services.AddDbContext<ApplicationDbContext>(Configure, optionsLifetime: ServiceLifetime.Singleton);
        builder.Services.AddDbContextFactory<ApplicationDbContext>(Configure, ServiceLifetime.Singleton);
        using var host = builder.Build();
        IDbContextFactory<ApplicationDbContext> factory;
        using (var scope = host.Services.CreateScope()) factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
        using var context = factory.CreateDbContext();
        Assert.NotNull(context.Model.FindEntityType(typeof(Sisonke.Web.Data.Entities.Stokvel)));
        using var nextScope = host.Services.CreateScope();
        Assert.Same(factory, nextScope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>());
        Assert.NotSame(context, nextScope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    [Fact]
    public async Task StokvelAndAccessServicesUseAndDisposeFreshContexts()
    {
        using var db = new SharedCacheSqliteTestDatabase();
        Guid stokvelId, tenantId;
        using (var seed = db.CreateContext())
        {
            var stokvel = TestData.CreateStokvel(seed); stokvelId = stokvel.Id; tenantId = stokvel.TenantId;
            var member = TestData.CreateStokvelMember(seed, stokvel, role: SisonkeRole.Treasurer); member.ApplicationUserId = "actor";
            seed.SaveChanges();
        }
        var factory = new TrackingFactory(db);
        var stokvels = new StokvelService(factory, new StokvelArchetypeConfigurationService(), null!);
        var access = new MemberAccessService(factory);
        var reads = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            Assert.Equal(stokvelId, (await stokvels.GetStokvelByTenantIdAsync(tenantId))!.Id);
            Assert.True(await access.CanManagePaymentsAsync("actor", stokvelId));
        }));
        await Task.WhenAll(reads);
        Assert.Equal(16, factory.Contexts.Count);
        foreach (var context in factory.Contexts) Assert.Throws<ObjectDisposedException>(() => context.Stokvels);
    }

    [Fact]
    public async Task FineOperationsUseFreshContextsAndPreserveAudits()
    {
        using var db = new SharedCacheSqliteTestDatabase();
        Guid stokvelId, memberId;
        using (var seed = db.CreateContext())
        {
            var stokvel = TestData.CreateStokvel(seed); stokvelId = stokvel.Id;
            memberId = TestData.CreateStokvelMember(seed, stokvel).Id; seed.SaveChanges();
        }
        var factory = new TrackingFactory(db);
        var service = new FineService(factory, new AuditLogService(factory, NullLogger<AuditLogService>.Instance));
        await service.EnsureDefaultFineTypesForStokvelAsync(stokvelId);
        var type = (await service.GetFineTypesByStokvelIdAsync(stokvelId)).First();
        var paid = await service.AddMemberFineAsync(memberId, type.Id, 50, "Late", DateTime.Today);
        await service.MarkFineAsPaidAsync(paid!.Id);
        Assert.Equal(FineStatus.Paid, (await service.GetMemberFineByIdAsync(paid.Id))!.Status);
        var cancelled = await service.AddMemberFineAsync(memberId, type.Id, 50, "Mistake", DateTime.Today);
        await service.VoidFineAsync(cancelled!.Id);
        Assert.Equal(FineStatus.Cancelled, (await service.GetMemberFineByIdAsync(cancelled.Id))!.Status);
        Assert.Equal(2, (await service.GetMemberFinesAsync(memberId)).Count);
        Assert.Equal(0, await service.GetOutstandingFinesTotalByStokvelIdAsync(stokvelId));
        foreach (var context in factory.Contexts) Assert.Throws<ObjectDisposedException>(() => context.MemberFines);
        using var check = db.CreateContext();
        Assert.Contains(check.AuditLogEntries, a => a.ActionType == "FinePaid");
        Assert.Contains(check.AuditLogEntries, a => a.ActionType == "FineWaived");
    }

    [Fact]
    public async Task FactoryContributionCaptureAndStatementUseFreshContexts()
    {
        using var db = new SharedCacheSqliteTestDatabase();
        Guid stokvelId, memberId;
        const string actor = "treasurer";
        using (var seed = db.CreateContext())
        {
            var stokvel = TestData.CreateStokvel(seed); stokvelId = stokvel.Id;
            var member = TestData.CreateStokvelMember(seed, stokvel, role: SisonkeRole.Treasurer);
            member.ApplicationUserId = actor; memberId = member.Id;
            seed.Users.Add(new ApplicationUser { Id = actor, UserName = actor }); seed.SaveChanges();
        }
        var factory = new TrackingFactory(db);
        var audit = new AuditLogService(factory, NullLogger<AuditLogService>.Instance);
        var rules = new ContributionService(factory);
        await rules.SaveContributionRuleAsync(stokvelId, new Sisonke.Web.Data.Entities.ContributionRule
            { Amount = 100, DueDayOfMonth = 28, EffectiveFrom = DateTime.Today });
        var posting = new ContributionPaymentService(factory, new MemberAccessService(factory), audit);
        var contribution = (await posting.EnsureMonthlyContributionRecordsAsync(stokvelId, DateTime.Today.Year, DateTime.Today.Month)).Single();
        await posting.CaptureContributionPaymentAsync(contribution.Id, 40, "reference", null, actor);
        var report = new FinanceReportService(factory, posting, new FineService(factory, audit), null!);
        var statement = await report.GetMemberFinancialStatementAsync(memberId, stokvelId);
        Assert.Equal(40, statement!.TotalContributionPaid);
        Assert.Equal(60, statement.TotalContributionOutstanding);
        Assert.Equal(60, (await posting.GetCurrentMonthContributionAsync(memberId, stokvelId))!.Balance);
        foreach (var context in factory.Contexts) Assert.Throws<ObjectDisposedException>(() => context.Payments);
        using var check = db.CreateContext();
        Assert.Single(check.Payments); Assert.Single(check.ContributionPaymentAudits);
    }

    private sealed class TrackingFactory(SharedCacheSqliteTestDatabase db) : IDbContextFactory<ApplicationDbContext>
    {
        public System.Collections.Concurrent.ConcurrentBag<ApplicationDbContext> Contexts { get; } = [];
        public ApplicationDbContext CreateDbContext() { var context = db.CreateContext(); Contexts.Add(context); return context; }
        public Task<ApplicationDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
