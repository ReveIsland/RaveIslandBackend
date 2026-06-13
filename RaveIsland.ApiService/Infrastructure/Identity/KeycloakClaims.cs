using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RaveIsland.ApiService.Infrastructure.Identity;

public sealed record UserProfile(string? Name, string? Email, IReadOnlyList<string> Roles);

public sealed record KeycloakUserInfo(
    string? Name,
    string? Email,
    IReadOnlyList<string> Roles,
    string? PreferredUsername);

public static class KeycloakClaims
{
    private static readonly HashSet<string> IgnoredRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "offline_access",
        "uma_authorization",
    };

    public static async Task<UserProfile> ResolveUserProfileAsync(
        ClaimsPrincipal user,
        HttpContext httpContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var name = GetDisplayName(user);
        var email = GetEmail(user);
        var roles = GetRoles(user);

        var userInfo = await TryFetchUserInfoAsync(
            httpContext,
            configuration,
            environment,
            cancellationToken);

        if (userInfo is not null)
        {
            name = FirstNonEmpty(name, userInfo.Name, userInfo.PreferredUsername);
            email = FirstNonEmpty(email, userInfo.Email);
            if (roles.Count == 0 && userInfo.Roles.Count > 0)
            {
                roles = userInfo.Roles;
            }
        }

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
        {
            var adminUser = await TryFetchUserFromAdminApiAsync(
                user,
                configuration,
                environment,
                cancellationToken);

            if (adminUser is not null)
            {
                name = FirstNonEmpty(name, adminUser.Name, adminUser.PreferredUsername);
                email = FirstNonEmpty(email, adminUser.Email);
            }
        }

        name = FirstNonEmpty(
            name,
            user.FindFirst("preferred_username")?.Value,
            ComposeName(user.FindFirst("given_name")?.Value, user.FindFirst("family_name")?.Value));

        return new UserProfile(name, email, roles);
    }

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

    public static IReadOnlyList<string> GetRoles(ClaimsPrincipal? user)
    {
        if (user is null)
        {
            return [];
        }

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

    public static Guid? GetTenantId(ClaimsPrincipal user)
    {
        var value = user.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(value, out var tenantId) ? tenantId : null;
    }

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

        if (roles.Length == 0 && payload.RealmAccess?.Roles is { Length: > 0 } realmRoles)
        {
            roles = realmRoles
                .Where(IsMeaningfulRole)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return new KeycloakUserInfo(
            FirstNonEmpty(payload.Name, payload.PreferredUsername, ComposeName(payload.GivenName, payload.FamilyName)),
            payload.Email,
            roles,
            payload.PreferredUsername);
    }

    public static async Task<KeycloakUserInfo?> TryFetchUserFromAdminApiAsync(
        ClaimsPrincipal user,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken cancellationToken = default)
    {
        var sub = user.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(sub))
        {
            return null;
        }

        var adminPassword = configuration["KC_BOOTSTRAP_ADMIN_PASSWORD"]
            ?? configuration["Parameters:keycloak-password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            return null;
        }

        var adminUsername = configuration["KC_BOOTSTRAP_ADMIN_USERNAME"] ?? "admin";
        var keycloakBase = ResolveKeycloakBase(configuration, environment);

        using var http = CreateKeycloakHttpClient(environment);

        using var tokenContent = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = "admin-cli",
            ["grant_type"] = "password",
            ["username"] = adminUsername,
            ["password"] = adminPassword,
        });

        using var tokenResponse = await http.PostAsync(
            $"{keycloakBase}/realms/master/protocol/openid-connect/token",
            tokenContent,
            cancellationToken);

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<AdminTokenResponse>(cancellationToken);
        if (string.IsNullOrWhiteSpace(tokenPayload?.AccessToken))
        {
            return null;
        }

        using var userRequest = new HttpRequestMessage(
            HttpMethod.Get,
            $"{keycloakBase}/admin/realms/raveisland/users/{sub}");
        userRequest.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenPayload.AccessToken);

        using var userResponse = await http.SendAsync(userRequest, cancellationToken);
        if (!userResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var adminUser = await userResponse.Content.ReadFromJsonAsync<AdminUserResponse>(cancellationToken);
        if (adminUser is null)
        {
            return null;
        }

        return new KeycloakUserInfo(
            FirstNonEmpty(
                adminUser.Name,
                ComposeName(adminUser.FirstName, adminUser.LastName),
                adminUser.Username),
            adminUser.Email,
            [],
            adminUser.Username);
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

    internal static HttpClient CreateKeycloakHttpClient(IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            return new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        }

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
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

        return ExtractRolesFromRealmAccessJson(realmAccess);
    }

    private static string[] ExtractRolesFromRealmAccessJson(string realmAccess)
    {
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
                .Where(IsMeaningfulRole)
                .Select(role => role!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
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

        [JsonPropertyName("realm_access")]
        public RealmAccessResponse? RealmAccess { get; set; }
    }

    private sealed class RealmAccessResponse
    {
        [JsonPropertyName("roles")]
        public string[]? Roles { get; set; }
    }

    private sealed class AdminTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class AdminUserResponse
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        public string? Name => FirstNonEmpty(
            ComposeName(FirstName, LastName),
            Username);
    }
}
