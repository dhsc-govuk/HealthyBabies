# Best Start Family Hubs and Healthy Babies — Data Reporting Service

A data reporting and compliance management platform for local authorities managing family hub data under the Best Start Family Hubs and Healthy Babies programme. Built with .NET 10, React 18, and PostgreSQL 15.

## Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Testing](#testing)
- [Deployment](#deployment)
- [Contributing](#contributing)
- [Licence](#licence)

## Architecture

A full-stack application with:

- **API**: .NET 10 REST API using Clean Architecture (Domain, Application, Infrastructure, API layers)
- **Client**: React 18 + TypeScript SPA using Vite, GOV.UK Design System, and MSAL authentication
- **Database**: PostgreSQL 15
- **Authentication**: Microsoft Entra External ID
- **Storage**: Azure Blob Storage for file uploads
- **Hosting**: Azure Container Apps

### Architecture Diagrams

- [Context diagram](./docs/C4_Context.png)
- [Container diagram](./docs/C4_Container.png)
- [Deployment diagram](./docs/Deployment.png)

## Prerequisites

### Required

- **.NET 10 SDK** — [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+** — [Download](https://nodejs.org/)
- **PostgreSQL 15** — [Download](https://www.postgresql.org/download/) or use Docker
- **Git** — [Download](https://git-scm.com/downloads)

### Optional (Docker setup)

- **Docker Desktop** — [Download](https://www.docker.com/products/docker-desktop)

### Azure Services

- Microsoft Entra External ID tenant
- Azure Blob Storage account
- Azure Container Apps (for deployment)

## Project Structure

```
family-hubs-suite/
├── api/                          # .NET 10 API
│   ├── src/
│   │   ├── Api/                  # Controllers, middleware, app settings
│   │   ├── Application/          # Commands, queries, validators (CQRS via MediatR)
│   │   ├── Domain/               # Entities, value objects, business logic
│   │   └── Infrastructure/       # EF Core, repositories, external services
│   └── tests/
│       ├── Api.Tests.Integration/
│       ├── Tests.Common/
│       └── Tests.Data/
├── client/                       # React + TypeScript SPA
│   ├── public/
│   └── src/
│       ├── components/           # GOV.UK Design System components + shared components
│       ├── views/                # Page components by role
│       ├── services/             # API service layer
│       ├── hooks/                # Custom React hooks
│       └── config.json           # Runtime configuration (see Configuration)
├── docs/                         # Architecture diagrams
├── docker-compose.yml            # Local development with Docker
└── README.md
```

## Getting Started

### Option 1: Local Development (without Docker)

#### 1. Clone the repository

```bash
git clone https://github.com/dhsc-govuk/HealthyBabies.git
cd HealthyBabies
```

#### 2. Set up PostgreSQL

```sql
CREATE DATABASE family_hubs_db;
CREATE USER family_hubs_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE family_hubs_db TO family_hubs_user;
```

#### 3. Configure the API

Create `api/src/Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=family_hubs_db;Username=family_hubs_user;Password=your_password;"
  },
  "Graph": {
    "ApplicationId": "your-api-app-registration-id",
    "ApplicationObjectId": "your-api-app-object-id",
    "EnterpriseApplicationObjectId": "your-enterprise-app-object-id"
  },
  "AzureBlob": {
    "ConnectionString": "your-blob-storage-connection-string"
  },
  "Smtp": {
    "Host": "your-smtp-host",
    "Port": 587,
    "EnableSsl": true,
    "Username": "your-smtp-username",
    "Password": "your-smtp-password",
    "SenderEmail": "noreply@example.gov.uk",
    "SenderName": "Best Start Family Hubs",
    "ClientUrl": "https://localhost:3000/"
  }
}
```

#### 4. Run database migrations

```bash
dotnet ef database update --project api/src/Infrastructure/Infrastructure.csproj --startup-project api/src/Api/Api.csproj
```

#### 5. Start the API

```bash
dotnet run --project api/src/Api/Api.csproj
```

The API will be available at `https://localhost:5001`.

#### 6. Configure the client

Update `client/src/config.json`:

```json
{
  "api_url": "https://localhost:5001/",
  "ad_tenant_id": "your-tenant-id",
  "ad_authority": "https://your-tenant.ciamlogin.com/",
  "ad_client_id": "your-client-app-id",
  "ad_scope": "https://your-tenant.onmicrosoft.com/your-api-client-id/API",
  "ad_redirect_uri": "/",
  "ad_domain_hint": "your-tenant.onmicrosoft.com",
  "accessibility_testing_enabled": "false",
  "app_insights_connection_string": ""
}
```

#### 7. Install client dependencies

```bash
npm --prefix client install
```

#### 8. Start the client

```bash
npm --prefix client run dev
```

The client will be available at `http://localhost:3000`.

### Option 2: Docker (full stack)

```bash
docker-compose up -d
```

This starts:
- PostgreSQL on `localhost:5432`
- API at `http://family-hubs-api.localtest.me` (via Traefik)
- Client at `http://family-hubs-client.localtest.me` (via Traefik)
- Traefik dashboard at `http://localhost:8080`

## Configuration

### API settings

| File | Purpose |
|------|---------|
| `api/src/Api/appsettings.json` | Base configuration with empty/default values |
| `api/src/Api/appsettings.Development.json` | Local overrides — **never commit this file** |

Key settings in `appsettings.json`:

| Key | Description |
|-----|-------------|
| `ConnectionStrings.DefaultConnection` | PostgreSQL connection string |
| `Graph.ApplicationId` | Microsoft Graph app registration ID |
| `AzureBlob.ConnectionString` | Azure Blob Storage connection string |
| `Smtp.*` | SMTP relay settings for email notifications |
| `OsPlaces.ApiKey` | OS Places API key for address lookup |
| `Cors.AllowedOrigins` | CORS origins for the client |

### Client settings (`client/src/config.json`)

| Key | Description |
|-----|-------------|
| `api_url` | Backend API base URL |
| `ad_tenant_id` | Entra External ID tenant ID |
| `ad_authority` | Entra External ID authority URL |
| `ad_client_id` | Client app registration ID |
| `ad_scope` | API scope for authentication |
| `ad_redirect_uri` | OAuth redirect URI |
| `ad_domain_hint` | Tenant domain hint for sign-in |
| `app_insights_connection_string` | Azure Application Insights connection string |

At build time, Vite environment variables (`VITE_*`) take precedence over `config.json` values.

## Testing

### API integration tests

Integration tests use Testcontainers (Docker-based PostgreSQL) — Docker must be running.

```bash
dotnet test api/FamilyHubs.sln
```

Run only integration tests:

```bash
dotnet test api/tests/Api.Tests.Integration
```

### Client type checking and build

```bash
npm --prefix client run build
```

## Deployment

The application is designed to run on Azure Container Apps with images stored in Azure Container Registry.

### Build and push images

```bash
az acr login --name <your-acr-name>

# API
docker build -t <acr-name>.azurecr.io/family-hubs-api:latest -f api/src/Api/Dockerfile ./api
docker push <acr-name>.azurecr.io/family-hubs-api:latest

# Client
docker build -t <acr-name>.azurecr.io/family-hubs-client:latest ./client
docker push <acr-name>.azurecr.io/family-hubs-client:latest
```

### Required Azure configuration

1. **Entra External ID app registrations**
   - API app registration with scopes exposed
   - Client app registration with redirect URIs configured

2. **Azure Blob Storage containers**
   - `framework-files`, `task-files`, `question-files`, `assignee-task-files`

3. **PostgreSQL**
   - Run EF Core migrations after deployment

4. **Container Apps environment variables**
   - Set all secrets via Azure App Settings or Key Vault references — do not bake secrets into images

## Contributing

This repository is published as open source under the [Open Government Licence](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/). Contributions are welcome.

Before contributing please read the [GDS Way](https://gds-way.digital.cabinet-office.gov.uk/) and [DHSC open source guidance](https://www.gov.uk/government/publications/open-source-guidance).

## Licence

Unless stated otherwise, the codebase is released under the [MIT Licence](https://opensource.org/licenses/MIT). Documentation and other content is released under the [Open Government Licence v3.0](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/).

© Crown copyright 2024–2025 Department of Health and Social Care
