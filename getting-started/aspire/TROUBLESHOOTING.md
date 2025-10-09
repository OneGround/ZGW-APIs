# Troubleshooting Guide

This guide helps you resolve common issues when running OneGround with .NET Aspire.

## Installation Issues

### .NET Aspire Workload Not Found

**Symptom:**
```
error: Unknown workload 'aspire'
```

**Solution:**
```bash
# Update workloads first
dotnet workload update

# Install Aspire
dotnet workload install aspire
```

### Docker Not Running

**Symptom:**
```
Cannot connect to the Docker daemon at unix:///var/run/docker.sock
```

**Solution:**
- Start Docker Desktop
- Ensure Docker is running: `docker ps`
- On Windows, make sure "Expose daemon on tcp://localhost:2375 without TLS" is enabled in Docker settings

## Startup Issues

### Port Already in Use

**Symptom:**
```
System.IO.IOException: Failed to bind to address http://127.0.0.1:5009
```

**Solution:**

**Windows:**
```powershell
# Find process using the port
netstat -ano | findstr :5009

# Kill the process (replace PID with actual process ID)
taskkill /PID <PID> /F
```

**macOS/Linux:**
```bash
# Find and kill process
lsof -ti:5009 | xargs kill -9
```

### HAProxy Won't Start

**Symptom:**
```
Error: Cannot open configuration file
```

**Solution:**
1. Ensure SSL certificate is generated:
   ```bash
   .\generate-cert.ps1  # Windows
   ./generate-cert.sh   # Linux/macOS
   ```

2. Check HAProxy configuration:
   ```bash
   docker run --rm -v "$(pwd)/haproxy:/usr/local/etc/haproxy" haproxy:2.9 haproxy -c -f /usr/local/etc/haproxy/haproxy.cfg
   ```

### PostgreSQL Fails to Start

**Symptom:**
```
Database 'oneground' does not exist
```

**Solution:**
```bash
# Remove existing volumes and restart
docker volume rm $(docker volume ls -q | grep postgres)

# Restart Aspire - it will recreate the database
dotnet run --project OneGround.Aspire.AppHost
```

## Runtime Issues

### Services Can't Connect to Each Other

**Symptom:**
```
HttpRequestException: No such host is known (autorisaties.oneground.local)
```

**Solution:**

1. **Check hosts file:**
   - Windows: `C:\Windows\System32\drivers\etc\hosts`
   - Linux/macOS: `/etc/hosts`
   
   Add:
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

2. **Verify HAProxy is running:**
   ```bash
   docker ps | grep haproxy
   ```

3. **Check HAProxy logs:**
   ```bash
   docker logs <haproxy-container-id>
   ```

### Database Connection Errors

**Symptom:**
```
Npgsql.NpgsqlException: Connection refused
```

**Solution:**

1. **Check PostgreSQL is running:**
   ```bash
   docker ps | grep postgres
   ```

