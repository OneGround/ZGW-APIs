# OneGround ZGW APIs - .NET Aspire Setup

## About This Guide

This guide provides instructions for running the OneGround ZGW APIs using **.NET Aspire**, Microsoft's opinionated stack for building observable, production-ready distributed applications.

.NET Aspire provides:
- **Service Discovery** - Automatic service-to-service communication
- **Observability** - Built-in telemetry, logging, and health checks
- **Configuration Management** - Centralized configuration across all services
- **Local Development** - Fast inner-loop development experience
- **Container Orchestration** - Simplified container management

## Prerequisites

Before you begin, ensure you have the following installed:

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [Visual Studio 2022 17.9+](https://visualstudio.microsoft.com/downloads/) or [Visual Studio Code](https://code.visualstudio.com/) with C# Dev Kit
- **.NET Aspire workload** - Install with:
  ```bash
  dotnet workload update
  dotnet workload install aspire
  ```

## Getting Started


### Quick Start: Docker Images Mode (default)

This is the fastest way to get started and evaluate OneGround APIs.

1. **Navigate to the Aspire directory:**
   ```powershell
   cd getting-started/aspire
   ```

2. **Run the Aspire AppHost:**
   ```powershell
   .\start.ps1
   # or
   dotnet run --project OneGround.Aspire.AppHost
   ```

3. **Access the Aspire Dashboard:**
   - The dashboard will automatically open at [https://localhost:17238](https://localhost:17238)
   - You can view all services, logs, traces, and metrics in real-time

4. **Access the APIs:**
   - All services are available at `https://*.oneground.local/`
   - See the [Service Endpoints](#service-endpoints) section below

---

### Quick Start: Source Code Mode (for development)

Run all OneGround services directly from source for debugging and code changes.

1. **Set the environment variable to enable source mode:**
   ```powershell
   $env:ONEGROUND_MODE = "source"
   .\start.ps1
   # or
   $env:ONEGROUND_MODE = "source"; dotnet run --project OneGround.Aspire.AppHost
   ```
   On Linux/macOS:
   ```bash
   export ONEGROUND_MODE=source
   ./start.sh
   # or
   export ONEGROUND_MODE=source && dotnet run --project OneGround.Aspire.AppHost
   ```

2. **All services will be launched from their source projects.**
   - Hot reload and debugging are available.
   - Any code changes will be reflected immediately.

**Note:** If you want to switch back to Docker image mode, unset the environment variable or set it to `image`.

---

## Service Endpoints

All services use the `.oneground.local` domain:

| Service | URL | Swagger | Health Check |
|---------|-----|---------|--------------|
| **Zaken API** | https://zaken.oneground.local | [/swagger](https://zaken.oneground.local/swagger) | [/health](https://zaken.oneground.local/health) |
| **Catalogi API** | https://catalogi.oneground.local | [/swagger](https://catalogi.oneground.local/swagger) | [/health](https://catalogi.oneground.local/health) |
| **Besluiten API** | https://besluiten.oneground.local | [/swagger](https://besluiten.oneground.local/swagger) | [/health](https://besluiten.oneground.local/health) |
| **Documenten API** | https://documenten.oneground.local | [/swagger](https://documenten.oneground.local/swagger) | [/health](https://documenten.oneground.local/health) |
| **Autorisaties API** | https://autorisaties.oneground.local | [/swagger](https://autorisaties.oneground.local/swagger) | [/health](https://autorisaties.oneground.local/health) |
| **Notificaties API** | https://notificaties.oneground.local | [/swagger](https://notificaties.oneground.local/swagger) | [/health](https://notificaties.oneground.local/health) |
| **Referentielijsten API** | https://referentielijsten.oneground.local | [/swagger](https://referentielijsten.oneground.local/swagger) | [/health](https://referentielijsten.oneground.local/health) |

## Configuration

### Environment Variables

The Aspire setup automatically configures all necessary environment variables. You can customize them in `OneGround.Aspire.AppHost/appsettings.json`:

```json
{
  "OneGround": {
    "ServiceAccountClientSecret": "your-secret-here",
    "UseDockerImages": true,
    "ImageTag": "latest"
  }
}
```

### Using Local Images

To use locally built Docker images instead of pulling from GitHub Container Registry:

1. Build the images locally:
   ```bash
   cd ../../
   docker build -f src/OneGround.ZGW.Zaken.WebApi/Dockerfile -t ghcr.io/oneground/zaken-api:local .
   # Repeat for other services...
   ```

2. Update `appsettings.json`:
   ```json
   {
     "OneGround": {
       "ImageTag": "local"
     }
   }
   ```

## Hosts File Configuration

To access services via `*.oneground.local` domains, add these entries to your hosts file:

**Windows:** `C:\Windows\System32\drivers\etc\hosts`  
**macOS/Linux:** `/etc/hosts`

```txt
127.0.0.1 zaken.oneground.local
127.0.0.1 catalogi.oneground.local
127.0.0.1 besluiten.oneground.local
127.0.0.1 documenten.oneground.local
127.0.0.1 autorisaties.oneground.local
127.0.0.1 notificaties.oneground.local
127.0.0.1 referentielijsten.oneground.local
127.0.0.1 keycloak.oneground.local
```

## Authentication

See [AUTHENTICATION.md](../../docs/AUTHENTICATION.md) for details on obtaining access tokens.

Quick start:
1. Access Keycloak at [http://keycloak.oneground.local:8080](http://keycloak.oneground.local:8080) (admin/admin)
2. Request a token using the service account credentials
3. Use the token in your API requests

## Features

### Observability

The Aspire dashboard provides:
- **Real-time logs** from all services
- **Distributed tracing** across service calls
- **Metrics and performance** monitoring
- **Health check status** for all services

### Service Discovery

Services automatically discover each other using Aspire's built-in service discovery. No manual configuration needed.

### Configuration Management

All configuration is centralized in the AppHost and automatically distributed to services.

### Development Experience

- **Fast startup** - Only build what you need
- **Hot reload** - Changes reflect immediately
- **Easy debugging** - Attach to any service from Visual Studio
- **Resource management** - Automatic cleanup on shutdown

## Troubleshooting

### Services won't start
- Ensure Docker Desktop is running
- Check that ports 5000-5020 and 8080 are not in use
- Verify .NET Aspire workload is installed: `dotnet workload list`

### Can't access *.oneground.local domains
- Verify hosts file entries are correct
- Clear your browser cache and DNS cache
- Try accessing via `http://localhost:[port]` directly

### Database connection errors
- Ensure PostgreSQL container is healthy in the Aspire dashboard
- Check database initialization logs
- Verify connection strings in the dashboard

### HAProxy certificate warnings
- The setup uses self-signed certificates for local development
- You can safely accept the certificate warnings in your browser
- For production, replace with proper certificates

## Advanced Configuration

### Custom PostgreSQL Connection

To use an external PostgreSQL instance, modify `Program.cs`:

```csharp
// Replace the AddPostgres line with:
var postgres = builder.AddConnectionString("postgres", "Host=myserver;Database=oneground;Username=user;Password=pass");
```

### Custom RabbitMQ Connection

```csharp
// Replace the AddRabbitMQ line with:
var rabbitmq = builder.AddConnectionString("rabbitmq", "amqp://user:pass@myserver:5672");
```

### Running Specific Services Only

Modify `Program.cs` to comment out services you don't need:

```csharp
// Comment out services you don't want to run
// AddZakenApi();
AddCatalogiApi();
AddAutorisatiesApi();
// ... only add what you need
```

## Production Deployment

While this Aspire setup is great for development, for production deployment consider:
- Using the Docker Compose setup in [getting-started/docker-compose](../docker-compose/)
- Or deploying to Azure Container Apps with Aspire deployment tools
- See [.NET Aspire deployment documentation](https://learn.microsoft.com/dotnet/aspire/deployment/overview)

## What's Different from Docker Compose?

| Feature | Docker Compose | .NET Aspire |
|---------|---------------|-------------|
| **Observability** | Manual setup | Built-in dashboard with logs, traces, metrics |
| **Service Discovery** | Environment variables | Automatic with service references |
| **Development** | Rebuild containers | Hot reload, direct debugging |
| **Configuration** | Multiple .env files | Centralized in AppHost |
| **Learning Curve** | YAML syntax | C# code (familiar to .NET devs) |

## License

This Aspire integration follows the same EUPL-1.2 license as the main OneGround project.

## Support

For issues or questions:
- Open an issue on [GitHub](https://github.com/OneGround/ZGW-APIs/issues)
- See the main [README](../../README.md) for general documentation
