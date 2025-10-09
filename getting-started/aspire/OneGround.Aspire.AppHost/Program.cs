
using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Configuration
var serviceAccountSecret = builder.Configuration["OneGround:ServiceAccountClientSecret"] ?? "your-secret-change-me";
var mode = Environment.GetEnvironmentVariable("ONEGROUND_MODE") ?? "image"; // "image" or "source"

// Infrastructure Services
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var postgresDb = postgres.AddDatabase("oneground");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume()
    .WithManagementPlugin();

var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "23.0")
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KEYCLOAK_ADMIN", "admin")
    .WithEnvironment("KEYCLOAK_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_HTTP_RELATIVE_PATH", "/")
    .WithArgs("start-dev");

// HAProxy for *.oneground.local routing
var haproxy = builder.AddContainer("haproxy", "haproxy", "2.9")
    .WithBindMount("./haproxy", "/usr/local/etc/haproxy", isReadOnly: true)
    .WithHttpEndpoint(port: 80, targetPort: 80, name: "http")
    .WithHttpsEndpoint(port: 443, targetPort: 443, name: "https");

// Service URLs for inter-service communication
var autorisatiesUrl = "http://autorisaties.oneground.local/api/v1/";
var catalogiUrl = "http://catalogi.oneground.local/api/v1/";
var besluitenUrl = "http://besluiten.oneground.local/api/v1/";
var documentenUrl = "http://documenten.oneground.local/api/v1/";
var notificatiesUrl = "http://notificaties.oneground.local/api/v1/";
var zakenUrl = "http://zaken.oneground.local/api/v1/";



// Common environment methods for ContainerResource and ProjectResource
IResourceBuilder<ContainerResource> AddCommonEnvContainer(IResourceBuilder<ContainerResource> resource)
{
    return resource
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
        .WithEnvironment("Auth__Authority", "http://keycloak.oneground.local:8080/realms/OneGround/")
        .WithEnvironment("Auth__ValidIssuer", "http://keycloak.oneground.local:8080/realms/OneGround")
        .WithEnvironment("Auth__ValidAudience", "account")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__Rsin", "000000000")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__ClientId", "oneground-000000000")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__ClientSecret", serviceAccountSecret)
        .WithEnvironment("NotificatieService__Type", "Http");
}

IResourceBuilder<ProjectResource> AddCommonEnvProject(IResourceBuilder<ProjectResource> resource)
{
    return resource
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WithEnvironment("ASPNETCORE_URLS", "http://+:8080")
        .WithEnvironment("Auth__Authority", "http://keycloak.oneground.local:8080/realms/OneGround/")
        .WithEnvironment("Auth__ValidIssuer", "http://keycloak.oneground.local:8080/realms/OneGround")
        .WithEnvironment("Auth__ValidAudience", "account")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__Rsin", "000000000")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__ClientId", "oneground-000000000")
        .WithEnvironment("ZgwServiceAccounts__Credentials__0__ClientSecret", serviceAccountSecret)
        .WithEnvironment("NotificatieService__Type", "Http");
}

// Helper to choose between AddContainer and AddProject



IResourceBuilder<ContainerResource> AddApi(
    string name,
    string image,
    string imageTag,
    string projectPath,
    int port,
    string[] envs)
{
    if (mode == "source")
    {
        var project = builder.AddProject(name, projectPath)
            .WithHttpEndpoint(port: port, targetPort: 8080, name: "http");
        foreach (var env in envs)
            project = project.WithEnvironment(env.Split('=')[0], env.Split('=')[1]);
        // Cast to ContainerResource for uniform return type (Aspire will treat as resource)
        return (IResourceBuilder<ContainerResource>)(object)AddCommonEnvProject(project);
    }
    else
    {
        var container = builder.AddContainer(name, image, imageTag)
            .WithHttpEndpoint(port: port, targetPort: 8080, name: "http");
        foreach (var env in envs)
            container = container.WithEnvironment(env.Split('=')[0], env.Split('=')[1]);
        return AddCommonEnvContainer(container);
    }
}

