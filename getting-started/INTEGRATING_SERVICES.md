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

#### Step 1: Download the AC standalone directory

Download the setup by following [GitHub link](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FAC) and unzip it.

#### Step 3: Configure the Service

1. **Environment Variables**: Edit `.env` file, replace all placeholder values (e.g., `<POSTGRES_HOST>`, `<RABBITMQ_HOST>`, etc.) with the actual connection details for your setup.

2. **Networking**: Edit `docker-compose.yml` file, in the `networks.external_network` section, change `name: bridge` to the name of the Docker network that your other services (Postgres, RabbitMQ, Redis) are connected to.

#### Step 4: Start the Service

Once you have saved your changes to the `docker-compose.yml` and `.env` files, you can start the service.

```bash
docker compose up -d
```

#### Step 5: Verify the Service

You can check the logs to ensure the service started correctly.

```bash
docker compose logs -f
```

The Autorisaties API should now be running and accessible on port `5009` (e.g., `http://localhost:5009`).

---

### Advanced Configuration

The environment variables provided in the `.env` file cover the mandatory dependencies only. You can explore the source repository for more settings in files like `appsettings.json`, `appsettings.Local.json`, and `appsettings.Shared.json`.

Any setting from these files can be configured as an environment variable. To do so, convert the JSON path to an environment variable by replacing the colon (`:`) with a double underscore (`__`). For example, to override `Logging:LogLevel:Default`, you would add an environment variable `Logging__LogLevel__Default=Warning` to the `.env` file.
