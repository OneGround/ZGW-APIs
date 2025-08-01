# OneGround ZGW APIs - Standalone Deployment Guide

This guide provides all the necessary steps to run any of the OneGround ZGW APIs as standalone services using the provided Docker Compose setups.

## Table of Contents

- [OneGround ZGW APIs - Standalone Deployment Guide](#oneground-zgw-apis---standalone-deployment-guide)
  - [Table of Contents](#table-of-contents)
  - [Core Prerequisites](#core-prerequisites)
  - [General Setup Instructions](#general-setup-instructions)
    - [Step 1: Obtain the Setup Files](#step-1-obtain-the-setup-files)
    - [Step 2: Understand the Files](#step-2-understand-the-files)
    - [Step 3: Configure Docker Networking](#step-3-configure-docker-networking)
  - [API-Specific Configuration](#api-specific-configuration)
    - [Autorisaties API (AC)](#autorisaties-api-ac)
      - [Environment Variables (`.env`)](#environment-variables-env)
    - [Besluiten API (BRC)](#besluiten-api-brc)
      - [Environment Variables (`.env`)](#environment-variables-env-1)
    - [Catalogi API (ZTC)](#catalogi-api-ztc)
      - [Environment Variables (`.env`)](#environment-variables-env-2)
    - [Documenten API (DRC)](#documenten-api-drc)
      - [Environment Variables (`.env`)](#environment-variables-env-3)
    - [Notificaties API (NRC)](#notificaties-api-nrc)
      - [Environment Variables (`.env`)](#environment-variables-env-4)
    - [Referentielijsten API (RC)](#referentielijsten-api-rc)
    - [Zaken API (ZRC)](#zaken-api-zrc)
      - [Environment Variables (`.env`)](#environment-variables-env-5)
  - [Running the Service](#running-the-service)
  - [Advanced Configuration](#advanced-configuration)
  - [Contributing](#contributing)
  - [License](#license)

---

## Core Prerequisites

Before you begin, ensure you have the following components installed and running for most API setups:

- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/install/)
- A running **PostgreSQL** instance
- A running **RabbitMQ** instance
- A running **Redis** instance
- An **OpenID Connect (OIDC)** compliant Identity Provider

**Note:** Not all APIs require every component. See the [API-Specific Configuration](#api-specific-configuration) section for the exact prerequisites for each service.

---

## General Setup Instructions

The setup process is similar for all APIs.

### Step 1: Obtain the Setup Files

Download the necessary configuration files for the specific API you want to run. You can find the links in the [API-Specific Configuration](#api-specific-configuration) section below. After downloading the ZIP archive, extract it to your local machine and navigate into the created directory.

### Step 2: Understand the Files

You will typically work with two primary files:

- `.env`: Contains all the configuration variables and secrets for connecting to your infrastructure.
- `docker-compose.yml`: Defines the API service, maps the port, and handles network configuration.

### Step 3: Configure Docker Networking

For APIs that need to communicate with other services, they must be on the same Docker network.

1. Open the `docker-compose.yml` file.
2. Locate the `networks.external_network` section at the bottom.
3. Change `name: bridge` to the name of your existing external network where your other services are running.

```yaml
# docker-compose.yml

networks:
  external_network:
    # IMPORTANT: Change 'bridge' to the name of the Docker network
    # where your other services are running.
    name: my-shared-network # <-- CHANGE THIS
    external: true
```

---

## API-Specific Configuration

This section contains the unique prerequisites, download links, environment variables, and default ports for each API.

---

### Autorisaties API (AC)

- **Description:** Handles authorizations between ZGW services.
- **Download Link:** [**Download the `standalone/AC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FAC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- **Prerequisites:** Core Prerequisites only.
- **Default Port:** `5009`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |

---

### Besluiten API (BRC)

- **Description:** Manages decisions (`besluiten`).
- **Download Link:** [**Download the `standalone/BRC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FBRC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/besluiten-api)
- **Additional Prerequisites:** Autorisaties API (AC), Notificaties API (NRC).
- **Default Port:** `5013`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). |
| `Services__NRC__Api` | The base URL of the Notificaties API (NRC). |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. |

---

### Catalogi API (ZTC)

- **Description:** Manages catalogs and catalog data.
- **Download Link:** [**Download the `standalone/ZTC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FZTC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- **Additional Prerequisites:** Autorisaties API (AC), Notificaties API (NRC).
- **Default Port:** `5010`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). |
| `Services__NRC__Api` | The base URL of the Notificaties API (NRC). |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. |

---

### Documenten API (DRC)

- **Description:** Manages documents and their metadata.
- **Download Link:** [**Download the `standalone/DRC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FDRC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)
- **Additional Prerequisites:** Autorisaties API (AC), Besluiten API (BRC), Catalogi API (ZTC), Notificaties API (NRC), Zaken API (ZRC).
- **Default Port:** `5011`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). |
| `Services__BRC__Api` | The base URL of the Besluiten API (BRC). |
| `Services__ZTC__Api` | The base URL of the Catalogi API (ZTC). |
| `Services__NRC__Api` | The base URL of the Notificaties API (NRC). |
| `Services__ZRC__Api` | The base URL of the Zaken API (ZRC). |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. |

---

### Notificaties API (NRC)

- **Description:** Consumes events from the event bus and makes them available to other applications.
- **Download Link:** [**Download the `standalone/NRC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FNRC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)
- **Additional Prerequisites:** Autorisaties API (AC).
- **Default Port:** `5012`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. |

---

### Referentielijsten API (RC)

- **Description:** A self-contained service that serves reference lists and allows anonymous access.
- **Download Link:** [**Download the `standalone/RC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FRC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/referentielijsten-api)
- **Prerequisites:** Docker and Docker Compose only.
- **Default Port:** `5014`
- **Environment Variables:** None required for basic setup. An optional `.env` file can be used for advanced configuration.

---

### Zaken API (ZRC)

- **Description:** Manages cases (`zaken`).
- **Download Link:** [**Download the `standalone/ZRC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FZRC)
- **Image Versions:** [GitHub Packages](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api)
- **Additional Prerequisites:** Autorisaties API (AC), Besluiten API (BRC), Catalogi API (ZTC), Documenten API (DRC), Notificaties API (NRC).
- **Default Port:** `5015`

#### Environment Variables (`.env`)

| Variable | Description |
| :--- | :--- |
| `ConnectionStrings__UserConnectionString` | The full connection string for the standard database user. |
| `ConnectionStrings__AdminConnectionString`| The full connection string for the database admin user. |
| `Eventbus__HostName` | The hostname or IP address of your RabbitMQ server. |
| `Eventbus__VirtualHost` | The virtual host to use on RabbitMQ. |
| `Eventbus__UserName` | The username for authenticating with RabbitMQ. |
| `Eventbus__Password` | The password for authenticating with RabbitMQ. |
| `Redis__ConnectionString` | The connection string for your Redis instance. |
| `Auth__Authority` | The base URL of your OIDC Identity Provider. |
| `Auth__ValidIssuer` | The issuer URL of your OIDC Identity Provider. |
| `Auth__ValidAudience` | The audience value (`aud` claim) for validating JWTs. |
| `Services__AC__Api` | The base URL of the Autorisaties API (AC). |
| `Services__BRC__Api` | The base URL of the Besluiten API (BRC). |
| `Services__DRC__Api` | The base URL of the Documenten API (DRC). |
| `Services__NRC__Api` | The base URL of the Notificaties API (NRC). |
| `Services__ZTC__Api` | The base URL of the Catalogi API (ZTC). |
| `ZgwServiceAccounts__Credentials__0__Rsin` | The RSIN of the service account for inter-service communication. |
| `ZgwServiceAccounts__Credentials__0__ClientId` | The Client ID of the service account. |
| `ZgwServiceAccounts__Credentials__0__ClientSecret`| The Client Secret of the service account. |

---

## Running the Service

Once you have configured your `.env` file for the chosen API, you can start the service in detached mode from your terminal:

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

The provided `.env` files cover only the mandatory dependencies. The APIs support many other configuration settings, which can be found by exploring the `appsettings.json` files in their respective source repositories.

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
