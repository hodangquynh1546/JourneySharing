using JourneySharing.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("DATABASE_URL is required.");
builder.Services.AddDbContext<JourneyDbContext>(options =>
    options.UseNpgsql(connectionString, npgsql => npgsql.UseNetTopologySuite()));
builder.Services.AddHealthChecks().AddDbContextCheck<JourneyDbContext>();

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.Run();

public partial class Program;
