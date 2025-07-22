# Autorisaties API installation guide

## Versioning

The complete list of available versions for the OneGround Autorisaties API is maintained on their [GitHub versions page](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api/versions).

## Prerequisites

Before running the Autorisaties API image, you must have the following services available:

- **PostgreSQL Database**: The API requires a PostgreSQL database for data storage.

- **RabbitMQ**: A RabbitMQ instance is needed to handle the event bus for asynchronous communication.

Ensure these services are running and you have the necessary credentials before proceeding with the installation.

## How to use this image

### Start an Autorisaties API instance via the Docker command

Replace the placeholders `<version>`, `<POSTGRES_...>`, and `<EVENTBUS_...>` with the actual values and run the command:

```bash
docker run -d \
  --name oneground-autorisaties-api \
  -e ConnectionStrings__UserConnectionString="Host=<POSTGRES_HOST>;Port=<POSTGRES_PORT>;Database=<POSTGRES_AC_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_USER_PASSWORD>" \
  -e ConnectionStrings__AdminConnectionString="Host=<POSTGRES_HOST>;Port=<POSTGRES_PORT>;Database=<POSTGRES_AC_DB>;Username=<POSTGRES_ADMIN>;Password=<POSTGRES_ADMIN_PASSWORD>" \
  -e Eventbus__HostName="<EVENTBUS_HOSTNAME>" \
  -e Eventbus__VirtualHost="<EVENTBUS_VIRTUALHOST>" \
  -e Eventbus__UserName="<EVENTBUS_USERNAME>" \
  -e Eventbus__Password="<EVENTBUS_PASSWORD>" \
  ghcr.io/oneground/zgw-apis/autorisaties-api:<version>
```

### Start an Autorisaties API instance via Docker Compose

Replace the placeholders `<version>`, `<POSTGRES_...>`, and `<EVENTBUS_...>` with the actual values add the service to your setup:

```bash
services:
    zgw.autorisaties.webapi:
    image: ghcr.io/oneground/zgw-apis/autorisaties-api:<version>
    environment:
      ConnectionStrings__UserConnectionString: "Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_AC_DB};Username=${POSTGRES_USER};Password=${POSTGRES_USER_PASSWORD}"
      ConnectionStrings__AdminConnectionString: "Host=${POSTGRES_HOST};Port=${POSTGRES_PORT};Database=${POSTGRES_AC_DB};Username=${POSTGRES_ADMIN};Password=${POSTGRES_ADMIN_PASSWORD}"
      Eventbus__HostName: "<EVENTBUS_HOSTNAME>"
      Eventbus__VirtualHost: "<EVENTBUS_VIRTUALHOST>"
      Eventbus__UserName: "<EVENTBUS_USERNAME>"
      Eventbus__Password: "<EVENTBUS_PASSWORD>"
```

### Environment Variables

The following environment variables are used to configure the Autorisaties API container:

### Database Connections

- `ConnectionStrings__UserConnectionString`: The connection string for the PostgreSQL database user. This user requires read and write permissions.

  Format: `Host=<POSTGRES_HOST>;Port=<POSTGRES_PORT>;Database=<POSTGRES_AC_DB>;Username=<POSTGRES_USER>;Password=<POSTGRES_USER_PASSWORD>`

- `ConnectionStrings__AdminConnectionString`: The connection string for the PostgreSQL database admin user. This user requires permissions to run database migrations.

  Format: `Host=<POSTGRES_HOST>;Port=<POSTGRES_PORT>;Database=<POSTGRES_AC_DB>;Username=<POSTGRES_ADMIN>;Password=<POSTGRES_ADMIN_PASSWORD>`

### Event Bus Configuration

These variables configure the connection to a RabbitMQ instance for event handling.

- `Eventbus__HostName`: The hostname or IP address of the RabbitMQ server.

- `Eventbus__VirtualHost`: The virtual host to use on the RabbitMQ server.

- `Eventbus__UserName`: The username for authenticating with the RabbitMQ server.

- `Eventbus__Password`: The password for the specified RabbitMQ user.

## Accessing the API Documentation (Swagger)

Once the container is running, you can access the interactive API documentation (Swagger UI) in your web browser. This interface allows you to view all available API endpoints and test them directly.

- URL: <http://localhost:5009/swagger/index.html>

Note: If you mapped the container's port 80 to a different host port (other than 5009), you will need to adjust the URL accordingly.

## Authorization

To Be Updated
