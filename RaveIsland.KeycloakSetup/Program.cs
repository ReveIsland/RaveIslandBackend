using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

const string realmRoleAdmin = "admin";

var keycloakBase = GetRequired("services__keycloak__http__0", "KEYCLOAK_HTTP", "ConnectionStrings__keycloak")
    .TrimEnd('/');
var realm = Environment.GetEnvironmentVariable("REALM") ?? "raveisland";
var adminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
var adminUserPassword = GetRequired("ADMIN_USER_PASSWORD");

var kcAdminUser = Environment.GetEnvironmentVariable("KEYCLOAK_ADMIN")
    ?? Environment.GetEnvironmentVariable("KC_BOOTSTRAP_ADMIN_USERNAME")
    ?? "admin";
var kcAdminPassword = GetOptional(
    "KC_BOOTSTRAP_ADMIN_PASSWORD",
    "KEYCLOAK_ADMIN_PASSWORD",
    "Parameters__keycloak-password")
    ?? throw new InvalidOperationException("Keycloak bootstrap admin password not configured.");

using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

Console.WriteLine("Waiting for Keycloak at {0}...", keycloakBase);
await WaitForKeycloakAsync(http, keycloakBase);

Console.WriteLine("Acquiring Keycloak admin token...");
var token = await GetAdminTokenAsync(http, keycloakBase, kcAdminUser, kcAdminPassword);

var existingUsers = await GetUsersByUsernameAsync(http, keycloakBase, realm, token, adminUsername);
var user = existingUsers.FirstOrDefault(u => string.Equals(u.Username, adminUsername, StringComparison.OrdinalIgnoreCase));

if (user?.Id is null)
{
    Console.WriteLine("Creating realm user '{0}' in '{1}'...", adminUsername, realm);
    var createPayload = new
    {
        username = adminUsername,
        enabled = true,
        emailVerified = true,
        firstName = "Admin",
        lastName = "User",
        email = $"{adminUsername}@raveisland.local",
        credentials = new[]
        {
            new { type = "password", value = adminUserPassword, temporary = false }
        }
    };

    using var createRequest = new HttpRequestMessage(HttpMethod.Post, $"{keycloakBase}/admin/realms/{realm}/users")
    {
        Content = JsonContent.Create(createPayload)
    };
    createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createResponse = await http.SendAsync(createRequest);
    if (createResponse.StatusCode != HttpStatusCode.Created)
    {
        var body = await createResponse.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Failed to create user '{adminUsername}': {(int)createResponse.StatusCode} {body}");
    }

    var location = createResponse.Headers.Location?.ToString();
    user = new UserRepresentation
    {
        Id = location?.Split('/').LastOrDefault(),
        Username = adminUsername
    };

    if (string.IsNullOrWhiteSpace(user.Id))
    {
        existingUsers = await GetUsersByUsernameAsync(http, keycloakBase, realm, token, adminUsername);
        user = existingUsers.First(u => string.Equals(u.Username, adminUsername, StringComparison.OrdinalIgnoreCase));
    }
}
else
{
    Console.WriteLine("Realm user '{0}' already exists (id: {1}).", adminUsername, user.Id);
}

await EnsureUserProfileAsync(http, keycloakBase, realm, token, user.Id!, adminUsername);
await EnsurePasswordAsync(http, keycloakBase, realm, token, user.Id!, adminUserPassword);
await EnsureRealmRoleAsync(http, keycloakBase, realm, token, user.Id!, realmRoleAdmin);
await EnsureAudienceClientScopeAsync(http, keycloakBase, realm, token, "raveisland-web");
await EnsureProfileClientScopeAsync(http, keycloakBase, realm, token);
await EnsureEmailClientScopeAsync(http, keycloakBase, realm, token);
await EnsureDefaultClientScopesAsync(
    http,
    keycloakBase,
    realm,
    token,
    "profile",
    "email",
    "roles",
    "web-origins",
    "acr",
    "role_list",
    "raveisland-audience");
await EnsureClientDefaultScopesAsync(
    http,
    keycloakBase,
    realm,
    token,
    "raveisland-web",
    "profile",
    "email",
    "roles",
    "raveisland-audience");

