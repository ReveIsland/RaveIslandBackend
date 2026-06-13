using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.AddRedisDistributedCache("cache");
builder.AddNpgsqlDbContext<AppDbContext>("raveisland");

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantIdResolver, TenantIdResolver>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IKeycloakAdminService, KeycloakAdminService>();

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
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(AppRoles.Admin));

    options.AddPolicy(AuthorizationPolicies.TenantAdminOnly, policy =>
        policy.RequireRole(AppRoles.TenantAdmin));

    options.AddPolicy(AuthorizationPolicies.TenantMember, policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(AppRoles.Admin) ||
            context.User.IsInRole(AppRoles.TenantAdmin) ||
            context.User.IsInRole(AppRoles.TenantUser)));

    options.AddPolicy(AuthorizationPolicies.TenantAdminOrAdmin, policy =>
        policy.RequireAssertion(context =>
            context.User.IsInRole(AppRoles.Admin) ||
            context.User.IsInRole(AppRoles.TenantAdmin)));
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

app.MapFeatureEndpoints();
app.MapDefaultEndpoints();

app.Run();
