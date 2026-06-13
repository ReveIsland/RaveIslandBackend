var builder = DistributedApplication.CreateBuilder(args);

var adminUserPassword = builder.AddParameter("admin-user-password", secret: true);
var keycloakAdminPassword = builder.AddParameter("keycloak-password", secret: true);
var smtpPassword = builder.AddParameter("smtp-password", secret: true);

var keycloak = builder.AddKeycloak("keycloak", 8080, adminPassword: keycloakAdminPassword)
    .WithDataVolume()
    .WithRealmImport("./Realms");

var maildev = builder.AddContainer("maildev", "maildev/maildev")
    .WithHttpEndpoint(port: 1080, targetPort: 1080, name: "ui")
    .WithEndpoint(port: 1025, targetPort: 1025, name: "smtp");

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
    .WaitFor(maildev)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", keycloakAdminPassword)
    .WithEnvironment("Smtp__Host", "localhost")
    .WithEnvironment("Smtp__Port", "1025")
    .WithEnvironment("Smtp__Username", "placeholder")
    .WithEnvironment("Smtp__Password", smtpPassword)
    .WithEnvironment("Smtp__FromAddress", "noreply@raveisland.local")
    .WithEnvironment("Smtp__FromName", "Rave Island")
    .WithEnvironment("Smtp__EnableSsl", "false")
    .WithEnvironment("App__WebBaseUrl", "http://localhost:5173")
    .WithHttpHealthCheck("/health");

builder.AddViteApp("web", "../RaveIsland.Web")
    .WithReference(apiService)
    .WaitFor(apiService)
    .WithEndpoint("http", e => e.Port = 5173)
    .WithEnvironment("PORT", "5173")
    .WithExternalHttpEndpoints();

builder.Build().Run();
