using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Sisonke.Web.Data;
using Sisonke.Web.Data.Enums;
using Sisonke.Web.Services.Entitlements;
using Sisonke.Web.Tests.TestSupport;

namespace Sisonke.Web.Tests.Billing;

/// <summary>
/// Exercises RequireFeatureEndpointFilter directly (rather than through a full
/// WebApplicationFactory) — proves that calling the API layer for a feature the UI would hide
/// returns HTTP 402, using the exact same IEntitlementService.AuthorizeAsync decision path as
/// the application service and UI enforcement surfaces.
/// </summary>
public class RequireFeatureEndpointFilterTests
{
    [Fact]
    public async Task DeniedFeature_Returns402AndDoesNotCallNext()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var starterPlan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Starter);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, starterPlan.Id, SubscriptionStatus.Active);
        await context.SaveChangesAsync();

        // ROTATIONAL_STOKVEL is Starter=false — a feature the UI would hide on this plan.
        var filter = new RequireFeatureEndpointFilter(FeatureCodes.RotationalStokvel, requestedUsage: 1);
        var nextCalled = false;
        var member = TestData.CreateStokvelMember(context, stokvel);
        member.ApplicationUserId = "user-1";
        await context.SaveChangesAsync();
        var httpContext = BuildHttpContext(harness, context, stokvel.Id, "user-1");
        var invocationContext = EndpointFilterInvocationContext.Create(httpContext);

        var result = await filter.InvokeAsync(invocationContext, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.False(nextCalled);
        var statusCodeResult = Assert.IsAssignableFrom<IStatusCodeHttpResult>(result);
        Assert.Equal(StatusCodes.Status402PaymentRequired, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task AllowedFeature_CallsNext()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var growingPlan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Growing);
        var stokvel = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, stokvel, growingPlan.Id, SubscriptionStatus.Active);
        await context.SaveChangesAsync();

        var filter = new RequireFeatureEndpointFilter(FeatureCodes.RotationalStokvel, requestedUsage: 1);
        var nextCalled = false;
        var member = TestData.CreateStokvelMember(context, stokvel);
        member.ApplicationUserId = "user-1";
        await context.SaveChangesAsync();
        var httpContext = BuildHttpContext(harness, context, stokvel.Id, "user-1");
        var invocationContext = EndpointFilterInvocationContext.Create(httpContext);

        await filter.InvokeAsync(invocationContext, _ =>
        {
            nextCalled = true;
            return ValueTask.FromResult<object?>(Results.Ok());
        });

        Assert.True(nextCalled);
    }

    [Fact]
    public async Task UserFromAnotherStokvel_Returns403BeforeEntitlementEvaluation()
    {
        using var harness = new EntitlementTestHarness();
        await harness.SeedCatalogueAsync();

        await using var context = harness.CreateContext();
        var plan = context.SubscriptionPlans.Single(p => p.Code == PlanCodes.Growing);
        var target = TestData.CreateStokvel(context);
        TestData.CreateOrganisationSubscription(context, target, plan.Id, SubscriptionStatus.Active);
        var other = TestData.CreateStokvel(context);
        var member = TestData.CreateStokvelMember(context, other);
        member.ApplicationUserId = "other-user";
        await context.SaveChangesAsync();

        var filter = new RequireFeatureEndpointFilter(FeatureCodes.RotationalStokvel, 1);
        var httpContext = BuildHttpContext(harness, context, target.Id, "other-user");
        var result = await filter.InvokeAsync(EndpointFilterInvocationContext.Create(httpContext),
            _ => ValueTask.FromResult<object?>(Results.Ok()));

        Assert.IsAssignableFrom<ForbidHttpResult>(result);
    }

    private static DefaultHttpContext BuildHttpContext(
        EntitlementTestHarness harness,
        ApplicationDbContext db,
        Guid stokvelId,
        string userId)
    {
        var services = new ServiceCollection()
            .AddSingleton<IEntitlementService>(harness.Sut)
            .AddSingleton(new Sisonke.Web.Services.MemberAccessService(db))
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = services };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId)], "Test"));
        httpContext.Request.RouteValues["stokvelId"] = stokvelId.ToString();
        return httpContext;
    }
}
