# .NET Aspire vs Docker Compose

This document explains the differences between the .NET Aspire setup and the traditional Docker Compose setup for OneGround ZGW APIs.

## Quick Comparison

| Aspect | Docker Compose | .NET Aspire |
|--------|---------------|-------------|
| **Best For** | Production-like environments, CI/CD | Local development, debugging |
| **Configuration** | YAML files | C# code |
| **Observability** | Manual setup (ELK, Grafana, etc.) | Built-in dashboard |
| **Service Discovery** | Environment variables | Automatic |
| **Hot Reload** | Requires container rebuild | Immediate (source mode) |
| **Debugging** | Attach to container | Native .NET debugging |
| **Learning Curve** | YAML syntax, Docker knowledge | C# knowledge |
| **Startup Time** | Slower (image pulling/building) | Faster (especially source mode) |
| **Resource Usage** | Higher (full containers) | Lower (shared runtime in source mode) |

## When to Use Docker Compose

Use the Docker Compose setup when you:

- **Need production parity**: Testing in an environment similar to production
- **Are doing integration testing**: Full end-to-end testing with all services
- **Don't have .NET SDK**: Just want to run the services without development tools
- **Are in CI/CD pipelines**: Automated testing and deployment
- **Want simple service management**: Docker is all you need

## When to Use .NET Aspire

Use the .NET Aspire setup when you:

- **Are developing**: Active code changes and debugging
- **Need fast feedback**: Hot reload and immediate changes
- **Want observability**: Real-time logs, traces, and metrics
- **Are a .NET developer**: Familiar with C# and .NET ecosystem
- **Need service orchestration**: Complex multi-service scenarios
- **Want guided debugging**: Step through code across services

## Detailed Comparison

### Configuration Management

#### Docker Compose
```yaml
# docker-compose.yml
services:
  zaken-api:
    image: ghcr.io/oneground/zaken-api:latest
    environment:
      - ConnectionStrings__DefaultConnection=${DB_CONNECTION}
      - Services__AC__Api=${AC_API_URL}
    ports:
      - "5009:8080"
```

**Pros:**
- Simple, declarative syntax
- Industry standard
- Works with any technology stack
- Easy to version control

**Cons:**
- Environment variables can be error-prone
- No type safety
- Difficult to share configuration across services
- Manual service discovery setup

#### .NET Aspire
```csharp
// Program.cs
var zakenApi = builder.AddContainer("zaken-api", "ghcr.io/oneground/zaken-api", "latest")
    .WithHttpEndpoint(port: 5009)
    .WithReference(postgresDb)
    .WithReference(autorisatiesApi);
```

**Pros:**
- Type-safe configuration
- IntelliSense support
- Automatic service discovery
- Reusable configuration methods
- Compile-time validation

**Cons:**
- Requires .NET knowledge
- Less portable across languages
- Newer, less mature ecosystem

### Observability

#### Docker Compose

With Docker Compose, you need to:
1. Set up logging drivers
2. Install and configure ELK stack or similar
3. Set up Prometheus/Grafana for metrics
4. Configure distributed tracing (Jaeger, Zipkin)
5. Manually correlate logs across services

**Effort:** High - requires significant setup

#### .NET Aspire

.NET Aspire provides out-of-the-box:
- **Unified Dashboard**: Single pane of glass for all services
- **Structured Logging**: Automatic log collection and formatting
- **Distributed Tracing**: OpenTelemetry integration
- **Metrics**: Real-time performance metrics
- **Health Checks**: Automatic health monitoring

**Effort:** None - works immediately

### Development Experience

#### Docker Compose

**Typical workflow:**
1. Make code changes
2. Rebuild Docker image
3. Stop and restart container
4. Wait for service to start
5. Test the change
6. Repeat

**Time per iteration:** 1-5 minutes (depending on image size)

#### .NET Aspire (Source Mode)

**Typical workflow:**
1. Make code changes
2. Hot reload automatically applies changes
3. Test immediately

**Time per iteration:** 1-10 seconds

