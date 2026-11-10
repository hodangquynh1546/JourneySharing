using System.Security.Claims;
using JourneySharing.Domain;
using JourneySharing.Infrastructure;
using Microsoft.EntityFrameworkCore;

public static class MessageEndpoints
{
    public static void MapMessageEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/connections/{connectionId:guid}/messages")
            .RequireAuthorization();
        group.MapGet("/", List);
        group.MapPost("/", Send);
    }

    private static async Task<IResult> List(Guid connectionId, ClaimsPrincipal principal,
        JourneyDbContext db, CancellationToken ct)
    {
        var connection = await AuthorizedConnection(connectionId, principal, db, ct);
        if (connection is null) return Results.NotFound();
        if (!CanRead(connection.Status)) return Results.Conflict(new { error = "Chat opens after a connection is accepted." });
        var latest = await db.Messages.AsNoTracking().Where(x => x.ConnectionId == connectionId)
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Take(100)
            .Select(x => new { x.Id, x.SenderId, x.Text, x.CreatedAt }).ToListAsync(ct);
        latest.Reverse();
        return Results.Ok(latest);
    }

    private static async Task<IResult> Send(Guid connectionId, MessageInput input,
        ClaimsPrincipal principal, JourneyDbContext db, CancellationToken ct)
    {
        var connection = await AuthorizedConnection(connectionId, principal, db, ct);
        if (connection is null) return Results.NotFound();
        if (connection.Status is not (ConnectionStatus.Accepted or ConnectionStatus.InProgress))
            return Results.Conflict(new { error = "Chat is closed for this connection." });
        var text = input.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text) || text.Length > 1000)
            return Results.BadRequest(new { error = "Message must contain 1–1000 characters." });
        var userId = Guid.Parse(principal.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var message = new ConnectionMessage
        {
            ConnectionId = connectionId, SenderId = userId, Text = text
        };
        db.Messages.Add(message);
        await db.SaveChangesAsync(ct);
        return Results.Created($"/api/v1/connections/{connectionId}/messages/{message.Id}",
            new { message.Id, message.SenderId, message.Text, message.CreatedAt });
    }

    private static bool CanRead(ConnectionStatus status) => status is
        ConnectionStatus.Accepted or ConnectionStatus.InProgress or
        ConnectionStatus.Completed or ConnectionStatus.Disputed;

    private static async Task<TripConnection?> AuthorizedConnection(Guid connectionId,
        ClaimsPrincipal principal, JourneyDbContext db, CancellationToken ct)
    {
        if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return null;
        var connection = await db.Connections.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == connectionId, ct);
        if (connection is null) return null;
        var driverOwns = await db.Trips.AnyAsync(x => x.Id == connection.TripId && x.UserId == userId, ct);
        var passengerOwns = connection.TravelRequestId is Guid requestId
            ? await db.TravelRequests.AnyAsync(x => x.Id == requestId && x.UserId == userId, ct)
            : await db.Trips.AnyAsync(x => x.Id == connection.OtherTripId && x.UserId == userId, ct);
        return driverOwns || passengerOwns ? connection : null;
    }
}

public sealed record MessageInput(string? Text);
