using Microsoft.AspNetCore.Mvc;

namespace Sisonke.Web.Services.Entitlements;

/// <summary>
/// Minimal-API enforcement surface. Resolves stokvelId from the route (every gated business
/// endpoint in this app is scoped by stokvel in its route, the same way the Blazor pages are —
/// e.g. /api/stokvels/{stokvelId}/members), calls the same IEntitlementService.AuthorizeAsync
/// used by application services, and returns HTTP 402 Payment Required with a problem-details
/// body on denial. 402 is reserved for entitlement denials specifically — genuine authorisation
/// failures (wrong user, wrong role) must keep using 401/403 so the two are distinguishable.
/// </summary>
public sealed class RequireFeatureEndpointFilter(string featureCode, int requestedUsage) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var stokvelId = ResolveStokvelId(context.HttpContext);

        if (stokvelId is null)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Missing stokvelId",
                Detail = "This endpoint requires a stokvelId route value to evaluate entitlements.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var entitlementService = context.HttpContext.RequestServices.GetRequiredService<IEntitlementService>();
        var decision = await entitlementService.AuthorizeAsync(stokvelId.Value, featureCode, requestedUsage, context.HttpContext.RequestAborted);

        if (!decision.Allowed)
        {
            return Results.Json(
                new
                {
                    title = "Feature not available on your plan",
                    status = StatusCodes.Status402PaymentRequired,
                    featureCode = decision.FeatureCode,
                    planCode = decision.PlanCode,
                    upgradeToPlanCode = decision.UpgradeToPlanCode,
                    message = decision.Message
                },
                statusCode: StatusCodes.Status402PaymentRequired,
                contentType: "application/problem+json");
        }

        return await next(context);
    }

    private static Guid? ResolveStokvelId(HttpContext httpContext)
    {
        if (httpContext.Request.RouteValues.TryGetValue("stokvelId", out var routeValue) &&
            Guid.TryParse(routeValue?.ToString(), out var stokvelId))
        {
            return stokvelId;
        }

        return null;
    }
}

public static class RequireFeatureEndpointFilterExtensions
{
    /// <summary>
    /// Gates an endpoint on a feature code, in addition to whatever authorisation the endpoint
    /// already requires. requestedUsage defaults to 1 (this request performs one instance of the
    /// gated operation) — correct for boolean/enum features (ignored) and for a numeric feature
    /// like MAX_MEMBERS where the endpoint creates exactly one record per call.
    /// </summary>
    public static TBuilder RequireFeature<TBuilder>(this TBuilder builder, string featureCode, int requestedUsage = 1)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.AddEndpointFilter(new RequireFeatureEndpointFilter(featureCode, requestedUsage));
        return builder;
    }
}
