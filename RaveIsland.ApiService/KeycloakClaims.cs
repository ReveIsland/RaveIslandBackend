using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaveIsland.ApiService;

internal sealed record KeycloakUserInfo(string? Name, string? Email, IReadOnlyList<string> Roles);

internal static class KeycloakClaims
{
    private static readonly HashSet<string> IgnoredRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "offline_access",
        "uma_authorization",
    };
    public static void MapRealmRoles(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var role in ExtractRolesFromRealmAccess(principal))
        {
            if (!principal.IsInRole(role))
            {
                identity.AddClaim(new Claim("roles", role));
            }
        }
    }

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal user)
    {
        return user.FindAll("roles")
            .Select(claim => claim.Value)
            .Concat(ExtractRolesFromRealmAccess(user))
            .Where(IsMeaningfulRole)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? GetDisplayName(ClaimsPrincipal user)
    {
        return FirstNonEmpty(
            user.FindFirst("name")?.Value,
            user.FindFirst("preferred_username")?.Value,
            ComposeName(user.FindFirst("given_name")?.Value, user.FindFirst("family_name")?.Value),
            user.Identity?.Name);
    }

    public static string? GetEmail(ClaimsPrincipal user) =>
        user.FindFirst("email")?.Value;

    public static async Task<KeycloakUserInfo?> TryFetchUserInfoAsync(
        HttpContext httpContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var authorization = httpContext.Request.Headers.Authorization.ToString();
        if (!authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var keycloakBase = ResolveKeycloakBase(configuration, environment);
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{keycloakBase}/realms/raveisland/protocol/openid-connect/userinfo");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);

        using var http = CreateKeycloakHttpClient(environment);

        using var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<UserInfoResponse>(cancellationToken);
        if (payload is null)
        {
            return null;
        }

        var roles = payload.Roles?
            .Where(IsMeaningfulRole)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        return new KeycloakUserInfo(
            FirstNonEmpty(payload.Name, payload.PreferredUsername, ComposeName(payload.GivenName, payload.FamilyName)),
            payload.Email,
            roles);
    }

    public static string ResolveKeycloakBase(IConfiguration configuration, IHostEnvironment environment)
    {
        var keycloakBase = configuration["KEYCLOAK_HTTP"]
            ?? configuration["services:keycloak:http:0"]
            ?? "http://localhost:8080";

        if (environment.IsDevelopment() &&
            (keycloakBase.Contains("keycloak", StringComparison.OrdinalIgnoreCase) ||
             keycloakBase.StartsWith("https+http://", StringComparison.OrdinalIgnoreCase)))
        {
            keycloakBase = "https://localhost:8080";
        }

        return keycloakBase.TrimEnd('/');
    }

    private static HttpClient CreateKeycloakHttpClient(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(5) };
    }

    private static bool IsMeaningfulRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role) || IgnoredRoles.Contains(role))
        {
            return false;
        }

        return !role.StartsWith("default-roles-", StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> ExtractRolesFromRealmAccess(ClaimsPrincipal user)
    {
        var realmAccess = user.FindFirst("realm_access")?.Value;
        if (string.IsNullOrWhiteSpace(realmAccess))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(realmAccess);
            if (!document.RootElement.TryGetProperty("roles", out var rolesElement) ||
                rolesElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return rolesElement.EnumerateArray()
                .Where(role => role.ValueKind == JsonValueKind.String)
                .Select(role => role.GetString())
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .ToArray();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ComposeName(string? givenName, string? familyName)
    {
        var parts = new[] { givenName, familyName }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToArray();

        return parts.Length == 0 ? null : string.Join(' ', parts);
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private sealed class UserInfoResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("preferred_username")]
        public string? PreferredUsername { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("roles")]
        public string[]? Roles { get; set; }
    }
}
