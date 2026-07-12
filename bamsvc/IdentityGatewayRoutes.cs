using Bam.Identity;
using Bam.Protocol.Data.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Bam.Svc;

/// <summary>
/// The result of a gateway route computation: a response body and the HTTP status code to send it with.
/// <see cref="Body"/> is <c>object</c> deliberately — it holds either an <see cref="ErrorResponse"/> or the
/// route's own success-shape response (<see cref="RegisterPersonResponse"/>/<see cref="ProfileData"/>), matching
/// the polymorphic body type <c>Results.Json(object)</c> itself accepts.
/// </summary>
public class GatewayOutcome
{
    public object Body { get; set; } = null!;
    public int StatusCode { get; set; }
}

/// <summary>
/// Maps bamsvc's existing public REST convenience routes (<c>/api/register</c>, <c>/api/profile/{handle}</c>)
/// onto <see cref="IRegistrationService"/> calls against <c>bamid</c> instead of in-process identity logic.
/// </summary>
public class IdentityGatewayRoutes
{
    private readonly IRegistrationService _registrationService;

    public IdentityGatewayRoutes(IRegistrationService registrationService)
    {
        _registrationService = registrationService;
    }

    public void MapRoutes(WebApplication app)
    {
        app.MapPost("/api/register", async (HttpContext ctx) =>
        {
            PersonRegistrationRequest? request = await ctx.Request.ReadFromJsonAsync<PersonRegistrationRequest>();
            GatewayOutcome outcome = BuildRegisterOutcome(request);
            return Results.Json(outcome.Body, statusCode: outcome.StatusCode);
        });

        app.MapGet("/api/profile/{handle}", (string handle) =>
        {
            GatewayOutcome outcome = BuildProfileOutcome(handle);
            return Results.Json(outcome.Body, statusCode: outcome.StatusCode);
        });
    }

    public GatewayOutcome BuildRegisterOutcome(PersonRegistrationRequest? request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return new GatewayOutcome
            {
                Body = new ErrorResponse { Error = "FirstName and LastName are required" },
                StatusCode = 400,
            };
        }

        try
        {
            AccountData accountData = _registrationService.RegisterPerson(
                request.FirstName, request.LastName, request.Email, request.Phone, request.Handle);
            return new GatewayOutcome
            {
                Body = new RegisterPersonResponse { PersonHandle = accountData.PersonHandle },
                StatusCode = 200,
            };
        }
        catch (Exception ex)
        {
            return new GatewayOutcome
            {
                Body = new ErrorResponse { Error = ex.Message },
                StatusCode = 500,
            };
        }
    }

    public GatewayOutcome BuildProfileOutcome(string handle)
    {
        ProfileData? result = _registrationService.GetProfile(handle);
        if (result == null)
        {
            return new GatewayOutcome
            {
                Body = new ErrorResponse { Error = "Profile not found" },
                StatusCode = 404,
            };
        }

        return new GatewayOutcome
        {
            Body = result,
            StatusCode = 200,
        };
    }
}
