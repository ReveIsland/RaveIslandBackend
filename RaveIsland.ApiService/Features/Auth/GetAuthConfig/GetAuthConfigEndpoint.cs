using System.Text.Json.Serialization;
using RaveIsland.ApiService.Common;
using RaveIsland.ApiService.Infrastructure.Identity;

namespace RaveIsland.ApiService.Features.Auth.GetAuthConfig;

public sealed class GetAuthConfigEndpoint : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/api/auth/config", Handle);

    private static async Task<IResult> Handle(
        IConfiguration configuration,
        IHostEnvironment environment,
        IHttpClientFactory httpClientFactory)
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
            scope,
        });
    }

    private sealed class OpenIdDiscoveryDocument
    {
        [JsonPropertyName("scopes_supported")]
        public List<string>? ScopesSupported { get; set; }
    }
}
