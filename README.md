# Family Hubs Suite

A secure, cloud-native compliance management platform for local authorities managing family hub data. Built with .NET 10 and React 18, hosted on Azure Container Apps with Microsoft Entra External ID (CIAM) authentication.

> **This README was rewritten from a full audit of the actual code** (not just the old aspirational draft) so that a new engineer can get the app running end-to-end, including Entra ID and Azure Blob Storage — the two pieces that were previously only "here are some keys" with no setup steps. If anything here stops matching the code, treat the code as the source of truth and update this file.

## Table of Contents

- [Architecture](#architecture)
- [⚠️ Security note before you start](#️-security-note-before-you-start)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Entra ID setup](#entra-id-setup)
- [Default admin user](#default-admin-user)
- [Azure Blob Storage setup](#azure-blob-storage-setup)
- [Running Locally](#running-locally)
- [Configuration Reference](#configuration-reference)
- [Testing](#testing)
- [Deployment](#deployment)
- [MFA](#mfa)
- [Troubleshooting](#troubleshooting)

---

## Architecture

A full-stack application consisting of:

- **API**: .NET 10 REST API — Clean Architecture (Domain, Application, Infrastructure, Api layers), MediatR / CQRS, FluentValidation, Hangfire (background jobs + `/background-jobs` dashboard), SignalR hub (`/hub`)
- **Client**: React 18 + TypeScript SPA — Vite, GOV.UK Design System, MSAL.js authentication, Application Insights Web SDK
- **Database**: PostgreSQL 15 — EF Core, Hangfire job store, MFA sessions
- **Authentication**: Microsoft Entra External ID (CIAM) — OIDC/OAuth2 via `Microsoft.Identity.Web`. **Two separate app registrations are required** — see [Entra ID setup](#entra-id-setup).
- **MFA**: custom-built TOTP + recovery codes (`api/src/Infrastructure/Mfa`), enforced by `MfaRequiredMiddleware` — **this is not Entra's built-in MFA/Conditional Access**, despite what an earlier draft of this README implied.
- **Identity management**: Microsoft Graph API (client-credentials flow, reusing the API app registration's own client secret) — used to create/enable/disable/delete Entra user accounts from the admin UI
- **Storage**: Azure Blob Storage — connection-string based (no Managed Identity, no emulator wired up for local dev). Used for submission file uploads, bulk-upload staging, and ASP.NET Data Protection key persistence. See [Azure Blob Storage setup](#azure-blob-storage-setup).
- **Monitoring**: Application Insights — API and browser telemetry (PII scrubbed server-side via a custom telemetry processor)
- **Hosting / CI-CD**: this repo currently has **two independent deployment lineages** that must not be confused — see [Deployment](#deployment).

For the full architecture diagram and detailed technical specification see [docs/Technical-Architecture-Spec.md](docs/Technical-Architecture-Spec.md) and [docs/architecture-c4-deployment.puml](docs/architecture-c4-deployment.puml).

---

## ⚠️ Security note before you start

An audit while writing this README found **live, working credentials committed to git history** (present since the repo's initial commit), not placeholders:

- `api/src/Api/appsettings.Development.json` — a real Entra client secret, a real Azure Storage account key (full connection string), and a real default-user password
- `client/src/config.json` — a real tenant ID, API app ID, and SPA app ID (lower sensitivity, but also not meant to be public)

Both files are tracked by git (`client/src/config.json` has a `.gitignore` entry, but it was already committed before that was added, so the entry has no effect). **Before or as part of this handover, rotate the client secret and storage account key in Azure/Entra, and decide whether to scrub git history** — editing the files going forward does not remove old values already in history. This README uses placeholders throughout; do not copy real values out of it into anything you share further.

---

## Prerequisites

### Required

- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 22+**: [Download](https://nodejs.org/) (matches `client/Dockerfile`'s `node:22-alpine`; Node 18 may still work but isn't what CI/Docker use)
- **PostgreSQL 15**: [Download](https://www.postgresql.org/download/) or use Docker
- **Git**: [Download](https://git-scm.com/downloads)

### Optional (for Docker setup)

- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop) (includes Docker Compose)

### Azure / Entra access you'll need

- Owner or Application Administrator access on the Entra External ID tenant, to create app registrations (see [Entra ID setup](#entra-id-setup))
- An Azure Storage account (or access to create one) if you want file-upload/bulk-upload features working locally — otherwise these can be left unconfigured and the rest of the app still runs
- Optional: an Application Insights resource, if you want telemetry locally

---

## Project Structure

```
family-hubs-suite/
├── api/                          # .NET 10 API
│   ├── src/
│   │   ├── Api/                  # Controllers, DTOs, Services, Middleware
│   │   ├── Application/          # Commands, Queries, Validators, Exceptions
│   │   ├── Domain/                # Entities, Value Objects, business logic
│   │   └── Infrastructure/       # EF Core, Repositories, Blob/Graph/Mfa/Hangfire setup
│   └── tests/
│       ├── Api.Tests.Integration/
│       ├── Tests.Common/
│       └── Tests.Data/
├── client/                       # React 18 + TypeScript SPA (Vite)
│   ├── public/
│   └── src/
│       ├── components/
│       │   ├── GovUKComponents/  # GOV.UK Design System wrappers
│       │   └── Global/           # Shared feature components
│       ├── views/                # Page components by role
│       ├── hooks/                # Custom React hooks
│       ├── services/             # API service layer
│       ├── types/                # TypeScript type definitions
│       ├── config.json           # Runtime config (tracked in git — see security note above)
│       ├── config.ts             # Loads config.json, overridable via VITE_* env vars
│       └── Routes.tsx
├── docs/                         # Architecture documentation and diagrams
├── database/
│   └── init.sql                  # Runs once when the Postgres Docker volume is first created
├── docker-compose.yml            # Local development with Docker (Traefik + Postgres + API + Client)
├── .env.example                  # Template for docker-compose's required variables — copy to .env
└── README.md
```

---

## Entra ID setup

**This app needs two separate app registrations in the same Entra External ID (CIAM) tenant.** The codebase's own naming for them (visible in exported manifests) is `familyhubs-adapp-api-mvp` (the API) and `familyhubs-adapp-hub-mvp` (the SPA/Hub). Whatever you name your own copies, keep the two roles distinct — the config keys below assume you know which app is which.

| | API app | Hub (SPA) app |
|---|---|---|
| Represents | The backend `api/` project | The React `client/` SPA |
| Platform type | *(none — Web/confidential client, used only for token validation + Graph)* | Single-page application |
| Exposes | A custom scope named `API` | — |
| Requests | — | The API app's `API` scope |
| Needs a client secret | Yes (used both for JWT validation config and as the Graph client-credentials identity) | No |
| Needs redirect URIs | No | Yes — your local/dev/test/prod SPA URLs |

### 1. Create the API app registration

1. Entra admin center → **Applications → App registrations → New registration**.
2. Name it (e.g. `familyhubs-adapp-api-<env>`).
3. Supported account types: **Accounts in this organizational directory only** (single tenant) — matches `signInAudience: AzureADMyOrg` in the existing manifests.
4. Leave Redirect URI blank for this app.
5. Register, then note the **Application (client) ID** and **Directory (tenant) ID** from the Overview page.

### 2. Expose the `API` scope

1. On the API app → **Expose an API**.
2. Set the **Application ID URI** if prompted (default `api://<client-id>` is fine, or match the existing convention `https://<tenant>.onmicrosoft.com/<client-id>` — the client's `config.json` in this repo uses the latter form).
3. **Add a scope**:
   - Scope name: `API`
   - Who can consent: Admins only
   - Admin consent display name / description: `API`
   - State: Enabled
4. Save. The full scope value you'll need later is `<Application ID URI>/API`, e.g. `https://familyhubsmvp.onmicrosoft.com/3ac3428e-.../API`.

### 3. Create a client secret on the API app

1. API app → **Certificates & secrets → New client secret**.
2. Copy the secret **value** immediately (shown once) — this is `AzureAd:ClientSecret` / `AD_CLIENT_SECRET`.
3. Note its expiry date and put a reminder in your calendar/rotation process — an expired secret breaks both authentication and all Graph-backed user-management features at once (they share this one secret, see below).

### 4. Grant Microsoft Graph application permissions to the API app

The API uses this same app registration's client secret to call Microsoft Graph directly (`ClientSecretCredential`, client-credentials flow, `https://graph.microsoft.com/.default`) to create/enable/disable/delete Entra user accounts (`api/src/Infrastructure/Graph/UsersService.cs`).

1. API app → **API permissions → Add a permission → Microsoft Graph → Application permissions**.
2. Add **`User.ReadWrite.All`** (needed for `Users.PostAsync`, `Users[id].PatchAsync`, `Users[id].DeleteAsync`).
3. Click **Grant admin consent for `<tenant>`** — application permissions don't work until this is done.

### 5. Create the Hub (SPA) app registration

1. **New registration** again, name it (e.g. `familyhubs-adapp-hub-<env>`).
2. Same account type as above (single tenant).
3. Platform: **Single-page application**. Add redirect URIs for every place the SPA runs, matching exactly what `ad_redirect_uri`/MSAL will use, e.g.:
   - `http://family-hubs-client.localtest.me/` (Docker Compose local)
   - `http://localhost:3000/` (non-Docker local)
   - your deployed environment URLs
4. Register, note its **Application (client) ID** — this is `AD_CLIENT_ID_CLIENT` / `client/src/config.json`'s `ad_client_id`. It does **not** need a client secret (it's a public client).

### 6. Grant the Hub app permission to call the API app's scope

1. Hub app → **API permissions → Add a permission → My APIs** (or "APIs my organization uses") → select the API app from step 1 → **Delegated permissions** → check `API`.
2. **Grant admin consent** here too.

### 7. Values you now have

| Value | Used for | Config key(s) |
|---|---|---|
| Tenant ID | Both apps | `AzureAd:TenantId`, `client/src/config.json` → `ad_tenant_id` |
| CIAM instance, `https://<tenant>.ciamlogin.com` | Both apps | `AzureAd:Instance`, `ad_authority` |
| Tenant domain, `<tenant>.onmicrosoft.com` | Both apps | `AzureAd:Domain`, `ad_domain_hint` |
| API app's Application (client) ID | API app auth + Graph | `AzureAd:ClientId`, `Graph:ApplicationId` |
| API app's client secret | API app auth + Graph | `AzureAd:ClientSecret` |
| API app's Application ID URI | Building the scope string | used to build `ad_scope` |
| Hub app's Application (client) ID | SPA/MSAL | `client/src/config.json` → `ad_client_id` |
| Full scope string, `<API app URI>/API` | SPA token requests | `ad_scope` |

---

## Default admin user

There's no self-service "become the first admin" flow — the very first admin account has to be created directly in Entra, then the API auto-seeds a matching row in the app's own database on startup (`ApplicationDbContextInitialiser.SeedAdminUser`, called from `Program.cs` on every boot — it's idempotent, it only inserts if no user with that email exists yet).

### 1. Create the user in Entra

1. Entra admin center → your tenant → **Users → New user → Create new user**.
2. Give them a display name and the email address you want to sign in with (this must be a *local account* identity in this CIAM tenant — the same shape the app itself creates via Graph, `SignInType: emailAddress`).
3. Set an initial password (or let Entra auto-generate one) and complete creation.

### 2. Copy their Object ID (this is the "subId")

1. Open the user you just created → **Overview** (or **Properties**) → copy the **Object ID** GUID.
2. This value is what the app calls `SubId` everywhere in code (`Domain/Users/SubId.cs`) — it's the value MSAL returns as `account.localAccountId` after login, and what `ApplicationUserClaimsTransformation` matches against the `Users` table on every request.

### 3. Configure the API with this user

Set, wherever you're configuring the API (`appsettings.Development.json` locally, or the `DEFAULT_ADMIN_EMAIL`/`DEFAULT_ADMIN_SUBID` variables in `.env` for Docker):

```json
"DefaultAdminUser": {
  "Email": "familyhubs-admin@<tenant>.onmicrosoft.com",
  "SubId": "<object-id-you-just-copied>"
}
```

### 4. Start (or restart) the API

On startup the API runs migrations, then `SeedAdminUser()` inserts a `Users` row with `Role = "admin"` for that email/SubId if one doesn't already exist. Sign in as that user in the client and you should land as an admin.

### If you ever need to promote a different/existing user manually

Prefer changing `DefaultAdminUser` config and restarting where possible — it's what the app itself relies on and keeps behaviour consistent. If you do need to reach into the database directly (e.g. fixing a broken local dev environment), the `Role` column is a plain string (`"admin"`, `"organisation admin"`, or `"delivery plan lead"` — see `UserRole` in `api/src/Domain/ValueObjects/UserRole.cs`), so promoting an **existing** user row is a straightforward update:

```sql
UPDATE "Users" SET "Role" = 'admin' WHERE "Email" = 'someone@example.onmicrosoft.com';
```

This only works for a user that already has a row (e.g. created earlier via the app's own admin-invite flow, or a previous `SeedAdminUser` run). Don't hand-write a full `INSERT` into `Users` — the table also has a `jsonb` `Name` column and several audit columns that are easy to get subtly wrong; let the app's own seeding/creation code populate new rows.

---

## Azure Blob Storage setup

1. Create a Storage Account (Standard, general-purpose v2 is fine for dev).
2. Copy an **account connection string** (Storage Account → **Access keys**) — this app is connection-string based, not Managed Identity, for blob access.
3. Create these containers (the app auto-creates most of them on first write via `CreateIfNotExistsAsync`, but pre-creating them avoids surprises, especially for the request-staging one which wasn't confirmed to auto-create):

| Container | Purpose |
|---|---|
| `data-protection-keys` | ASP.NET Core Data Protection key ring (`keys.xml`) — auto-created |
| `submission-files` | Persisted submission file attachments — auto-created |
| `bulk-upload-staging` | Staged submissions bulk-upload CSVs, pre-confirm |
| `services-bulk-upload-staging` | Staged services bulk-upload files, pre-confirm |
| `locations-bulk-upload-staging` | Staged locations bulk-upload files, pre-confirm |
| `request-staging` | Generic staged raw HTTP request bodies |

4. **Set the connection string in two places** — this is an easy thing to miss, the codebase reads it via two independent config keys that both need the same value:
   - `AzureBlob:ConnectionString` — used by the general-purpose blob service (`IBlobService`) for everything except Data Protection
   - `ConnectionStrings:BlobStorage` — used specifically for the Data Protection key ring (`ConfigurePersistenceServices.cs`)

If both are left blank, the app still runs — Data Protection falls back to a non-persisted local key ring, and blob-dependent features (file uploads, bulk-upload) will fail when used. That's fine for local dev if you don't need those features.

There is **no local storage emulator (Azurite) wired into `docker-compose.yml`** — either point at a real (ideally a dedicated dev/test) Azure Storage account, or leave the connection strings blank and accept that upload features won't work locally.

All uploads/downloads go through the API server-side (`OpenWriteStream`/`GetReadStream`) — the browser never talks to Blob Storage directly, so no storage-account CORS configuration is needed for the client to work.

---

## Running Locally

### Option 1: Without Docker

#### 1. Clone and set up PostgreSQL

```bash
git clone https://github.com/dhsc-govuk-infrastructure/family-hubs-suite.git
cd family-hubs-suite
```

```sql
CREATE DATABASE family_hubs_db;
CREATE USER family_hubs_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE family_hubs_db TO family_hubs_user;
```

#### 2. Configure the API

Create `api/src/Api/appsettings.Development.json` (this file already exists in the repo with real values checked in — see the [security note](#️-security-note-before-you-start); replace them with your own tenant's values rather than reusing what's there):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=family_hubs_db;Username=family_hubs_user;Password=your_password;",
    "BlobStorage": "<same value as AzureBlob:ConnectionString below, or omit to skip Data Protection persistence>"
  },
  "AzureAd": {
    "Instance": "https://<tenant>.ciamlogin.com",
    "TenantId": "<tenant-id>",
    "ClientId": "<api-app-client-id>",
    "Domain": "<tenant>.onmicrosoft.com",
    "ClientSecret": "<api-app-client-secret>"
  },
  "Graph": {
    "ApplicationId": "<api-app-client-id>",
    "ApplicationObjectId": "<api-app-object-id>",
    "DefaultUserPassword": "<temporary password used when creating new Entra users>",
    "UseDefaultPassword": true
  },
  "DefaultAdminUser": {
    "Email": "familyhubs-admin@<tenant>.onmicrosoft.com",
    "SubId": "<object-id-of-the-admin-user-you-created-in-entra>"
  },
  "AzureBlob": {
    "ConnectionString": "<storage account connection string, or omit to run without file uploads>"
  },
  "ApplicationInsights": {
    "ConnectionString": ""
  },
  "Mfa": {
    "SessionExpiryHours": 8,
    "BypassForTesting": true
  },
  "Cors": {
    "AllowedOrigins": ["http://localhost:3000"]
  },
  "Logging": {
    "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" }
  }
}
```

`Mfa:BypassForTesting: true` skips the custom TOTP challenge locally — never set this in a deployed environment.

#### 3. Run migrations and start the API

```bash
dotnet ef database update --project api/src/Infrastructure --startup-project api/src/Api
dotnet run --project api/src/Api/Api.csproj
```

The API listens on the URL printed by `dotnet run` (typically `https://localhost:5001` / `http://localhost:5000`).

#### 4. Configure the client

`client/src/config.json` already exists in the repo (also with real values checked in — replace with your own). Either edit it directly, or override any field with an environment variable prefixed `VITE_` (see [Configuration Reference](#configuration-reference)) without touching the tracked file — e.g. a `client/.env.local` (gitignored by the default Vite `.gitignore` pattern, though this repo doesn't currently have a template for it):

```json
{
  "api_url": "https://localhost:5001/",
  "ad_tenant_id": "<tenant-id>",
  "ad_authority": "https://<tenant>.ciamlogin.com/",
  "ad_client_id": "<hub-app-client-id>",
  "ad_scope": "https://<tenant>.onmicrosoft.com/<api-app-client-id>/API",
  "ad_redirect_uri": "/",
  "ad_domain_hint": "<tenant>.onmicrosoft.com",
  "accessibility_testing_enabled": "true",
  "app_insights_connection_string": ""
}
```

#### 5. Install and start the client

```bash
npm --prefix client install
npm --prefix client run dev
```

The client runs at `http://localhost:3000`. Make sure `http://localhost:3000/` is registered as a redirect URI on the Hub app registration (step 5 of [Entra ID setup](#entra-id-setup)).

### Option 2: Docker

```bash
cp .env.example .env
# edit .env with your Entra values from the setup above
docker-compose up -d
```

This starts, via Traefik:
- PostgreSQL on `localhost:5432`
- API at `http://family-hubs-api.localtest.me` (also published on `localhost:5000`)
- Client at `http://family-hubs-client.localtest.me` (also published on `localhost:3000`)
- Traefik dashboard at `http://localhost:8080`

Register both `http://family-hubs-api.localtest.me/` and `http://family-hubs-client.localtest.me/` as valid redirect URIs / CORS origins where relevant, alongside the plain `localhost` ones, if you use both entry points interchangeably.

`*.localtest.me` always resolves to `127.0.0.1` — no hosts-file changes needed.

---

## Configuration Reference

### API (`api/src/Api/appsettings*.json`)

| Section:Key | Required | Notes |
|---|---|---|
| `ConnectionStrings:DefaultConnection` | Yes | Postgres connection string |
| `ConnectionStrings:BlobStorage` | No | Separate from `AzureBlob:ConnectionString` below — only backs Data Protection key persistence |
| `Database:ManagedIdentityClientId` | No | Only used when Postgres itself is configured for Entra/Managed Identity auth (deployed environments) |
| `AzureAd:Instance` / `TenantId` / `ClientId` / `Domain` / `ClientSecret` | Yes | API app registration — see [Entra ID setup](#entra-id-setup) |
| `AzureAd:SignUpSignInPolicyId` | No | Left blank in this repo; only relevant if using Entra ID B2C-style user-flow policies, not the current CIAM setup |
| `Graph:ApplicationId` / `ApplicationObjectId` / `EnterpriseApplicationObjectId` | No | Informational only in the current code — not read by anything, safe to leave blank |
| `Graph:DefaultUserPassword` / `UseDefaultPassword` | Recommended for local dev | If `UseDefaultPassword` is true, new Entra users the app creates get this fixed password instead of a random emailed one |
| `DefaultAdminUser:Email` / `SubId` | Yes, once | See [Default admin user](#default-admin-user) |
| `AzureBlob:ConnectionString` | No | See [Azure Blob Storage setup](#azure-blob-storage-setup) |
| `ApplicationInsights:ConnectionString` | No | |
| `Hangfire:User` / `Password` | Recommended | Basic-auth credentials for the `/background-jobs` dashboard |
| `Smtp:*` | No | Outbound email (e.g. temporary-password emails when `Graph:UseDefaultPassword` is false) |
| `Mfa:SessionExpiryHours` / `BypassForTesting` | `BypassForTesting` for local dev only | See [MFA](#mfa) |
| `OsPlaces:ApiKey` | No | Ordnance Survey Places API — only needed for address lookup features |
| `Cors:AllowedOrigins` | Yes | Must include your client's origin or the SPA can't call the API |
| `Seeder:UseExportedData` / `FlushExistingData` | No | Controls whether form-question seed data comes from an exported JSON file or the code-based seeders |

### Client (`client/src/config.json`, overridable by `VITE_*` env vars)

| `config.json` key | `VITE_` override | Notes |
|---|---|---|
| `api_url` | `VITE_API_URL` | Backend API base URL |
| `ad_tenant_id` | `VITE_AD_TENANT_ID` | |
| `ad_authority` | `VITE_AD_AUTHORITY` | e.g. `https://<tenant>.ciamlogin.com/` |
| `ad_client_id` | `VITE_AD_CLIENT_ID` | **Hub app's** client ID, not the API app's |
| `ad_scope` | `VITE_AD_SCOPE` | Full scope string, `<API app ID URI>/API` |
| `ad_redirect_uri` | `VITE_AD_REDIRECT_URI` | Must exactly match a redirect URI registered on the Hub app |
| `ad_domain_hint` | `VITE_AD_DOMAIN_HINT` | Passed to `loginRedirect` to skip the tenant picker |
| `accessibility_testing_enabled` | — | No `VITE_` override exists for this one |
| `app_insights_connection_string` | `VITE_APP_INSIGHTS_CONNECTION_STRING` | |

Ignore the `VITE_B2C_*` build args declared in `client/Dockerfile` — they're dead config left over from an earlier Azure AD B2C naming scheme and aren't read anywhere in `client/src`. Use the `VITE_AD_*` names above.

---

## Testing

### API

```bash
dotnet test api/FamilyHubs.sln                   # all tests
dotnet test api/tests/Api.Tests.Integration       # integration only (needs Docker for Testcontainers)
```

### Client

```bash
npm --prefix client run build   # type-check + build
```

---

## Deployment

**This repo currently has two independent deployment setups tied to different branches — know which one you're looking at before changing anything CI-related:**

1. **`main` branch → GitHub Actions → Azure Container Apps** (`.github/workflows/build-deploy.yml`, `.github/workflows/buildanddeploy.yml`). Builds via `az acr build`, deploys via `az containerapp update`, authenticates with OIDC federated credentials (no stored secrets). Dev auto-deploys on push to `main`; tst/prd are manual (`workflow_dispatch`).
2. **`mvp` branch (Bitbucket) → Bitbucket Pipelines → Azure Web Apps** (`bitbucket-pipelines.yml`, present on `origin/mvp` only). Uses the `atlassian/azure-web-apps-deploy` pipe. Auto-deploys to the "MVP" environment on push to `mvp`; "UAT" is a manual follow-on stage.

Confirm with the team which of these is the currently-live production path before assuming either README section is authoritative for "prod" — they may be mid-migration between the two.

### Required Azure/Entra configuration (either path)

1. Both Entra app registrations from [Entra ID setup](#entra-id-setup), with redirect URIs added for each environment's actual URL.
2. Environment variables / secrets on whichever compute target is used, covering everything in [Configuration Reference](#configuration-reference).
3. `dotnet ef database update --project api/src/Infrastructure --startup-project api/src/Api` after any schema change.
4. The Blob Storage containers from [Azure Blob Storage setup](#azure-blob-storage-setup).

---

## MFA

MFA here is entirely custom (TOTP + recovery codes), **not** Entra's built-in MFA/Conditional Access — don't look for it in the Entra tenant config. It's enforced by `MfaRequiredMiddleware` after standard JWT auth on every request except a small set of `/mfa/*` endpoints. For local dev, set `Mfa:BypassForTesting: true` to skip it entirely; never set that in a deployed environment.

---

## Troubleshooting

**`AADSTS50011: Redirect URI mismatch`**
The redirect URI goes on the **Hub (SPA)** app registration, not the API app — check Authentication → Single-page application in the Entra portal.

**Sign-in succeeds but the user lands with no role / 403s everywhere**
Their `Users` table row doesn't exist or their `SubId` doesn't match their Entra Object ID. For the very first user, see [Default admin user](#default-admin-user).

**`MFA_SETUP_REQUIRED` (403) on every API call locally**
Set `Mfa:BypassForTesting: true` in your local config.

**File upload / bulk-upload features return errors, everything else works**
Check both `AzureBlob:ConnectionString` and `ConnectionStrings:BlobStorage` are set — see [Azure Blob Storage setup](#azure-blob-storage-setup).

**CORS errors calling the API from the client**
Add the client's origin to `Cors:AllowedOrigins` in the API config.

**Database connection fails locally**
Check `ConnectionStrings:DefaultConnection`, that PostgreSQL is running, and that the user has the right grants.

**Testcontainers fails in CI/locally**
Ensure Docker is running and reachable.

---

## Key Technologies

- [.NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [React 18](https://react.dev/)
- [GOV.UK Design System](https://design-system.service.gov.uk/)
- [MSAL.js](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- [Microsoft.Identity.Web](https://github.com/AzureAD/microsoft-identity-web)
- [MediatR](https://github.com/jbogard/MediatR)
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)
- [Hangfire](https://www.hangfire.io/)
- [Microsoft Entra External ID](https://learn.microsoft.com/en-us/entra/external-id/)
