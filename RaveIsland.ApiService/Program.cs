using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Email;
using RaveIsland.ApiService.Infrastructure.Identity;
using RaveIsland.ApiService.Infrastructure.Persistence;
using RaveIsland.ApiService.Features.Events.CheckIn;
using RaveIsland.ApiService.Features.Events.Promos;
using RaveIsland.ApiService.Features.Events.Publish;
using RaveIsland.ApiService.Infrastructure.Billing;
using RaveIsland.ApiService.Infrastructure.Lookups;
using RaveIsland.ApiService.Infrastructure.Media;
using RaveIsland.ApiService.Infrastructure.Tenancy;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();

builder.AddRedisDistributedCache("cache");
builder.AddNpgsqlDbContext<AppDbContext>("raveisland");

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection(SmtpOptions.SectionName));
builder.Services.Configure<AppOptions>(builder.Configuration.GetSection(AppOptions.SectionName));
builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection(StripeOptions.SectionName));

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantIdResolver, TenantIdResolver>();
builder.Services.AddScoped<ITenantMembershipResolver, TenantMembershipResolver>();
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<IKeycloakAdminService, KeycloakAdminService>();
builder.Services.AddScoped<ILookupCacheService, LookupCacheService>();
builder.Services.AddScoped<ILookupSeeder, LookupSeeder>();
builder.Services.AddScoped<EventPublishValidator>();
builder.Services.AddScoped<IEventPublishValidator, SubscriptionAwareEventPublishValidator>();
builder.Services.AddScoped<IPromoValidationService, PromoValidationService>();
builder.Services.AddSingleton<IQrTokenService, QrTokenService>();
builder.Services.AddSingleton<IMediaStorageService, LocalMediaStorageService>();
builder.Services.AddScoped<IStripeCustomerService, StripeCustomerService>();
builder.Services.AddScoped<IStripeCheckoutService, StripeCheckoutService>();
builder.Services.AddScoped<IStripePortalService, StripePortalService>();
builder.Services.AddScoped<IStripeEntitlementService, StripeEntitlementService>();
builder.Services.AddScoped<IStripeMeterService, StripeMeterService>();
builder.Services.AddScoped<IStripeWebhookHandler, StripeWebhookHandler>();
builder.Services.AddScoped<IStripeBillingSyncService, StripeBillingSyncService>();
builder.Services.AddScoped<StripeBillingSyncService>();
builder.Services.AddScoped<IBillingSetupService, BillingSetupService>();

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
            OnTokenValidated = async context =>
            {
                var azp = context.Principal?.FindFirst("azp")?.Value;
                if (!string.Equals(azp, "raveisland-web", StringComparison.Ordinal))
                {
                    context.Fail("Access token was not issued for the raveisland-web client.");
                    return;
                }

                if (context.Principal is not null)
                {
                    KeycloakClaims.MapRealmRoles(context.Principal);

                    var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                    var environment = context.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();
                    await KeycloakClaims.EnrichPrincipalFromUserInfoAsync(
                        context.Principal,
                        context.HttpContext,
                        configuration,
                        environment,
                        context.HttpContext.RequestAborted);
                }
            },
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireAssertion(context =>
            AuthorizationRoleHelper.HasAnyRole(context.User, AppRoles.Admin)));

    options.AddPolicy(AuthorizationPolicies.TenantAdminOnly, policy =>
        policy.RequireAssertion(context =>
            AuthorizationRoleHelper.HasAnyRole(context.User, AppRoles.TenantAdmin)));

    options.AddPolicy(AuthorizationPolicies.TenantMember, policy =>
        policy.RequireAssertion(context =>
            AuthorizationRoleHelper.HasAnyRole(
                context.User,
                AppRoles.Admin,
                AppRoles.TenantAdmin,
                AppRoles.TenantUser)));

    options.AddPolicy(AuthorizationPolicies.TenantAdminOrAdmin, policy =>
        policy.RequireAssertion(context =>
            AuthorizationRoleHelper.HasAnyRole(
                context.User,
                AppRoles.Admin,
                AppRoles.TenantAdmin)));
});

var app = builder.Build();

var stripeOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<StripeOptions>>().Value;
if (stripeOptions.IsConfigured)
{
    StripeConfiguration.ApiKey = stripeOptions.SecretKey;
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    var seeder = scope.ServiceProvider.GetRequiredService<ILookupSeeder>();
    await seeder.SeedAsync();
}

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/webhooks/stripe"))
    {
        context.Request.EnableBuffering();
    }

    await next();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("WebDev");
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
});
app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseAuthorization();

app.MapGet("/", () => Results.Ok(new { message = "Rave Island API" }));

app.MapFeatureEndpoints();
app.MapDefaultEndpoints();

app.Run();
