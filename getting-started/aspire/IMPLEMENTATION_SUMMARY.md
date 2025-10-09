# .NET Aspire Implementation Summary

This document provides an overview of the .NET Aspire integration that has been implemented for OneGround ZGW APIs.

## What Was Implemented

### 1. Project Structure

```
getting-started/aspire/
├── OneGround.Aspire.AppHost/          # Main Aspire host project
│   ├── Program.cs                     # Service orchestration
│   ├── appsettings.json              # Configuration
│   ├── OneGround.Aspire.AppHost.csproj
│   ├── Properties/
│   │   └── launchSettings.json       # Launch profiles
│   ├── haproxy/
│   │   ├── haproxy.cfg               # HAProxy configuration
│   │   └── certs/                    # SSL certificates
│   ├── generate-cert.ps1             # Certificate setup (Windows)
│   └── generate-cert.sh              # Certificate setup (Linux/macOS)
├── OneGround.Aspire.sln              # Solution file
├── start.ps1                         # Quick start script (Windows)
├── start.sh                          # Quick start script (Linux/macOS)
├── README.md                         # Main documentation
├── ASPIRE_VS_DOCKER_COMPOSE.md      # Comparison guide
└── TROUBLESHOOTING.md                # Troubleshooting guide
```

### 2. Key Features

#### Service Orchestration
- **Docker Image Mode**: Run pre-built OneGround images from container registry
- **Source Code Mode**: Build and run directly from source code for active development
- **Flexible Configuration**: Switch between modes using environment variables

#### Infrastructure Services
- **PostgreSQL**: Database with PgAdmin for management
- **RabbitMQ**: Message broker with management UI
- **Keycloak**: Authentication and authorization
- **HAProxy**: Reverse proxy for `*.oneground.local` domain routing

#### OneGround Services
All ZGW APIs are configured:
- Autorisaties API (port 5001)
- Catalogi API (port 5011)
- Besluiten API (port 5013)
- Documenten API (port 5007)
- Zaken API (port 5009)
- Notificaties API (port 5015)
- Referentielijsten API (port 5017)
- Documenten Listener (port 5099)
- Notificaties Listener (port 5098)

#### Observability
- **Aspire Dashboard**: Real-time monitoring at https://localhost:17238
- **Distributed Logging**: Centralized logs from all services
- **Distributed Tracing**: OpenTelemetry integration
- **Metrics**: Performance and health monitoring
- **Health Checks**: Automatic service health monitoring

### 3. Network Setup

All services are accessible via `*.oneground.local` domains:
- `https://zaken.oneground.local`
- `https://catalogi.oneground.local`
- `https://besluiten.oneground.local`
- `https://documenten.oneground.local`
- `https://autorisaties.oneground.local`
- `https://notificaties.oneground.local`
- `https://referentielijsten.oneground.local`
- `http://keycloak.oneground.local:8080`

HAProxy handles routing and SSL termination using certificates from the docker-compose setup.

### 4. Configuration Management

#### appsettings.json
```json
{
  "OneGround": {
    "UseDockerImages": true,        // true = images, false = source
    "ImageTag": "latest",            // Docker image tag
    "ServiceAccountClientSecret": "your-secret-change-me"
  }
}
```

#### Launch Profiles
- **https**: Default profile - runs with Docker images
- **Development**: Source code mode - builds and runs from source

### 5. Service Discovery

Automatic service-to-service communication using Aspire's built-in service discovery:
- Type-safe service references
- Automatic URL resolution
- No manual URL configuration needed

### 6. SSL/TLS Configuration

- Reuses existing certificates from `localdev/oneground-certificates`
- Supports self-signed certificates for local development
- Scripts provided for certificate setup on all platforms

## How It Works

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Aspire Dashboard                         │
│              https://localhost:17238                        │
│     (Logs, Traces, Metrics, Health Checks)                 │
└─────────────────────────────────────────────────────────────┘
                           │
                           │ Observability
                           ▼
┌─────────────────────────────────────────────────────────────┐
│                      HAProxy                                │
│              *.oneground.local routing                      │
│               ports 80/443                                  │
└─────────────────────────────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
   ┌─────────┐      ┌─────────┐       ┌─────────┐
   │  Zaken  │      │Catalogi │  ...  │Besluiten│
   │   API   │      │   API   │       │   API   │
   │  :5009  │      │  :5011  │       │  :5013  │
   └─────────┘      └─────────┘       └─────────┘
        │                  │                  │
        └──────────────────┼──────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        ▼                  ▼                  ▼
   ┌──────────┐     ┌──────────┐      ┌──────────┐
   │PostgreSQL│     │ RabbitMQ │      │ Keycloak │
   │          │     │          │      │          │
   └──────────┘     └──────────┘      └──────────┘
