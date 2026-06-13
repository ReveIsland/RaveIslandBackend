# Rave Island

Aspire distributed app with Keycloak authentication, Redis caching, a .NET API, and a Vite React frontend.

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (10.x)
- [Node.js](https://nodejs.org/) (20+)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for Keycloak, Redis, and PostgreSQL containers)
- [Aspire CLI](https://learn.microsoft.com/dotnet/aspire/cli/overview)

## First-time setup

1. Set the admin user password parameter (used for the `admin` user in the `raveisland` realm).

   Local dev uses the default in `RaveIsland.AppHost/appsettings.Development.json`. Override with:

   ```bash
   aspire config set Parameters:admin-user-password "YourSecurePassword123!" --secret
   ```

   If `aspire start` times out on first run, update the CLI to match the AppHost SDK (13.4.x):

   ```bash
   dotnet tool update -g aspire.cli
   aspire update --self --non-interactive --channel stable
   ```

2. Install frontend dependencies:

   ```bash
   cd RaveIsland.Web
   npm install
   ```

## Run the app

1. Start Docker Desktop.
2. From the repository root:

   ```bash
   aspire start
   ```

3. Open the Aspire dashboard URL from the CLI output, then browse:
   - **web** — React app at http://localhost:5173
   - **keycloak** — http://localhost:8080
   - **apiservice** — API (proxied from the web app at `/api`)

## Default sign-in

After startup, sign in with:

- **Username:** `admin`
- **Password:** value of `Parameters:admin-user-password`

The admin user has the `admin` role and can access `/admin` in the web app.

## Projects

| Project | Description |
|---------|-------------|
| `RaveIsland.AppHost` | Aspire orchestrator (Keycloak, Redis, PostgreSQL, API, Vite web) |
| `RaveIsland.ApiService` | Backend API with JWT auth, Redis cache, and PostgreSQL (EF Core) |
| `RaveIsland.KeycloakSetup` | One-shot job to create the realm admin user |
| `RaveIsland.Web` | Vite + React + OIDC frontend |
| `RaveIsland.ServiceDefaults` | Shared telemetry, health checks, service discovery |