### Debugging

#### Docker Compose

**Remote debugging:**
```bash
# Attach debugger to running container
docker attach <container-id>
```

**Challenges:**
- Need to expose debug ports
- Requires remote debugging setup
- Limited breakpoint support
- Difficult to debug multiple services simultaneously

#### .NET Aspire

**Native debugging:**
- Set breakpoints in Visual Studio
- Step through code naturally
- Debug multiple services simultaneously
- See all service interactions
- Full access to local variables and call stack

### Resource Usage

#### Docker Compose

Each service runs in its own container:
- **PostgreSQL**: ~50-100 MB
- **RabbitMQ**: ~150-200 MB
- **Keycloak**: ~500-700 MB
- **Each ZGW API**: ~100-150 MB
- **HAProxy**: ~20-30 MB

**Total:** ~2-3 GB RAM

#### .NET Aspire (Source Mode)

Services share the .NET runtime:
- **PostgreSQL container**: ~50-100 MB
- **RabbitMQ container**: ~150-200 MB
- **Keycloak container**: ~500-700 MB
- **All ZGW APIs**: ~300-500 MB (shared runtime)
- **HAProxy container**: ~20-30 MB

**Total:** ~1.5-2 GB RAM

### Service Discovery

#### Docker Compose

**Manual configuration:**
```yaml
environment:
  - Services__AC__Api=http://autorisaties-api:8080/api/v1/
  - Services__ZTC__Api=http://catalogi-api:8080/api/v1/
```

- Manual URL construction
- Must know service names
- Hard to refactor
- Error-prone

#### .NET Aspire

**Automatic discovery:**
```csharp
var zakenApi = builder.AddProject<Projects.ZakenApi>("zaken-api")
    .WithReference(autorisatiesApi)  // Automatic URL resolution
    .WithReference(catalogiApi);      // Type-safe references
```

- Automatic URL construction
- Type-safe references
- Compile-time validation
- Easy refactoring

## Migration Path

You don't have to choose one exclusively! Here's a recommended approach:

1. **Local Development**: Use .NET Aspire
   - Fast iteration
   - Easy debugging
   - Great observability

2. **Integration Testing**: Use Docker Compose
   - Production-like environment
   - Full isolation
   - Realistic networking

3. **Production**: Use Kubernetes/Docker Swarm/Cloud Services
   - Built from same Docker images
   - Production-grade orchestration
   - Scalability and resilience

## Feature Matrix

| Feature | Docker Compose | .NET Aspire |
|---------|----------------|-------------|
| **Multi-language support** | ✅ Excellent | ⚠️ .NET focused |
| **Production deployment** | ✅ Yes | ⚠️ Azure-focused |
| **Local development** | ⚠️ Slower | ✅ Excellent |
| **Hot reload** | ❌ No | ✅ Yes |
| **Native debugging** | ❌ Limited | ✅ Excellent |
| **Observability** | ⚠️ Manual setup | ✅ Built-in |
| **Service discovery** | ⚠️ Manual | ✅ Automatic |
| **Configuration** | ⚠️ YAML | ✅ C# (type-safe) |
| **Resource usage** | ⚠️ Higher | ✅ Lower (source mode) |
| **Learning curve** | ⚠️ Docker + YAML | ⚠️ .NET + Aspire |
| **Maturity** | ✅ Very mature | ⚠️ New (2024) |
| **Community** | ✅ Huge | ⚠️ Growing |
| **Cloud-agnostic** | ✅ Yes | ⚠️ Azure-optimized |

## Conclusion

**Use Docker Compose when:**
- You need production parity
- You're working in polyglot environments
- You're setting up CI/CD pipelines
- You don't have .NET SDK installed

**Use .NET Aspire when:**
- You're actively developing .NET services
- You need fast feedback loops
- You want excellent debugging experience
- You need built-in observability
- You're deploying to Azure

**Use both!** They complement each other:
- Develop with Aspire
- Test with Docker Compose
- Deploy with Kubernetes or Azure Container Apps
