using System.Security.Claims;
using JourneySharing.Application;

public static class ConnectionEndpoints
{
    public static void MapConnectionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/connections").RequireAuthorization();
        group.MapPost("/", async (ConnectionIntent input, ClaimsPrincipal user,
            IConnectionService service, CancellationToken ct) =>
        {
            var id = ActorId(user);
            return id is null ? Results.Unauthorized() : Result(await service.CreateAsync(id.Value, input, ct));
        });
        group.MapGet("/", async (ClaimsPrincipal user, IConnectionService service, CancellationToken ct) =>
        {
            var id = ActorId(user);
            return id is null ? Results.Unauthorized() : Results.Ok(await service.ListMineAsync(id.Value, ct));
        });
        group.MapPost("/{connectionId:guid}/accept", async (Guid connectionId, ClaimsPrincipal user,
            IConnectionService service, CancellationToken ct) =>
        {
            var id = ActorId(user);
            return id is null ? Results.Unauthorized() : Result(await service.AcceptAsync(id.Value, connectionId, ct));
        });
        group.MapPost("/{connectionId:guid}/reject", async (Guid connectionId, ClaimsPrincipal user,
            IConnectionService service, CancellationToken ct) =>
        {
            var id = ActorId(user);
            return id is null ? Results.Unauthorized() : Result(await service.RejectAsync(id.Value, connectionId, ct));
        });
        group.MapPost("/{connectionId:guid}/cancel", async (Guid connectionId, ClaimsPrincipal user,
            IConnectionService service, CancellationToken ct) =>
        {
            var id = ActorId(user);
            return id is null ? Results.Unauthorized() : Result(await service.CancelAsync(id.Value, connectionId, ct));
        });
    }

    private static Guid? ActorId(ClaimsPrincipal user) =>
        Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    private static IResult Result(ConnectionResult result) => result.StatusCode switch
    {
        200 => Results.Ok(new { result.Id }),
        201 => Results.Created($"/api/v1/connections/{result.Id}", new { result.Id }),
        400 => Results.BadRequest(new { result.Error }),
        403 => Results.Forbid(),
        404 => Results.NotFound(new { result.Error }),
        409 => Results.Conflict(new { result.Error }),
        422 => Results.UnprocessableEntity(new { result.Error }),
        _ => Results.Problem(statusCode: result.StatusCode, title: result.Error)
    };
}