Console.WriteLine("Keycloak setup completed for user '{0}'.", adminUsername);
return;

static string GetRequired(params string[] keys)
{
    var value = GetOptional(keys);
    if (!string.IsNullOrWhiteSpace(value))
    {
        return value;
    }

    throw new InvalidOperationException($"Required configuration missing. Set one of: {string.Join(", ", keys)}");
}

static string? GetOptional(params string[] keys)
{
    foreach (var key in keys)
    {
        var value = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }
    }

    return null;
}

static async Task<UserRepresentation[]> GetUsersByUsernameAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string username)
{
    using var request = new HttpRequestMessage(
        HttpMethod.Get,
        $"{keycloakBase}/admin/realms/{realm}/users?username={Uri.EscapeDataString(username)}");
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    return await SendAsync<UserRepresentation[]>(http, request) ?? [];
}

static async Task WaitForKeycloakAsync(HttpClient http, string keycloakBase)
{
    for (var attempt = 1; attempt <= 60; attempt++)
    {
        try
        {
            var response = await http.GetAsync($"{keycloakBase}/realms/master");
            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (Exception ex) when (attempt < 60)
        {
            Console.WriteLine("Keycloak not ready (attempt {0}/60): {1}", attempt, ex.Message);
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    throw new TimeoutException("Keycloak did not become ready in time.");
}

static async Task<string> GetAdminTokenAsync(HttpClient http, string keycloakBase, string username, string password)
{
    using var content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = "admin-cli",
        ["username"] = username,
        ["password"] = password
    });

    var response = await http.PostAsync($"{keycloakBase}/realms/master/protocol/openid-connect/token", content);
    response.EnsureSuccessStatusCode();

    var tokenResponse = await response.Content.ReadFromJsonAsync<TokenResponse>()
        ?? throw new InvalidOperationException("Empty token response from Keycloak.");

    if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
    {
        throw new InvalidOperationException("Keycloak token response did not include an access token.");
    }

    return tokenResponse.AccessToken;
}

static async Task EnsurePasswordAsync(HttpClient http, string keycloakBase, string realm, string token, string userId, string password)
{
    using var request = new HttpRequestMessage(HttpMethod.Put, $"{keycloakBase}/admin/realms/{realm}/users/{userId}/reset-password")
    {
        Content = JsonContent.Create(new { type = "password", value = password, temporary = false })
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Failed to set password for user {userId}: {(int)response.StatusCode} {body}");
    }
}

static async Task EnsureUserProfileAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string userId,
    string username)
{
    using var request = new HttpRequestMessage(HttpMethod.Put, $"{keycloakBase}/admin/realms/{realm}/users/{userId}")
    {
        Content = JsonContent.Create(new
        {
            enabled = true,
            emailVerified = true,
            firstName = "Admin",
            lastName = "User",
            email = $"{username}@raveisland.local",
        }),
    };
    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await http.SendAsync(request);
    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to update profile for user '{username}': {(int)response.StatusCode} {body}");
    }
}

static async Task EnsureProfileClientScopeAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token)
{
    var scope = await EnsureClientScopeAsync(
        http,
        keycloakBase,
        realm,
        token,
        "profile",
        includeInTokenScope: true,
        displayOnConsent: true);

    await EnsureProtocolMapperAsync(
        http,
        keycloakBase,
        realm,
        token,
        scope.Id!,
        "username",
        new Dictionary<string, string>
        {
            ["user.attribute"] = "username",
            ["claim.name"] = "preferred_username",
            ["jsonType.label"] = "String",
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "true",
        },
        "oidc-usermodel-attribute-mapper");

    await EnsureProtocolMapperAsync(
        http,
        keycloakBase,
        realm,
        token,
        scope.Id!,
        "full name",
        new Dictionary<string, string>
        {
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "true",
        },
        "oidc-full-name-mapper");

    await EnsureProtocolMapperAsync(
        http,
        keycloakBase,
        realm,
        token,
        scope.Id!,
        "given name",
        new Dictionary<string, string>
        {
            ["user.attribute"] = "firstName",
            ["claim.name"] = "given_name",
            ["jsonType.label"] = "String",
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "true",
        },
        "oidc-usermodel-attribute-mapper");

    await EnsureProtocolMapperAsync(
        http,
        keycloakBase,
        realm,
        token,
        scope.Id!,
        "family name",
        new Dictionary<string, string>
        {
            ["user.attribute"] = "lastName",
            ["claim.name"] = "family_name",
            ["jsonType.label"] = "String",
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "true",
        },
        "oidc-usermodel-attribute-mapper");
}

static async Task EnsureEmailClientScopeAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token)
{
    var scope = await EnsureClientScopeAsync(
        http,
        keycloakBase,
        realm,
        token,
        "email",
        includeInTokenScope: true,
        displayOnConsent: true);

    await EnsureProtocolMapperAsync(
        http,
        keycloakBase,
        realm,
        token,
        scope.Id!,
        "email",
        new Dictionary<string, string>
        {
            ["user.attribute"] = "email",
            ["claim.name"] = "email",
            ["jsonType.label"] = "String",
            ["id.token.claim"] = "true",
            ["access.token.claim"] = "true",
            ["userinfo.token.claim"] = "true",
        },
        "oidc-usermodel-attribute-mapper");
}

static async Task<ClientScopeRepresentation> EnsureClientScopeAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string scopeName,
    bool includeInTokenScope,
    bool displayOnConsent)
{
    using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{realm}/client-scopes");
    scopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var allScopes = await SendAsync<ClientScopeRepresentation[]>(http, scopesRequest) ?? [];
    var existing = allScopes.FirstOrDefault(s =>
        string.Equals(s.Name, scopeName, StringComparison.OrdinalIgnoreCase));

    if (existing?.Id is not null)
    {
        return existing;
    }

    using var createScopeRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"{keycloakBase}/admin/realms/{realm}/client-scopes")
    {
        Content = JsonContent.Create(new
        {
            name = scopeName,
            protocol = "openid-connect",
            attributes = new Dictionary<string, string>
            {
                ["include.in.token.scope"] = includeInTokenScope ? "true" : "false",
                ["display.on.consent.screen"] = displayOnConsent ? "true" : "false",
            },
        }),
    };
    createScopeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var createScopeResponse = await http.SendAsync(createScopeRequest);
    if (createScopeResponse.StatusCode != HttpStatusCode.Created)
    {
        var body = await createScopeResponse.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to create client scope '{scopeName}': {(int)createScopeResponse.StatusCode} {body}");
    }

    var location = createScopeResponse.Headers.Location?.ToString();
    var created = new ClientScopeRepresentation
    {
        Id = location?.Split('/').LastOrDefault(),
        Name = scopeName,
    };

    if (!string.IsNullOrWhiteSpace(created.Id))
    {
        Console.WriteLine("Created client scope '{0}'.", scopeName);
        return created;
    }

    allScopes = await SendAsync<ClientScopeRepresentation[]>(http, scopesRequest) ?? [];
    return allScopes.First(s => string.Equals(s.Name, scopeName, StringComparison.OrdinalIgnoreCase));
}

static async Task EnsureProtocolMapperAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string scopeId,
    string mapperName,
    Dictionary<string, string> config,
    string protocolMapper)
{
    using var mappersRequest = new HttpRequestMessage(
        HttpMethod.Get,
        $"{keycloakBase}/admin/realms/{realm}/client-scopes/{scopeId}/protocol-mappers/models");
    mappersRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var mappers = await SendAsync<ProtocolMapperRepresentation[]>(http, mappersRequest) ?? [];
    if (mappers.Any(m => string.Equals(m.Name, mapperName, StringComparison.OrdinalIgnoreCase)))
    {
        return;
    }

    using var createMapperRequest = new HttpRequestMessage(
        HttpMethod.Post,
        $"{keycloakBase}/admin/realms/{realm}/client-scopes/{scopeId}/protocol-mappers/models")
    {
        Content = JsonContent.Create(new
        {
            name = mapperName,
            protocol = "openid-connect",
            protocolMapper,
            config,
        }),
    };
    createMapperRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await http.SendAsync(createMapperRequest);
    if (!response.IsSuccessStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to create mapper '{mapperName}' on scope '{scopeId}': {(int)response.StatusCode} {body}");
    }

    Console.WriteLine("Added protocol mapper '{0}'.", mapperName);
}

static async Task EnsureAudienceClientScopeAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string audienceClientId,
    string scopeName = "raveisland-audience")
{
    using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{realm}/client-scopes");
    scopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var allScopes = await SendAsync<ClientScopeRepresentation[]>(http, scopesRequest) ?? [];
    var scope = allScopes.FirstOrDefault(s =>
        string.Equals(s.Name, scopeName, StringComparison.OrdinalIgnoreCase));

    if (scope?.Id is null)
    {
        using var createScopeRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{keycloakBase}/admin/realms/{realm}/client-scopes")
        {
            Content = JsonContent.Create(new
            {
                name = scopeName,
                protocol = "openid-connect",
                attributes = new Dictionary<string, string>
                {
                    ["include.in.token.scope"] = "false",
                    ["display.on.consent.screen"] = "false",
                },
            }),
        };
        createScopeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createScopeResponse = await http.SendAsync(createScopeRequest);
        if (createScopeResponse.StatusCode != HttpStatusCode.Created)
        {
            var body = await createScopeResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create audience client scope '{scopeName}': {(int)createScopeResponse.StatusCode} {body}");
        }

        var location = createScopeResponse.Headers.Location?.ToString();
        scope = new ClientScopeRepresentation
        {
            Id = location?.Split('/').LastOrDefault(),
            Name = scopeName,
        };

        if (string.IsNullOrWhiteSpace(scope.Id))
        {
            using var reloadScopesRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"{keycloakBase}/admin/realms/{realm}/client-scopes");
            reloadScopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            allScopes = await SendAsync<ClientScopeRepresentation[]>(http, reloadScopesRequest) ?? [];
            scope = allScopes.First(s => string.Equals(s.Name, scopeName, StringComparison.OrdinalIgnoreCase));
        }

        using var createMapperRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"{keycloakBase}/admin/realms/{realm}/client-scopes/{scope.Id}/protocol-mappers/models")
        {
            Content = JsonContent.Create(new
            {
                name = "raveisland-web-audience",
                protocol = "openid-connect",
                protocolMapper = "oidc-audience-mapper",
                config = new Dictionary<string, string>
                {
                    ["included.client.audience"] = audienceClientId,
                    ["id.token.claim"] = "false",
                    ["access.token.claim"] = "true",
                },
            }),
        };
        createMapperRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createMapperResponse = await http.SendAsync(createMapperRequest);
        if (!createMapperResponse.IsSuccessStatusCode)
        {
            var body = await createMapperResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Failed to create audience mapper for '{scopeName}': {(int)createMapperResponse.StatusCode} {body}");
        }

        Console.WriteLine("Created audience client scope '{0}' for '{1}'.", scopeName, audienceClientId);
    }
}

static async Task EnsureClientDefaultScopesAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    string clientId,
    params string[] scopeNames)
{
    using var clientsRequest = new HttpRequestMessage(
        HttpMethod.Get,
        $"{keycloakBase}/admin/realms/{realm}/clients?clientId={Uri.EscapeDataString(clientId)}");
    clientsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var clients = await SendAsync<ClientRepresentation[]>(http, clientsRequest) ?? [];
    var client = clients.FirstOrDefault(c => string.Equals(c.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Client '{clientId}' was not found in realm '{realm}'.");

    using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{realm}/client-scopes");
    scopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var allScopes = await SendAsync<ClientScopeRepresentation[]>(http, scopesRequest) ?? [];
    var scopeIdsByName = allScopes
        .Where(s => !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Id))
        .ToDictionary(s => s.Name!, s => s.Id!, StringComparer.OrdinalIgnoreCase);

    using var clientScopesRequest = new HttpRequestMessage(
        HttpMethod.Get,
        $"{keycloakBase}/admin/realms/{realm}/clients/{client.Id}/default-client-scopes");
    clientScopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var clientScopes = await SendAsync<ClientScopeRepresentation[]>(http, clientScopesRequest) ?? [];
    var clientScopeNames = clientScopes
        .Select(s => s.Name)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var scopeName in scopeNames)
    {
        if (clientScopeNames.Contains(scopeName))
        {
            continue;
        }

        if (!scopeIdsByName.TryGetValue(scopeName, out var scopeId))
        {
            Console.WriteLine("Warning: client scope '{0}' was not found for client '{1}'.", scopeName, clientId);
            continue;
        }

        using var addRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{keycloakBase}/admin/realms/{realm}/clients/{client.Id}/default-client-scopes/{scopeId}");
        addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(addRequest);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Added default client scope '{0}' to client '{1}'.", scopeName, clientId);
            continue;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to add default client scope '{scopeName}' to client '{clientId}': {(int)response.StatusCode} {body}");
    }
}

