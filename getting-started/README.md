# Getting Started with OneGround ZGW APIs

This directory contains different ways to run and deploy OneGround ZGW APIs. Choose the approach that best fits your needs.

## Available Options

### 1. .NET Aspire (Recommended for Development)

**Best for:** Local development, debugging, and fast iteration

**Location:** [aspire/](aspire/)

**Features:**
- ✅ Built-in observability dashboard
- ✅ Hot reload for code changes
- ✅ Native .NET debugging
- ✅ Automatic service discovery
- ✅ Lower resource usage
- ✅ Type-safe configuration

**Quick Start:**
```bash
cd aspire
.\start.ps1              # Windows
./start.sh               # Linux/macOS
```

**Documentation:** [aspire/README.md](aspire/README.md)

---

### 2. Docker Compose (Recommended for Testing)

**Best for:** Integration testing, production-like environments

**Location:** [docker-compose/](docker-compose/)

**Features:**
- ✅ Production parity
- ✅ Full isolation
- ✅ Technology agnostic
- ✅ Industry standard
- ✅ CI/CD friendly

**Quick Start:**
```bash
cd docker-compose
docker-compose up -d
```

**Documentation:** Check docker-compose directory

---

### 3. Standalone (Individual Services)

**Best for:** Running specific services individually

**Location:** [standalone/](standalone/)

**Features:**
- ✅ Minimal dependencies
- ✅ Service-by-service control
- ✅ Flexible configuration

---

## Comparison

| Feature | .NET Aspire | Docker Compose | Standalone |
|---------|------------|----------------|------------|
| **Ease of Setup** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐ |
| **Development Speed** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Debugging** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐⭐⭐ |
| **Observability** | ⭐⭐⭐⭐⭐ | ⭐⭐ | ⭐⭐ |
| **Production Parity** | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ | ⭐⭐ |
| **Resource Usage** | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Prerequisites** | .NET SDK, Docker | Docker only | .NET SDK |

## Choosing the Right Approach

### Use .NET Aspire if you:
- Are actively developing OneGround services
- Need fast feedback loops with hot reload
- Want excellent debugging experience
- Need built-in observability and monitoring
- Are familiar with .NET ecosystem

### Use Docker Compose if you:
- Need production-like environment for testing
- Are doing integration testing
- Don't have .NET SDK installed
- Are setting up CI/CD pipelines
- Need simple service management with Docker

### Use Standalone if you:
- Only need specific services
- Want minimal overhead
- Are doing focused development on one service
- Have specific configuration requirements

## Can I Use Multiple Approaches?

**Yes!** These approaches are complementary:
- **Develop** with .NET Aspire (fast iteration)
- **Test** with Docker Compose (production parity)
- **Debug specific services** with Standalone

## Prerequisites

### Common Requirements
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### For .NET Aspire
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- .NET Aspire workload: `dotnet workload install aspire`

### For Standalone
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later

## Network Configuration

All approaches use `*.oneground.local` domains. Add these entries to your hosts file:

**Windows:** `C:\Windows\System32\drivers\etc\hosts`  
**macOS/Linux:** `/etc/hosts`

```
127.0.0.1 zaken.oneground.local
127.0.0.1 catalogi.oneground.local
127.0.0.1 besluiten.oneground.local
127.0.0.1 documenten.oneground.local
127.0.0.1 autorisaties.oneground.local
127.0.0.1 notificaties.oneground.local
127.0.0.1 referentielijsten.oneground.local
127.0.0.1 keycloak.oneground.local
```

## Service Endpoints

Once running, services are available at:

- **Zaken API**: https://zaken.oneground.local
- **Catalogi API**: https://catalogi.oneground.local
- **Besluiten API**: https://besluiten.oneground.local
- **Documenten API**: https://documenten.oneground.local
- **Autorisaties API**: https://autorisaties.oneground.local
- **Notificaties API**: https://notificaties.oneground.local
- **Referentielijsten API**: https://referentielijsten.oneground.local
- **Keycloak**: http://keycloak.oneground.local:8080

## Authentication

All approaches use Keycloak for authentication. See [../docs/AUTHENTICATION.md](../docs/AUTHENTICATION.md) for details.

## Troubleshooting

### .NET Aspire Issues
See [aspire/TROUBLESHOOTING.md](aspire/TROUBLESHOOTING.md)

### General Issues
- Ensure Docker is running
- Check that required ports are not in use
- Verify hosts file configuration
- Check firewall settings

## Documentation

- **Main README**: [../README.md](../README.md)
- **Authentication**: [../docs/AUTHENTICATION.md](../docs/AUTHENTICATION.md)
- **Logs**: [../docs/LOGS.md](../docs/LOGS.md)
- **Aspire vs Docker Compose**: [aspire/ASPIRE_VS_DOCKER_COMPOSE.md](aspire/ASPIRE_VS_DOCKER_COMPOSE.md)

## Support

For issues or questions:
- Open an issue on [GitHub](https://github.com/OneGround/ZGW-APIs/issues)
- Check the documentation in each subdirectory
- See troubleshooting guides

## License

EUPL-1.2 - See [../LICENSE](../LICENSE) for details