// API definitions

var autorisatiesApi = AddApi(
    "autorisaties-api",
    "ghcr.io/oneground/autorisaties-api",
    "1.0",
    "../../src/OneGround.ZGW.Autorisaties.WebApi/OneGround.ZGW.Autorisaties.WebApi.csproj",
    5001,
    new[] { $"Services__NRC__Api={notificatiesUrl}" }
);


var catalogiApi = AddApi(
    "catalogi-api",
    "ghcr.io/oneground/catalogi-api",
    "1.3",
    "../../src/OneGround.ZGW.Catalogi.WebApi/OneGround.ZGW.Catalogi.WebApi.csproj",
    5011,
    new[] { $"Services__AC__Api={autorisatiesUrl}", $"Services__NRC__Api={notificatiesUrl}" }
);


var besluitenApi = AddApi(
    "besluiten-api",
    "ghcr.io/oneground/besluiten-api",
    "1.0",
    "../../src/OneGround.ZGW.Besluiten.WebApi/OneGround.ZGW.Besluiten.WebApi.csproj",
    5013,
    new[] { $"Services__AC__Api={autorisatiesUrl}", $"Services__ZTC__Api={catalogiUrl}", $"Services__ZRC__Api={zakenUrl}", $"Services__NRC__Api={notificatiesUrl}" }
);


var documentenApi = AddApi(
    "documenten-api",
    "ghcr.io/oneground/documenten-api",
    "1.5",
    "../../src/OneGround.ZGW.Documenten.WebApi/OneGround.ZGW.Documenten.WebApi.csproj",
    5007,
    new[] { $"Services__AC__Api={autorisatiesUrl}", $"Services__BRC__Api={besluitenUrl}", $"Services__ZTC__Api={catalogiUrl}", $"Services__NRC__Api={notificatiesUrl}", $"Services__ZRC__Api={zakenUrl}" }
);


var zakenApi = AddApi(
    "zaken-api",
    "ghcr.io/oneground/zaken-api",
    "1.5",
    "../../src/OneGround.ZGW.Zaken.WebApi/OneGround.ZGW.Zaken.WebApi.csproj",
    5009,
    new[] { $"Services__AC__Api={autorisatiesUrl}", $"Services__BRC__Api={besluitenUrl}", $"Services__ZTC__Api={catalogiUrl}", $"Services__NRC__Api={notificatiesUrl}", $"Services__DRC__Api={documentenUrl}" }
);


var notificatiesApi = AddApi(
    "notificaties-api",
    "ghcr.io/oneground/notificaties-api",
    "1.0",
    "../../src/OneGround.ZGW.Notificaties.WebApi/OneGround.ZGW.Notificaties.WebApi.csproj",
    5015,
    new[] { $"Services__AC__Api={autorisatiesUrl}" }
);


var referentielijstenApi = AddApi(
    "referentielijsten-api",
    "ghcr.io/oneground/referentielijsten-api",
    "1.0",
    "../../src/OneGround.ZGW.Referentielijsten.WebApi/OneGround.ZGW.Referentielijsten.WebApi.csproj",
    5017,
    new[] { $"Services__AC__Api={autorisatiesUrl}" }
);


var documentenListener = AddApi(
    "documenten-listener",
    "ghcr.io/oneground/documenten-listener",
    "1.5",
    "../../src/OneGround.ZGW.Documenten.Messaging.Listener/OneGround.ZGW.Documenten.Messaging.Listener.csproj",
    5099,
    Array.Empty<string>()
);


var notificatiesListener = AddApi(
    "notificaties-listener",
    "ghcr.io/oneground/notificaties-listener",
    "1.0",
    "../../src/OneGround.ZGW.Notificaties.Messaging.Listener/OneGround.ZGW.Notificaties.Messaging.Listener.csproj",
    5098,
    Array.Empty<string>()
);

builder.Build().Run();