static async Task EnsureDefaultClientScopesAsync(
    HttpClient http,
    string keycloakBase,
    string realm,
    string token,
    params string[] scopeNames)
{
    using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{realm}/client-scopes");
    scopesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var allScopes = await SendAsync<ClientScopeRepresentation[]>(http, scopesRequest) ?? [];
    var scopeIdsByName = allScopes
        .Where(s => !string.IsNullOrWhiteSpace(s.Name) && !string.IsNullOrWhiteSpace(s.Id))
        .ToDictionary(s => s.Name!, s => s.Id!, StringComparer.OrdinalIgnoreCase);

    using var defaultsRequest = new HttpRequestMessage(
        HttpMethod.Get,
        $"{keycloakBase}/admin/realms/{realm}/default-default-client-scopes");
    defaultsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var defaultScopes = await SendAsync<ClientScopeRepresentation[]>(http, defaultsRequest) ?? [];
    var defaultScopeNames = defaultScopes
        .Select(s => s.Name)
        .Where(name => !string.IsNullOrWhiteSpace(name))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var scopeName in scopeNames)
    {
        if (defaultScopeNames.Contains(scopeName))
        {
            continue;
        }

        if (!scopeIdsByName.TryGetValue(scopeName, out var scopeId))
        {
            Console.WriteLine("Warning: client scope '{0}' was not found in realm '{1}'.", scopeName, realm);
            continue;
        }

        using var addRequest = new HttpRequestMessage(
            HttpMethod.Put,
            $"{keycloakBase}/admin/realms/{realm}/default-default-client-scopes/{scopeId}");
        addRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await http.SendAsync(addRequest);
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("Added default client scope '{0}' to realm '{1}'.", scopeName, realm);
            continue;
        }

        var body = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException(
            $"Failed to add default client scope '{scopeName}': {(int)response.StatusCode} {body}");
    }
}

static async Task EnsureRealmRoleAsync(HttpClient http, string keycloakBase, string realm, string token, string userId, string roleName)
{
    using var roleRequest = new HttpRequestMessage(HttpMethod.Get, $"{keycloakBase}/admin/realms/{realm}/roles/{roleName}");
    roleRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var role = await SendAsync<RoleRepresentation>(http, roleRequest)
        ?? throw new InvalidOperationException($"Realm role '{roleName}' was not found.");

    using var mapRequest = new HttpRequestMessage(HttpMethod.Post, $"{keycloakBase}/admin/realms/{realm}/users/{userId}/role-mappings/realm")
    {
        Content = JsonContent.Create(new[] { role })
    };
    mapRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

    var response = await http.SendAsync(mapRequest);
    if (response.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.Created or HttpStatusCode.OK)
    {
        return;
    }

    var body = await response.Content.ReadAsStringAsync();
    if (response.StatusCode == HttpStatusCode.Conflict || body.Contains("exists", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    response.EnsureSuccessStatusCode();
}

static async Task<T?> SendAsync<T>(HttpClient http, HttpRequestMessage request)
{
    var response = await http.SendAsync(request);
    if (response.StatusCode == HttpStatusCode.NotFound)
    {
        return default;
    }

    response.EnsureSuccessStatusCode();
    return await response.Content.ReadFromJsonAsync<T>();
}

file sealed class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}

file sealed class UserRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }
}

file sealed class RoleRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

file sealed class ClientScopeRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

file sealed class ProtocolMapperRepresentation
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

file sealed class ClientRepresentation
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }
}
