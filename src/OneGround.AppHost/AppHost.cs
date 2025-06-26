using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var postgresUserName = builder.AddParameter("PostgresUserName");
var postgresPassword = builder.AddParameter("PostgresPassword");
var postgres = builder.AddPostgres("oneground-postgres", postgresUserName, postgresPassword, 5432)
    .WithPgAdmin();

var acUserDb = postgres.AddDatabase("UserConnectionString", "ac_db");

var acAdminDb = postgres.AddDatabase("AdminConnectionString", "ac_db");

var rabbitMqUserName = builder.AddParameter("RabbitUserName");
var rabbitMqPassword = builder.AddParameter("RabbitPassword");
var rabbitMq = builder
    .AddRabbitMQ("oneground-rabbitmq", rabbitMqUserName, rabbitMqPassword, 5672)
    .WithManagementPlugin(15672)
    .WithEnvironment("RABBITMQ_DEFAULT_VHOST", "oneground")
    .WithEnvironment("HOSTNAME", "localhost");

// .WithDockerfile()
// .WithBuildArg("hostname", "localhost");

var autorisatiesApi = builder.AddProject<ZGW_Autorisaties_WebApi>("autorisaties-api").WithReference(rabbitMq).WithReference(acUserDb);

//.WithReference(acAdminDb);

builder.Build().Run();
