# Family Hubs Suite

A comprehensive compliance management platform built with .NET 10 and React, featuring task management, document generation, and Microsoft Entra External ID authentication.

## 📋 Table of Contents

- [Architecture](#architecture)
- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running Locally](#running-locally)
- [Testing](#testing)
- [Deployment](#deployment)
- [Additional Resources](#additional-resources)

## 🏗️ Architecture

This is a full-stack application consisting of:

- **API**: .NET 10 REST API with Clean Architecture (Domain, Application, Infrastructure, API layers)
- **Client**: React 18 + TypeScript SPA with Vite, Material-UI, and MSAL authentication
- **Database**: PostgreSQL 15
- **Authentication**: Microsoft Entra External ID (formerly Azure AD B2C)
- **Storage**: Azure Blob Storage
- **Hosting**: Azure Container Apps

## ✅ Prerequisites

Before you begin, ensure you have the following installed:

### Required
- **.NET 10 SDK**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Node.js 18+**: [Download](https://nodejs.org/)
- **PostgreSQL 15**: [Download](https://www.postgresql.org/download/) or use Docker
- **Git**: [Download](https://git-scm.com/downloads)

### Optional (for Docker setup)
- **Docker Desktop**: [Download](https://www.docker.com/products/docker-desktop)
- **Docker Compose**: Included with Docker Desktop

### Azure Services
- Azure subscription with access to:
  - Microsoft Entra External ID tenant
  - Azure Blob Storage account
  - Azure Container Apps (for deployment)

## 📁 Project Structure

```
family-hubs-suite/
├── api/                          # .NET 10 API
│   ├── src/
│   │   ├── Api/                  # API layer (controllers, middleware)
│   │   ├── Application/          # Application layer (commands, queries, DTOs)
│   │   ├── Domain/               # Domain layer (entities, value objects)
│   │   └── Infrastructure/       # Infrastructure layer (EF Core, repositories)
│   └── tests/
│       ├── Api.Tests.Integration/
│       ├── Tests.Common/
│       └── Tests.Data/
├── client/                       # React + TypeScript SPA
│   ├── public/
│   ├── src/
│   │   ├── components/
│   │   ├── views/
│   │   ├── services/
│   │   └── config.json
│   ├── package.json
│   └── vite.config.ts
├── database/                     # Database initialization scripts
└── README.md
```

## 🚀 Getting Started

### Option 1: Local Development (Without Docker)

#### 1. Clone the Repository

```bash
git clone https://github.com/dhsc-govuk-infrastructure/family-hubs-suite.git
cd family-hubs-suite
```

#### 2. Setup PostgreSQL Database

Create a PostgreSQL database:

```sql
CREATE DATABASE family_hubs_db;
CREATE USER family_hubs_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE family_hubs_db TO family_hubs_user;
```

#### 3. Configure API

Create `api/src/Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=family_hubs_db;Username=family_hubs_user;Password=your_password;"
  },
  "AzureAdB2C": {
    "Instance": "https://your-tenant.ciamlogin.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-api-client-id",
    "Domain": "your-tenant.onmicrosoft.com",
    "ClientSecret": "your-client-secret",
    "SignUpSignInPolicyId": ""
  },
  "AzureStorage": {
    "ConnectionString": "your-blob-storage-connection-string"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

#### 4. Run Database Migrations

```bash
cd api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
```

#### 5. Start the API

```bash
cd api/src/Api
dotnet run
```

The API will be available at `https://localhost:5001`

#### 6. Configure Client

Update `client/src/config.json`:

```json
{
  "api_url": "https://localhost:5001/",
  "ad_domain": "your-tenant",
  "ad_client_id": "your-client-app-id",
  "ad_scope": "https://your-tenant.onmicrosoft.com/your-api-client-id/API",
  "ad_redirect_uri": "/"
}
```

#### 7. Install Client Dependencies

```bash
cd client
npm install
```

#### 8. Start the Client

```bash
npm run dev
```

The client will be available at `http://localhost:3000`

### Option 2: Docker (Local Development)

#### 1. Build and Run API

```bash
cd api
docker build -t family-hubs-api -f src/Api/Dockerfile .
docker run -p 5001:8080 family-hubs-api
```

#### 2. Build and Run Client

```bash
cd client
docker build -t family-hubs-client .
docker run -p 3000:80 family-hubs-client
```

## ⚙️ Configuration

### API Configuration Files

- **appsettings.json**: Base configuration
- **appsettings.Development.json**: Development environment settings
- **appsettings.Production.json**: Production environment settings (use environment variables)

### Client Configuration

The client uses `config.json` for runtime configuration:

- `api_url`: Backend API URL
- `ad_domain`: Entra External ID tenant domain (without `.ciamlogin.com`)
- `ad_client_id`: Client application ID from Azure Portal
- `ad_scope`: API scope for authentication
- `ad_redirect_uri`: OAuth redirect URI

### Environment Variables (API)

Key environment variables for production:

```bash
ConnectionStrings__DefaultConnection=<postgres-connection-string>
AzureAdB2C__Instance=<entra-instance>
AzureAdB2C__TenantId=<tenant-id>
AzureAdB2C__ClientId=<client-id>
AzureAdB2C__ClientSecret=<client-secret>
AzureStorage__ConnectionString=<blob-storage-connection-string>
```

## 🧪 Testing

### API Tests

Run all integration tests:

```bash
cd api
dotnet test
```

Run specific test project:

```bash
dotnet test tests/Api.Tests.Integration
```

Run with coverage:

```bash
dotnet test /p:CollectCoverage=true /p:CoverageDirectory=./coverage
```

### Client Tests

```bash
cd client
npm test
```

## 🚢 Deployment

### Azure Container Apps

The application is deployed to Azure Container Apps. Docker images are built and pushed to Azure Container Registry.

#### Build and Push Images

```bash
# Login to Azure Container Registry
az acr login --name <your-acr-name>

# Build and push API
docker build -t <acr-name>.azurecr.io/family-hubs-api:latest -f api/src/Api/Dockerfile ./api
docker push <acr-name>.azurecr.io/family-hubs-api:latest

# Build and push Client
docker build -t <acr-name>.azurecr.io/family-hubs-client:latest ./client
docker push <acr-name>.azurecr.io/family-hubs-client:latest
```

#### Deploy to Container Apps

```bash
# Update API Container App
az containerapp update --name <api-app-name> --resource-group <rg-name> --image <acr-name>.azurecr.io/family-hubs-api:latest

# Update Client Container App
az containerapp update --name <client-app-name> --resource-group <rg-name> --image <acr-name>.azurecr.io/family-hubs-client:latest
```

### Required Azure Configuration

1. **App Registration** in Entra External ID:
   - Configure redirect URIs
   - Set supported account types to "Customers"
   - Add API scopes

2. **Azure Container Apps**:
   - Configure application settings/environment variables
   - Enable HTTPS only
   - Configure CORS if needed

3. **Azure Blob Storage**:
   - Create containers: `framework-files`, `task-files`, `question-files`, `assignee-task-files`
   - Configure CORS for client access

4. **PostgreSQL Database**:
   - Run migrations after deployment
   - Configure connection strings in App Settings

## 📚 Additional Resources

### Documentation

- [Clean Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures#clean-architecture) - Architecture pattern used
- [Microsoft Entra External ID](https://learn.microsoft.com/en-us/entra/external-id/) - Authentication setup

### Key Technologies

- [.NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10)
- [React 18](https://react.dev/)
- [Material-UI](https://mui.com/)
- [MSAL.js](https://github.com/AzureAD/microsoft-authentication-library-for-js)
- [MediatR](https://github.com/jbogard/MediatR) - CQRS implementation
- [FluentValidation](https://docs.fluentvalidation.net/)
- [Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)

### Common Issues

**Issue**: `AADSTS50011: Redirect URI mismatch`
- **Solution**: Add redirect URIs to app registration in Azure Portal

**Issue**: Database connection fails
- **Solution**: Check connection string, ensure PostgreSQL is running, verify firewall rules

**Issue**: CORS errors
- **Solution**: Configure CORS in API startup and Azure Blob Storage

**Issue**: Testcontainers fails in CI/CD
- **Solution**: Ensure Docker service is available and `DOCKER_HOST` is set

## 👥 Contact

- **Repository Owner**: DHSC
- **Repository**: https://github.com/dhsc-govuk-infrastructure/family-hubs-suite

## 📝 License

Proprietary - All rights reserved