```

### Service Communication Flow

1. **Client Request** → HAProxy (`*.oneground.local`)
2. **HAProxy** → Routes to appropriate service (Zaken, Catalogi, etc.)
3. **Service** → Communicates with other services via service discovery
4. **Services** → Access PostgreSQL, RabbitMQ as needed
5. **Services** → Authenticate via Keycloak
6. **All telemetry** → Aspire Dashboard for observability

## Usage Modes

### Mode 1: Docker Images (Production-like)

**Best for:**
- Quick evaluation of OneGround
- Testing with production-like images
- CI/CD pipelines

**How to run:**
```bash
cd getting-started/aspire
.\start.ps1                          # Windows
# or
./start.sh                           # Linux/macOS
```

**Or directly:**
```bash
dotnet run --project OneGround.Aspire.AppHost
```

### Mode 2: Source Code (Development)

**Best for:**
- Active development
- Debugging
- Making code changes

**How to run:**
```bash
cd getting-started/aspire
.\start.ps1 -FromSource             # Windows
# or
./start.sh --from-source            # Linux/macOS
```

**Or directly:**
```bash
dotnet run --project OneGround.Aspire.AppHost --launch-profile Development
```

## Integration with Existing Setup

### Reused Components

1. **SSL Certificates**: Uses existing certificates from `localdev/oneground-certificates`
2. **HAProxy Configuration**: Similar routing to docker-compose setup
3. **Service Configuration**: Same environment variables and settings
4. **Authentication**: Same Keycloak realm and configuration

### Differences from Docker Compose

| Aspect | Docker Compose | .NET Aspire |
|--------|---------------|-------------|
| **Configuration** | YAML files | C# code |
| **Observability** | Manual setup | Built-in dashboard |
| **Development** | Rebuild containers | Hot reload |
| **Service Discovery** | Manual URLs | Automatic |
| **Resource Usage** | Higher | Lower (source mode) |

See [ASPIRE_VS_DOCKER_COMPOSE.md](ASPIRE_VS_DOCKER_COMPOSE.md) for detailed comparison.

## Standalone Design

The Aspire setup is designed to be **completely standalone**:

✅ **Self-contained**: All files in `getting-started/aspire/`
✅ **Reusable**: Can be extracted to a separate repository
✅ **No modifications** to existing codebase required
✅ **Optional feature**: Doesn't interfere with docker-compose setup
✅ **Shared resources**: Uses existing certificates and configuration patterns

### To Extract to Separate Repository

1. Copy entire `getting-started/aspire/` directory
2. Update project references in `.csproj` to point to your OneGround source
3. Update certificate paths if needed
4. Ready to use!

## Prerequisites

- .NET 8.0 SDK or later
- Docker Desktop
- .NET Aspire workload (`dotnet workload install aspire`)
- Optional: Visual Studio 2022 17.9+ or VS Code with C# Dev Kit

## Quick Start

1. **Install prerequisites**
2. **Run the start script:**
   ```bash
   cd getting-started/aspire
   .\start.ps1              # Windows
   ./start.sh               # Linux/macOS
   ```
3. **Access the Aspire Dashboard** at https://localhost:17238
4. **Access the APIs** at `https://*.oneground.local`

## Documentation

- **[README.md](README.md)**: Main getting started guide
- **[ASPIRE_VS_DOCKER_COMPOSE.md](ASPIRE_VS_DOCKER_COMPOSE.md)**: Detailed comparison
- **[TROUBLESHOOTING.md](TROUBLESHOOTING.md)**: Common issues and solutions

## Benefits

### For Developers
- ⚡ **Fast iteration**: Hot reload in source mode
- 🔍 **Easy debugging**: Native .NET debugging
- 📊 **Great observability**: Built-in dashboard
- 🎯 **Type safety**: C# configuration with IntelliSense

### For Operations
- 🐳 **Container support**: Works with Docker images
- 🔄 **Production parity**: Same images as production
- 📈 **Monitoring**: Built-in telemetry and metrics
- 🚀 **Easy deployment**: Deploy to Azure Container Apps

### For Teams
- 📚 **Easy onboarding**: Simple start scripts
- 🛠️ **Flexible**: Choose Docker images or source code
- 🔗 **Service discovery**: Automatic inter-service communication
- 📝 **Well documented**: Comprehensive guides

## Future Enhancements

Possible improvements:
- Add database seeding automation
- Include test data generation
- Add Azure deployment configurations
- Create custom Aspire components for OneGround services
- Add performance testing integration
- Create VS Code extension for easier management

## Support

For issues or questions:
- Check [TROUBLESHOOTING.md](TROUBLESHOOTING.md)
- Open an issue on [GitHub](https://github.com/OneGround/ZGW-APIs/issues)
- See main [README](../../README.md) for general documentation

## License

This Aspire integration follows the same EUPL-1.2 license as the main OneGround project.
