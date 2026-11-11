using JourneySharing.Infrastructure;
using JourneySharing.Domain;
using JourneySharing.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json.Serialization;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
var jwtSecret = builder.Configuration["JWT_SECRET"];
if (string.IsNullOrWhiteSpace(jwtSecret) || Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException("JWT_SECRET must contain at least 32 UTF-8 bytes.");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret));
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = signingKey,
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    });
builder.Services.AddAuthorization();
var frontendOrigins = (builder.Configuration["FRONTEND_URL"] ?? "http://localhost:4200,http://127.0.0.1:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    .Select(origin => origin.TrimEnd('/'))
    .Concat(["https://localhost", "capacitor://localhost"])
    .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
    policy.WithOrigins(frontendOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddScoped<IConnectionService, ConnectionService>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", policy =>
    {
        policy.PermitLimit = 10;
        policy.Window = TimeSpan.FromMinutes(1);
    });
});
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("DATABASE_URL is required.");
builder.Services.AddDbContext<JourneyDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));
builder.Services.AddHealthChecks().AddDbContextCheck<JourneyDbContext>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseRateLimiter();
app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapPostEndpoints();
app.MapConnectionEndpoints();
app.MapMessageEndpoints();

app.MapPost("/api/v1/auth/register", async (RegisterRequest request, JourneyDbContext db,
    IPasswordHasher<User> hasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    if (!new EmailAddressAttribute().IsValid(email) ||
        string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Length > 160 ||
        request.Password.Length < 12 || request.Password.Length > 128)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["account"] = ["Enter a valid name, email, and password of 12–128 characters."]
        });
    if (await db.Users.AnyAsync(x => x.Email == email)) return Results.Conflict();
    var user = new User { FullName = request.FullName.Trim(), Email = email, PasswordHash = string.Empty };
    user.PasswordHash = hasher.HashPassword(user, request.Password);
    db.Users.Add(user);
    try { await db.SaveChangesAsync(); }
    catch (DbUpdateException) { return Results.Conflict(); }
    return Results.Created($"/api/v1/users/me", new { user.Id, user.FullName, user.Email });
}).RequireRateLimiting("auth");

app.MapPost("/api/v1/auth/login", async (LoginRequest request, JourneyDbContext db,
    IPasswordHasher<User> hasher) =>
{
    var email = request.Email.Trim().ToLowerInvariant();
    var user = await db.Users.SingleOrDefaultAsync(x => x.Email == email && x.IsActive);
    if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password)
        == PasswordVerificationResult.Failed) return Results.Unauthorized();
    var expiry = DateTime.UtcNow.AddMinutes(30);
    var token = new JwtSecurityToken(
        claims: [new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                 new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())],
        expires: expiry,
        signingCredentials: new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256));
    return Results.Ok(new { accessToken = new JwtSecurityTokenHandler().WriteToken(token), expiresAt = expiry });
}).RequireRateLimiting("auth");

app.MapGet("/api/v1/users/me", async (ClaimsPrincipal principal, JourneyDbContext db) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();
    var user = await db.Users.FindAsync(userId);
    return user is null || !user.IsActive ? Results.Unauthorized() : Results.Ok(new
    {
        user.Id, user.FullName, user.Email, user.Phone, user.AvatarUrl, user.CreatedAt
    });
}).RequireAuthorization();

app.MapPatch("/api/v1/users/me", async (UpdateProfileRequest request,
    ClaimsPrincipal principal, JourneyDbContext db) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        return Results.Unauthorized();
    var fullName = request.FullName?.Trim();
    var phone = request.Phone?.Trim();
    if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 160 ||
        (phone?.Length > 0 && !System.Text.RegularExpressions.Regex.IsMatch(phone, @"^\+?[0-9]{8,15}$")))
        return Results.BadRequest(new { error = "Enter a name and an optional phone number with 8–15 digits." });
    var user = await db.Users.FindAsync(userId);
    if (user is null || !user.IsActive) return Results.Unauthorized();
    user.FullName = fullName;
    user.Phone = string.IsNullOrEmpty(phone) ? null : phone;
    await db.SaveChangesAsync();
    return Results.Ok(new { user.Id, user.FullName, user.Email, user.Phone });
}).RequireAuthorization();
app.Run();

public partial class Program;
public sealed record RegisterRequest(string FullName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record UpdateProfileRequest(string? FullName, string? Phone);
