using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RaveIsland.ApiService.Infrastructure.Identity;

public interface IKeycloakAdminService
{
    Task<string> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default);

    Task SetUserEnabledAsync(string keycloakUserId, bool enabled, CancellationToken cancellationToken = default);

    Task UpdateUserRoleAsync(string keycloakUserId, string newRole, CancellationToken cancellationToken = default);

    Task<string?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task SetTenantAttributeAsync(string keycloakUserId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlySet<string>> GetPlatformAdminUserIdsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, KeycloakUserProfile>> GetUsersByIdsAsync(
        IEnumerable<string> keycloakUserIds,
        CancellationToken cancellationToken = default);
}

public sealed record KeycloakUserProfile(
    string Id,
    string? Email,
    string? FirstName,
    string? LastName);

public sealed class KeycloakAdminService(
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<KeycloakAdminService> logger) : IKeycloakAdminService
{
    private const string Realm = "raveisland";

    public async Task<string> CreateUserAsync(
        string email,
        string firstName,
        string lastName,
        string password,
        Guid tenantId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        var username = email;
        var createPayload = new
        {
            username,
            email,
            firstName,
            lastName,
            enabled = true,
            emailVerified = true,
            requiredActions = Array.Empty<string>(),
            attributes = new Dictionary<string, string[]>
            {
                ["tenant_id"] = [tenantId.ToString()],
            },
            credentials = new[]
            {
                new { type = "password", value = password, temporary = false },
            },
        };

        using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{keycloakBase}/admin/realms/{Realm}/users")
        {
            Content = JsonContent.Create(createPayload),
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await http.SendAsync(createRequest, cancellationToken);
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create Keycloak user: {(int)createResponse.StatusCode} {body}");
        }

        var location = createResponse.Headers.Location?.ToString();
        var userId = location?.Split('/').LastOrDefault();
        if (string.IsNullOrWhiteSpace(userId))
        {
            var users = await GetUsersByEmailAsync(http, keycloakBase, token, email, cancellationToken);
            userId = users.FirstOrDefault()?.Id
                ?? throw new InvalidOperationException("Created user but could not resolve user id.");
        }

        await AssignRealmRoleAsync(http, keycloakBase, token, userId, role, cancellationToken);
        logger.LogInformation("Created Keycloak user {UserId} with role {Role}", userId, role);
        return userId;
    }

    public async Task SetUserEnabledAsync(string keycloakUserId, bool enabled, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        var user = await GetUserForUpdateAsync(http, keycloakBase, token, keycloakUserId, cancellationToken)
            ?? throw new InvalidOperationException($"Keycloak user '{keycloakUserId}' was not found.");

        user.Enabled = enabled;
        await PutUserAsync(http, keycloakBase, token, keycloakUserId, user, cancellationToken);
    }

    public async Task UpdateUserRoleAsync(string keycloakUserId, string newRole, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        foreach (var roleToRemove in Common.AppRoles.TenantRoles)
        {
            await TryRemoveRealmRoleAsync(http, keycloakBase, token, keycloakUserId, roleToRemove, cancellationToken);
        }

        await AssignRealmRoleAsync(http, keycloakBase, token, keycloakUserId, newRole, cancellationToken);
    }

    public async Task<string?> GetUserIdByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        var users = await GetUsersByEmailAsync(http, keycloakBase, token, email.Trim(), cancellationToken);
        return users.FirstOrDefault()?.Id;
    }

    public async Task SetTenantAttributeAsync(
        string keycloakUserId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        var user = await GetUserForUpdateAsync(http, keycloakBase, token, keycloakUserId, cancellationToken);
        if (user is null)
        {
            logger.LogWarning("Keycloak user {UserId} was not found while syncing tenant_id.", keycloakUserId);
            return;
        }

        user.Attributes ??= new Dictionary<string, string[]>();
        if (user.Attributes.TryGetValue("tenant_id", out var existing) &&
            existing.Any(value => string.Equals(value, tenantId.ToString(), StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        user.Attributes["tenant_id"] = [tenantId.ToString()];
        await PutUserAsync(http, keycloakBase, token, keycloakUserId, user, cancellationToken);
        logger.LogInformation("Set tenant_id attribute for Keycloak user {UserId}", keycloakUserId);
    }

    public async Task<IReadOnlySet<string>> GetPlatformAdminUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{keycloakBase}/admin/realms/{Realm}/roles/{Common.AppRoles.Admin}/users?max=1000");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var users = await SendAsync<UserRepresentation[]>(http, request, cancellationToken) ?? [];
        return users
            .Where(u => !string.IsNullOrWhiteSpace(u.Id))
            .Select(u => u.Id!)
            .ToHashSet(StringComparer.Ordinal);
    }

    public async Task<IReadOnlyDictionary<string, KeycloakUserProfile>> GetUsersByIdsAsync(
        IEnumerable<string> keycloakUserIds,
        CancellationToken cancellationToken = default)
    {
        var ids = keycloakUserIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<string, KeycloakUserProfile>(StringComparer.Ordinal);
        }

        var token = await GetAdminTokenAsync(cancellationToken);
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

        var profiles = await Task.WhenAll(ids.Select(async id =>
        {
            var user = await GetUserForUpdateAsync(http, keycloakBase, token, id, cancellationToken);
            return user is null
                ? null
                : new KeycloakUserProfile(id, user.Email, user.FirstName, user.LastName);
        }));

        return profiles
            .Where(profile => profile is not null)
            .ToDictionary(profile => profile!.Id, profile => profile!, StringComparer.Ordinal);
    }

    private async Task<string> GetAdminTokenAsync(CancellationToken cancellationToken)
    {
        var adminPassword = configuration["KC_BOOTSTRAP_ADMIN_PASSWORD"]
            ?? configuration["Parameters:keycloak-password"]
            ?? throw new InvalidOperationException("Keycloak admin password not configured.");

        var adminUsername = configuration["KC_BOOTSTRAP_ADMIN_USERNAME"] ?? "admin";
        var keycloakBase = KeycloakClaims.ResolveKeycloakBase(configuration, environment);
        using var http = KeycloakClaims.CreateKeycloakHttpClient(environment);

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

        tokenResponse.EnsureSuccessStatusCode();
        var tokenPayload = await tokenResponse.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
            ?? throw new InvalidOperationException("Empty token response from Keycloak.");

        return tokenPayload.AccessToken
            ?? throw new InvalidOperationException("Keycloak token response did not include an access token.");
    }

    private static async Task AssignRealmRoleAsync(
        HttpClient http,
        string keycloakBase,
        string token,
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        using var roleRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{Realm}/roles/{roleName}");
        roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var role = await SendAsync<RoleRepresentation>(http, roleRequest, cancellationToken)
            ?? throw new InvalidOperationException($"Realm role '{roleName}' was not found.");

        using var mapRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{keycloakBase}/admin/realms/{Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role }),
        };
        mapRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(mapRequest, cancellationToken);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to assign role '{roleName}': {(int)response.StatusCode} {body}");
        }
    }

    private static async Task TryRemoveRealmRoleAsync(
        HttpClient http,
        string keycloakBase,
        string token,
        string userId,
        string roleName,
        CancellationToken cancellationToken)
    {
        using var roleRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{Realm}/roles/{roleName}");
        roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var role = await SendAsync<RoleRepresentation>(http, roleRequest, cancellationToken);
        if (role is null)
        {
            return;
        }

        using var mapRequest = new HttpRequestMessage(
            HttpMethod.Delete,
            $"{keycloakBase}/admin/realms/{Realm}/users/{userId}/role-mappings/realm")
        {
            Content = JsonContent.Create(new[] { role }),
        };
        mapRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        await http.SendAsync(mapRequest, cancellationToken);
    }

    private static async Task<UserUpdateRepresentation?> GetUserForUpdateAsync(
        HttpClient http,
        string keycloakBase,
        string token,
        string userId,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{keycloakBase}/admin/realms/{Realm}/users/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await SendAsync<UserUpdateRepresentation>(http, request, cancellationToken);
    }

    private static async Task PutUserAsync(
        HttpClient http,
        string keycloakBase,
        string token,
        string userId,
        UserUpdateRepresentation user,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, $"{keycloakBase}/admin/realms/{Realm}/users/{userId}")
        {
            Content = JsonContent.Create(user),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to update Keycloak user {userId}: {(int)response.StatusCode} {body}");
        }
    }

    private static async Task<UserRepresentation[]> GetUsersByEmailAsync(
        HttpClient http,
        string keycloakBase,
        string token,
        string email,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{keycloakBase}/admin/realms/{Realm}/users?email={Uri.EscapeDataString(email)}&exact=true");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await SendAsync<UserRepresentation[]>(http, request, cancellationToken) ?? [];
    }

    private static async Task<T?> SendAsync<T>(HttpClient http, HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await http.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class RoleRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class UserUpdateRepresentation
    {
        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("firstName")]
        public string? FirstName { get; set; }

        [JsonPropertyName("lastName")]
        public string? LastName { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("emailVerified")]
        public bool EmailVerified { get; set; }

        [JsonPropertyName("attributes")]
        public Dictionary<string, string[]>? Attributes { get; set; }

        [JsonPropertyName("requiredActions")]
        public string[] RequiredActions { get; set; } = [];
    }

    private sealed class UserRepresentation
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }
    }
}
