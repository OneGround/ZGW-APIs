# OneGround Besluiten API (BRC) - Standalone Deployment

This repository contains a standalone Docker Compose setup for the **OneGround Besluiten API (BRC)**. This guide provides all the necessary steps to get the API running and connected to your own existing infrastructure.

## Table of Contents

- [OneGround Besluiten API (BRC) - Standalone Deployment](#oneground-besluiten-api-brc---standalone-deployment)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Getting Started](#getting-started)
    - [Step 1: Obtain the Setup Files](#step-1-obtain-the-setup-files)
    - [Step 2: Understand the Files](#step-2-understand-the-files)
  - [Choosing an Image Version](#choosing-an-image-version)
  - [Configuration](#configuration)
    - [Environment Variables (`.env`)](#environment-variables-env)
    - [Docker Networking (`docker-compose.yml`)](#docker-networking-docker-composeyml)
  - [Running the Service](#running-the-service)
  - [Advanced Configuration](#advanced-configuration)
  - [Contributing](#contributing)
  - [License](#license)

---

## Prerequisites

Before you begin, ensure you have the following components installed, running, and accessible from your Docker environment:

- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/install/)
- A running **PostgreSQL** instance
- A running **RabbitMQ** instance
- A running **Redis** instance
- A running **Autorisaties API (AC)** instance
- A running **Notificaties API (NRC)** instance
- An **OpenID Connect (OIDC)** compliant Identity Provider

Your existing services should be accessible over a shared Docker network for the BRC container to connect to them.

---

## Getting Started

### Step 1: Obtain the Setup Files

Download the necessary configuration files as a ZIP archive and extract them to your local machine.

[**Download the `standalone/BRC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FBRC)

After extracting, navigate into the created directory.

### Step 2: Understand the Files

You will be working with two primary files for this setup:

- `.env`: Contains all the configuration variables and secrets for connecting to your infrastructure.
- `docker-compose.yml`: Defines the AC service, maps the port, and handles network configuration.

---

## Choosing an Image Version

The `docker-compose.yml` file is configured to use a specific version of the `besluiten-api` image (e.g., `1.0`). You can browse all available versions and select a different one if needed.

A complete list of published image versions can be found on the [**GitHub Packages page**](https://github.com/OneGround/ZGW-APIs/pkgs/container/besluiten-api).

To use a different version, simply update the `image` tag in your `docker-compose.yml` file.

---

## Configuration

You must configure the service to connect to your existing infrastructure before running it.

### Environment Variables (`.env`)

Open the `.env` file in a text editor. Replace all placeholder values (e.g., `<POSTGRES_HOST>`) with your actual connection details.

| Variable | Description | Example |
| :--- | :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. | `Host=postgres-db;Port=5432;...` |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. | `Host=postgres-db;Port=5432;...` |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. | `rabbitmq-server` |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. | `/` |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. | `guest` |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. | `guest` |
| `Redis__ConnectionString` | The connection string for your Redis instance. | `localhost:6379` |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. | `https://auth.example.com` |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. | `https://auth.example.com` |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. | `besluiten-api` |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). | `http://autorisaties-api:5000` |
| `Services__NRC__Api` | The base URL of the Notificaties API (NRC). | `http://notificaties-api:5000` |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. | `000000000` |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. | `brc-service-account` |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. | `a-very-secure-secret` |

### Docker Networking (`docker-compose.yml`)

The BRC service must attach to the same Docker network as your other services to communicate with them.

1. Open the `docker-compose.yml` file.
2. Locate the `networks.external_network` section at the bottom.
3. Change `name: bridge` to the name of your existing external network.

```yaml
# docker-compose.yml

networks:
  external_network:
    # IMPORTANT: Change 'bridge' to the name of the Docker network
    # where your Postgres, RabbitMQ, Redis, AC, and NRC services are running.
    name: my-shared-network # <-- CHANGE THIS
    external: true
```

---

## Running the Service

Once you have saved your configurations, start the service in detached mode from your terminal:

```bash
docker compose up -d
```

To verify that the service has started correctly and to follow its log output, use the following command:

```bash
docker compose logs -f
```

If the startup is successful, the Besluiten API will be running and accessible on port **5013** of your Docker host (e.g., `http://localhost:5013`). You can change this port mapping in the `docker-compose.yml` file if needed.

---

## Advanced Configuration

The provided `.env` file covers only the mandatory dependencies. The API supports many other configuration settings, which can be found by exploring the `appsettings.json` files in the source repository.

Any setting from those files can be overridden using an environment variable. To do so, convert the JSON path of the setting to an environment variable name by replacing the colon (`:`) with a double underscore (`__`).

For example, to change the default log level, add the following line to your `.env` file:

```dotenv
# Example: Overriding a nested setting from appsettings.json
Logging__LogLevel__Default=Warning
```

---

## Contributing

We welcome contributions to improve this project! To get started, please read our [contributing guidelines](https://github.com/OneGround/ZGW-APIs/blob/main/CONTRIBUTING.md).

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for details.