2. **Verify connection string in Aspire Dashboard:**
   - Open [https://localhost:17238](https://localhost:17238)
   - Navigate to the service
   - Check "Environment" tab for connection strings

3. **Test connection manually:**
   ```bash
   docker exec -it <postgres-container> psql -U postgres -d oneground
   ```

### RabbitMQ Connection Issues

**Symptom:**
```
RabbitMQ.Client.Exceptions.BrokerUnreachableException
```

**Solution:**

1. **Check RabbitMQ status:**
   ```bash
   docker ps | grep rabbitmq
   ```

2. **Access RabbitMQ Management UI:**
   - Open http://localhost:15672 (guest/guest)
   - Verify queues and connections

3. **Check RabbitMQ logs:**
   ```bash
   docker logs <rabbitmq-container-id>
   ```

## Authentication Issues

### Keycloak Not Accessible

**Symptom:**
```
Unable to connect to http://keycloak.oneground.local:8080
```

**Solution:**

1. **Add to hosts file:**
   ```
   127.0.0.1 keycloak.oneground.local
   ```

2. **Check Keycloak container:**
   ```bash
   docker ps | grep keycloak
   docker logs <keycloak-container-id>
   ```

3. **Access directly via localhost:**
   - Try http://localhost:8080 instead

### Invalid Token Errors

**Symptom:**
```
401 Unauthorized: The token is invalid
```

**Solution:**

1. **Verify Keycloak configuration:**
   - Realm: OneGround
   - Client: oneground-000000000
   - Service account enabled

2. **Check token endpoint:**
   ```bash
   curl -X POST http://keycloak.oneground.local:8080/realms/OneGround/protocol/openid-connect/token \
     -d "client_id=oneground-000000000" \
     -d "client_secret=your-secret-change-me" \
     -d "grant_type=client_credentials"
   ```

3. **Verify environment variables:**
   - Check `appsettings.json` for correct client secret
   - Ensure all services have the same configuration

## Performance Issues

### Slow Startup

**Symptom:**
Services take several minutes to start

**Solution:**

1. **Use local images:**
   ```json
   // appsettings.json
   {
     "OneGround": {
       "ImageTag": "local"
     }
   }
   ```
   Pre-pull images:
   ```bash
   docker pull ghcr.io/oneground/zaken-api:latest
   docker pull ghcr.io/oneground/catalogi-api:latest
   # ... etc
   ```

2. **Reduce services:**
   Comment out unused services in `Program.cs`

3. **Use source mode:**
   ```bash
   dotnet run --launch-profile Development
   ```

### High Memory Usage

**Symptom:**
System running out of memory

**Solution:**

1. **Use source mode instead of containers:**
   - Reduces memory overhead significantly

2. **Increase Docker memory limit:**
   - Docker Desktop → Settings → Resources → Memory
   - Increase to at least 8GB

3. **Run fewer services:**
   - Only start the services you need

## SSL/TLS Issues

### Certificate Warnings in Browser

**Symptom:**
```
NET::ERR_CERT_AUTHORITY_INVALID
```

**Solution:**

This is expected with self-signed certificates. You can:

1. **Accept the warning:**
   - Click "Advanced" → "Proceed to site"

2. **Trust the certificate (Windows):**
   ```powershell
   $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2(".\haproxy\certs\oneground.local.pem")
   $store = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root","LocalMachine")
   $store.Open("ReadWrite")
   $store.Add($cert)
   $store.Close()
   ```

3. **Use HTTP instead:**
   - Access services via `http://` instead of `https://`

## .NET Aspire Dashboard Issues

### Dashboard Not Opening

**Symptom:**
```
Unable to connect to https://localhost:17238
```

**Solution:**

1. **Check if dashboard is running:**
   ```bash
   netstat -ano | findstr :17238  # Windows
   lsof -i :17238                 # macOS/Linux
   ```

2. **Try different browser:**
   - Some browsers have strict HTTPS policies

3. **Check firewall:**
   - Ensure port 17238 is not blocked

### No Services Showing in Dashboard

**Symptom:**
Dashboard is empty, no services listed

**Solution:**

1. **Wait for startup:**
   - Services may take time to appear

2. **Check logs in terminal:**
   - Look for errors in the console output

3. **Verify OTLP endpoints:**
   - Check launchSettings.json for correct endpoints

## Source Code Mode Issues

### Projects Not Found

**Symptom:**
```
Error: Projects.OneGround_ZGW_Zaken_WebApi could not be found
```

**Solution:**

1. **Build the solution first:**
   ```bash
   cd ../../../src
   dotnet build ZGW.all.sln
   ```

2. **Ensure project references are correct:**
   - Check paths in `OneGround.Aspire.AppHost.csproj`

### Compilation Errors

**Symptom:**
```
CS0234: The type or namespace name does not exist
```

**Solution:**

1. **Restore packages:**
   ```bash
   dotnet restore
   ```

2. **Clean and rebuild:**
   ```bash
   dotnet clean
   dotnet build
   ```

## Getting Help

If you're still having issues:

1. **Check the Aspire Dashboard:**
   - Logs tab for detailed error messages
   - Resources tab for service status

2. **Enable verbose logging:**
   ```json
   // appsettings.json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Debug"
       }
     }
   }
   ```

3. **Open an issue:**
   - Visit [GitHub Issues](https://github.com/OneGround/ZGW-APIs/issues)
   - Include logs, error messages, and configuration

4. **Check .NET Aspire documentation:**
   - [Official docs](https://learn.microsoft.com/dotnet/aspire/)
   - [GitHub discussions](https://github.com/dotnet/aspire/discussions)
