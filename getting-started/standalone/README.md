# OneGround ZGW APIs - Standalone Setup Guide

This guide provides all the necessary steps to run any of the OneGround ZGW APIs as standalone services using the provided Docker Compose setups.

## Table of Contents

- [OneGround ZGW APIs - Standalone Setup Guide](#oneground-zgw-apis---standalone-setup-guide)
  - [Table of Contents](#table-of-contents)
  - [Core Prerequisites](#core-prerequisites)
  - [Authentication](#authentication)
    - [Required JWT Claims](#required-jwt-claims)
  - [API-Specific Configuration](#api-specific-configuration)
    - [Autorisaties API (AC)](#autorisaties-api-ac)
      - [Required Autorisaties API (AC) Environment Variables (`.env`)](#required-autorisaties-api-ac-environment-variables-env)
    - [Besluiten API (BRC)](#besluiten-api-brc)
      - [Required Besluiten API (BRC) Environment Variables (`.env`)](#required-besluiten-api-brc-environment-variables-env)
    - [Catalogi API (ZTC)](#catalogi-api-ztc)
      - [Required Catalogi API (ZTC) Environment Variables (`.env`)](#required-catalogi-api-ztc-environment-variables-env)
    - [Documenten API (DRC)](#documenten-api-drc)
      - [Required Documenten API (DRC) Environment Variables (`.env`)](#required-documenten-api-drc-environment-variables-env)
    - [Notificaties API (NRC)](#notificaties-api-nrc)
      - [Required Notificaties API (NRC) Environment Variables (`.env`)](#required-notificaties-api-nrc-environment-variables-env)
    - [Referentielijsten API (RC)](#referentielijsten-api-rc)
    - [Zaken API (ZRC)](#zaken-api-zrc)
      - [Required Zaken API (ZRC) Environment Variables (`.env`)](#required-zaken-api-zrc-environment-variables-env)
  - [Configuring and Running the Service](#configuring-and-running-the-service)
    - [Step 1: Create and Configure `.env` File](#step-1-create-and-configure-env-file)
    - [Step 2: Configure `docker-compose.yml`](#step-2-configure-docker-composeyml)
    - [Step 3: Run the Service](#step-3-run-the-service)
  - [Advanced Configuration](#advanced-configuration)
  - [License](#license)

---

## Core Prerequisites

Before you begin, ensure you have the following components installed and running for most API setups:

- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/install/)
- A running **PostgreSQL** instance
- A running **RabbitMQ** instance
- A running **Redis** instance
- An **OAuth2** compliant Identity Provider

> **Note:** Not all APIs require every component. See the [API-Specific Configuration](#api-specific-configuration) section for the exact prerequisites for each service.

---

## Authentication

The ZGW APIs use JWT (JSON Web Tokens) for authentication. To successfully authenticate with the APIs, your identity provider must be configured to include specific claims in the access token.

### Required JWT Claims

The following claim is mandatory for the ZGW APIs:

- `rsin`: The identifier of the organization.

For an example of how to configure Keycloak to include the necessary claims, please refer to the [Keycloak Setup Documentation](https://github.com/OneGround/ZGW-APIs/tree/main/localdev/keycloak/KeycloakSetup#alternative-identity-providers).

---

## API-Specific Configuration

This section contains the unique prerequisites, environment variables, and default ports for each API.

---

### Autorisaties API (AC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- **Prerequisites:**
  - Core Prerequisites
- **Default Port:** `5009`

#### Required Autorisaties API (AC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=ac_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=ac_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"
```

---

### Besluiten API (BRC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/besluiten-api)
- **Additional Prerequisites:**
  - A running **Autorisaties API (AC)** instance
  - A running **Notificaties API (NRC)** instance
- **Default Port:** `5013`

#### Required Besluiten API (BRC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=brc_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=brc_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"

# --- ZGW Service Dependencies ---
Services__AC__Api="https://autorisaties.oneground.local/api/v1/"
Services__NRC__Api="https://notificaties.oneground.local/api/v1/"

# --- ZGW Service Account Credentials ---
ZgwServiceAccounts__Credentials__0__Rsin="000000000"
ZgwServiceAccounts__Credentials__0__ClientId="oneground-000000000"
ZgwServiceAccounts__Credentials__0__ClientSecret="<SERVICE_ACCOUNT_CLIENT_SECRET>"
```

---

### Catalogi API (ZTC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- **Additional Prerequisites:**
  - A running **Autorisaties API (AC)** instance
  - A running **Notificaties API (NRC)** instance
- **Default Port:** `5011`

#### Required Catalogi API (ZTC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=ztc_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=ztc_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"

# --- ZGW Service Dependencies ---
Services__AC__Api="https://autorisaties.oneground.local/api/v1/"
Services__NRC__Api="https://notificaties.oneground.local/api/v1/"

# --- ZGW Service Account Credentials ---
ZgwServiceAccounts__Credentials__0__Rsin="000000000"
ZgwServiceAccounts__Credentials__0__ClientId="oneground-000000000"
ZgwServiceAccounts__Credentials__0__ClientSecret="<SERVICE_ACCOUNT_CLIENT_SECRET>"
```

---

### Documenten API (DRC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)
- **Additional Prerequisites:**
  - A running **Autorisaties API (AC)** instance
  - A running **Besluiten API (BRC)** instance
  - A running **Catalogi API (ZTC)** instance
  - A running **Notificaties API (NRC)** instance
  - A running **Zaken API (ZRC)** instance
- **Default Port:** `5007`

#### Required Documenten API (DRC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=drc_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=drc_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"

# --- ZGW Service Dependencies ---
Services__AC__Api="https://autorisaties.oneground.local/api/v1/"
Services__BRC__Api="https://besluiten.oneground.local/api/v1/"
Services__ZTC__Api="https://catalogi.oneground.local/api/v1/"
Services__NRC__Api="https://notificaties.oneground.local/api/v1/"
Services__ZRC__Api="https://zaken.oneground.local/api/v1/"

# --- ZGW Service Account Credentials ---
ZgwServiceAccounts__Credentials__0__Rsin="000000000"
ZgwServiceAccounts__Credentials__0__ClientId="oneground-000000000"
ZgwServiceAccounts__Credentials__0__ClientSecret="<SERVICE_ACCOUNT_CLIENT_SECRET>"
```

---

### Notificaties API (NRC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)
- **Additional Prerequisites:**
  - A running **Autorisaties API (AC)** instance
- **Default Port:** `5015`

#### Required Notificaties API (NRC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=nrc_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=nrc_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"

# --- ZGW Service Dependencies ---
Services__AC__Api="https://autorisaties.oneground.local/api/v1/"

# --- ZGW Service Account Credentials ---
ZgwServiceAccounts__Credentials__0__Rsin="000000000"
ZgwServiceAccounts__Credentials__0__ClientId="oneground-000000000"
ZgwServiceAccounts__Credentials__0__ClientSecret="<SERVICE_ACCOUNT_CLIENT_SECRET>"
```

---

### Referentielijsten API (RC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/referentielijsten-api)
- **Prerequisites:**
  - Docker and Docker Compose only.
- **Default Port:** `5018`
- **Environment Variables:** None required for basic setup. An optional `.env` file can be used for advanced configuration.

---

### Zaken API (ZRC)

- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api)
- **Additional Prerequisites:**
  - A running **Autorisaties API (AC)** instance
  - A running **Besluiten API (BRC)** instance
  - A running **Catalogi API (ZTC)** instance
  - A running **Documenten API (DRC)** instance
  - A running **Notificaties API (NRC)** instance
- **Default Port:** `5005`

#### Required Zaken API (ZRC) Environment Variables (`.env`)

Copy the snippet below, paste it into your new `.env` file, and adjust the values to match your configuration.

```ini
# .env

# --- Database Connection Strings ---
ConnectionStrings__UserConnectionString="Host=postgres_docker_db;Port=5432;Database=zrc_db;Username=postgres;Password=postgres"
ConnectionStrings__AdminConnectionString="Host=postgres_docker_db;Port=5432;Database=zrc_db;Username=postgres;Password=postgres"

# --- RabbitMQ Event Bus Connection ---
Eventbus__HostName="rabbit_mq"
Eventbus__VirtualHost="oneground"
Eventbus__UserName="guest"
Eventbus__Password="guest"

# --- Redis Cache Connection ---
Redis__ConnectionString="redis:6379"

# --- Identity Provider (OAuth2) Settings ---
Auth__Authority="http://localhost:8080/realms/OneGround/"
Auth__ValidIssuer="http://localhost:8080/realms/OneGround"
Auth__ValidAudience="account"

# --- ZGW Service Dependencies ---
Services__AC__Api="https://autorisaties.oneground.local/api/v1/"
Services__BRC__Api="https://besluiten.oneground.local/api/v1/"
Services__DRC__Api="https://documenten.oneground.local/api/v1/"
Services__NRC__Api="https://notificaties.oneground.local/api/v1/"
Services__ZTC__Api="https://catalogi.oneground.local/api/v1/"

# --- ZGW Service Account Credentials ---
ZgwServiceAccounts__Credentials__0__Rsin="000000000"
ZgwServiceAccounts__Credentials__0__ClientId="oneground-000000000"
ZgwServiceAccounts__Credentials__0__ClientSecret="<SERVICE_ACCOUNT_CLIENT_SECRET>"
```

---

## Configuring and Running the Service

### Step 1: Create and Configure `.env` File

First, create a new file named `.env` in the same directory as the `docker-compose.yml` file. Follow the instructions for your chosen API in the [API-Specific Configuration](#api-specific-configuration) section to copy the required environment variables into this file. Adjust the values to match your setup.

### Step 2: Configure `docker-compose.yml`

The `docker-compose.yml` file defines how to run the API container. Below is a generic example explaining the key sections you may need to configure.

```yaml
# docker-compose.yml

# A unique name for the Docker Compose project.
name: oneground-<service-name>-api

services:
  # Defines the service for the specific API.
  oneground.<api-name>.webapi:
    # The Docker image to use. You can change the tag to use a different version.
    image: ghcr.io/oneground/<service-name>-api:<tag>
    restart: unless-stopped
    ports:
      # Maps a port on your host machine to the container's internal port.
      # You can change the host port (e.g., 5009) if it conflicts with another service.
      - "<host-port>:5000"
    env_file:
      # Loads environment variables from the .env file.
      - ".env"
    networks:
      # Connects the service to a Docker network.
      - external_network

networks:
  external_network:
    # For APIs that need to communicate with other services, they must be on the same Docker network.
    # IMPORTANT: Change 'existing-external-network-name' to the name of your existing external network.
    name: existing-external-network-name # <-- CHANGE THIS
    external: true
```

The most common change you will make is updating the `networks.external_network.name` to match the Docker network where your other services (like PostgreSQL, Redis, and other ZGW APIs) are running.

### Step 3: Run the Service

Once you have configured your `.env` and `docker-compose.yml` files, you can start the service in detached mode from your terminal:

```bash
docker compose up -d
```

To verify that the service has started correctly and to follow its log output, use the following command:

```bash
docker compose logs -f
```

If the startup is successful, the API will be running and accessible on its default port on your Docker host (e.g., `http://localhost:<DEFAULT_PORT>`).

---

## Advanced Configuration

The provided `.env` files cover only the mandatory dependencies. The APIs support many other configuration settings, which can be found by exploring the `appsettings.json` files in their respective source folders.

Any setting from those files can be overridden using an environment variable. To do so, convert the JSON path of the setting to an environment variable name by replacing the colon (`:`) with a double underscore (`__`).

For example, to change the default log level, add the following line to your `.env` file:

```dotenv
# Example: Overriding a nested setting from appsettings.json
Logging__LogLevel__Default=Warning
```

---

## License

This project is licensed under the [**BSD 3-Clause License**](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for details.
