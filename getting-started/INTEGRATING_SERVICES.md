# Run OneGround ZGW APIs as Standalone services

## Running the Standalone Autorisaties API (AC)

This guide provides step-by-step instructions for running the OneGround Autorisaties API (AC) as a standalone service. This approach requires editing a single Docker Compose file to connect the service to your existing infrastructure.

### Prerequisites

Before you begin, ensure you have the following:

* **Docker** and **Docker Compose**.
* A running **PostgreSQL** instance.
* A running **RabbitMQ** instance.
* A running **Redis** instance.
* An **OpenID Connect (OIDC)** compliant Identity Provider.

---

### Setup Instructions

#### Step 1: Create a Project Directory

First, create a dedicated directory for the service to keep things organized.

```bash
mkdir oneground-ac && cd oneground-ac
```

#### Step 2: Download the Docker Compose File

Download the `docker-compose.autorisaties.yml` file into your new directory.

```bash
curl -o docker-compose.autorisaties.yml [https://raw.githubusercontent.com/OneGround/ZGW-APIs/main/getting-started/docker-compose.autorisaties.yml](https://raw.githubusercontent.com/OneGround/ZGW-APIs/main/getting-started/docker-compose.autorisaties.yml)
```

#### Step 3: Configure the Service

Open the `docker-compose.autorisaties.yml` file you just downloaded in a text editor.

You will need to edit two sections:

1. **Environment Variables**: In the `services.zgw.autorisaties.webapi.environment` section, replace all placeholder values (e.g., `<POSTGRES_HOST>`, `<RABBITMQ_HOST>`, etc.) with the actual connection details for your setup.

2. **Networking**: At the bottom of the file, in the `networks.external_network` section, change `name: bridge` to the name of the Docker network that your other services (Postgres, RabbitMQ, Redis) are connected to.

> **Note on Security**: This method places configuration, including secrets, directly into the `docker-compose.autorisaties.yml` file for simplicity. For production environments, we strongly recommend using a separate `.env` file or another secrets management tool to protect sensitive data.

#### Step 4: Start the Service

Once you have saved your changes to the `docker-compose.autorisaties.yml` file, you can start the service.

```bash
docker compose -f docker-compose.autorisaties.yml up -d
```

#### Step 5: Verify the Service

You can check the logs to ensure the service started correctly.

```bash
docker compose logs -f
```

The Autorisaties API should now be running and accessible on port `5009` (e.g., `http://localhost:5009`).

---

### Advanced Configuration

The environment variables provided in the `docker-compose.autorisaties.yml` file cover the mandatory dependencies only. You can explore the source repository for more settings in files like `appsettings.json`, `appsettings.Local.json`, and `appsettings.Shared.json`.

Any setting from these files can be configured as an environment variable. To do so, convert the JSON path to an environment variable by replacing the colon (`:`) with a double underscore (`__`). For example, to override `Logging:LogLevel:Default`, you would add an environment variable `Logging__LogLevel__Default=Warning` to the `docker-compose.autorisaties.yml` file.
