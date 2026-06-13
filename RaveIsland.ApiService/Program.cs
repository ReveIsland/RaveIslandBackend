using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using RaveIsland.ApiService;
using RaveIsland.ApiService.Data;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.AddRedisDistributedCache("cache");
builder.AddNpgsqlDbContext<AppDbContext>("raveisland");

builder.Services.AddCors(options =>
{
    options.AddPolicy("WebDev", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddKeycloakJwtBearer("keycloak", "raveisland", options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

        if (builder.Environment.IsDevelopment())
        {
            options.Authority = "https://localhost:8080/realms/raveisland";
        }

        // Keycloak SPA access tokens often omit aud; azp identifies the client instead.
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidateAudience = false;
        options.TokenValidationParameters.RoleClaimType = "roles";
        options.TokenValidationParameters.NameClaimType = "preferred_username";

        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = context =>
            {
                var azp = context.Principal?.FindFirst("azp")?.Value;
                if (!string.Equals(azp, "raveisland-web", StringComparison.Ordinal))
                {
                    context.Fail("Access token was not issued for the raveisland-web client.");
                    return Task.CompletedTask;
                }

                if (context.Principal is not null)
                {
                    KeycloakClaims.MapRealmRoles(context.Principal);
                }

                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin"));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("WebDev");
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "Rave Island API" }));

app.MapGet("/api/auth/config", async (IConfiguration configuration, IHostEnvironment environment, IHttpClientFactory httpClientFactory) =>
{
    const string realm = "raveisland";
    const string clientId = "raveisland-web";

    var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);

    var scope = "openid roles";
    try
    {
        var http = httpClientFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(5);
        using var discoveryResponse = await http.GetAsync(
            $"{keycloakBase}/realms/{realm}/.well-known/openid-configuration");
        if (discoveryResponse.IsSuccessStatusCode)
        {
            var discovery = await discoveryResponse.Content.ReadFromJsonAsync<OpenIdDiscoveryDocument>();
            if (discovery?.ScopesSupported is { Count: > 0 } supportedScopes)
            {
                var requested = new[] { "openid", "profile", "email", "roles" };
                scope = string.Join(' ', requested.Where(supportedScopes.Contains));
            }
        }
    }
    catch
    {
        // Fall back to the minimal scope set above.
    }

    return Results.Ok(new
    {
        authority = $"{keycloakBase}/realms/{realm}",
        clientId,
        realm,
        scope
    });
});

app.MapGet("/api/me", async (
    ClaimsPrincipal user,
    HttpContext httpContext,
    IConfiguration configuration,
    IHostEnvironment environment,
    CancellationToken cancellationToken) =>
{
    if (user.Identity?.IsAuthenticated != true)
    {
        return Results.Unauthorized();
    }

    var profile = await KeycloakClaims.ResolveUserProfileAsync(
        user,
        httpContext,
        configuration,
        environment,
        cancellationToken);

    return Results.Ok(new
    {
        name = profile.Name,
        email = profile.Email,
        roles = profile.Roles
    });
}).RequireAuthorization();

app.MapGet("/api/items", async (AppDbContext db, CancellationToken cancellationToken) =>
{
    var items = await db.Items
        .AsNoTracking()
        .OrderByDescending(i => i.CreatedAt)
        .Select(i => new { i.Id, i.Title, i.CreatedBy, i.CreatedAt })
        .ToListAsync(cancellationToken);

    return Results.Ok(items);
}).RequireAuthorization();

app.MapPost("/api/items", async (CreateItemRequest request, AppDbContext db, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return Results.BadRequest(new { error = "Title is required." });
    }

    var item = new Item
    {
        Id = Guid.NewGuid(),
        Title = request.Title.Trim(),
        CreatedBy = KeycloakClaims.GetDisplayName(user) ?? user.FindFirst("preferred_username")?.Value,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    db.Items.Add(item);
    await db.SaveChangesAsync(cancellationToken);

    return Results.Created($"/api/items/{item.Id}", new
    {
        item.Id,
        item.Title,
        item.CreatedBy,
        item.CreatedAt
    });
}).RequireAuthorization();

app.MapGet("/api/admin/stats", async (IDistributedCache cache, AppDbContext db, CancellationToken cancellationToken) =>
{
    const string cacheKey = "admin:stats:views";
    var raw = await cache.GetStringAsync(cacheKey, cancellationToken);
    var count = int.TryParse(raw, out var current) ? current + 1 : 1;
    await cache.SetStringAsync(cacheKey, count.ToString(), cancellationToken);

    var itemCount = await db.Items.CountAsync(cancellationToken);

    return Results.Ok(new { viewCount = count, itemCount, cached = true });
}).RequireAuthorization("AdminOnly");

app.MapDefaultEndpoints();

app.Run();

record CreateItemRequest(string Title);

file sealed class OpenIdDiscoveryDocument
{
    [System.Text.Json.Serialization.JsonPropertyName("scopes_supported")]
    public List<string>? ScopesSupported { get; set; }
}
