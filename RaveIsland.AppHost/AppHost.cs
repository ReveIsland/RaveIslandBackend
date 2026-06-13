var builder = DistributedApplication.CreateBuilder(args);

var adminUserPassword = builder.AddParameter("admin-user-password", secret: true);
var keycloakAdminPassword = builder.AddParameter("keycloak-password", secret: true);

var keycloak = builder.AddKeycloak("keycloak", 8080, adminPassword: keycloakAdminPassword)
    .WithDataVolume()
    .WithRealmImport("./Realms");

var redis = builder.AddRedis("cache");

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var raveDb = postgres.AddDatabase("raveisland");

var keycloakSetup = builder.AddProject<Projects.RaveIsland_KeycloakSetup>("keycloak-setup")
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithEnvironment("REALM", "raveisland")
    .WithEnvironment("ADMIN_USERNAME", "admin")
    .WithEnvironment("ADMIN_USER_PASSWORD", adminUserPassword)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword);

var apiService = builder.AddProject<Projects.RaveIsland_ApiService>("apiservice")
    .WithReference(keycloak)
    .WithReference(redis)
    .WithReference(raveDb)
    .WaitFor(keycloak)
    .WaitFor(keycloakSetup)
    .WaitFor(raveDb)
    .WithHttpHealthCheck("/health");

builder.AddViteApp("web", "../RaveIsland.Web")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEndpoint("http", e => e.Port = 5173)
    .WithEnvironment("PORT", "5173")
    .WithExternalHttpEndpoints();

builder.Build().Run();
