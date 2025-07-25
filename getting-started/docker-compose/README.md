# OneGround ZGW APIs: Docker Compose Setup

## About This Guide

This guide provides instructions for launching a complete, local demonstration of the OneGround ZGW APIs using the provided **Docker Compose** setup. The goal is to get you up and running quickly so you can explore the entire suite of services.

This setup includes:

* All core ZGW API services running in Docker containers.
* HAProxy for routing services to user-friendly local domain names.
* Keycloak for authentication, pre-configured for the services.
* Scripts to automatically generate and install a local SSL certificate.

Follow the instructions below to launch the stack, authenticate, and interact with the live APIs.

## Prerequisites

Before you begin, ensure you have the following software installed:

* [GIT](https://github.com/git-guides/install-git)
* [Docker Engine](https://docs.docker.com/engine/install/) (Desktop or Server)
* [Docker Compose](https://docs.docker.com/compose/install/) (This is often included with Docker Desktop)

## Getting Started

### 1. Clone the Repository

First, clone the repository to your local machine and navigate into the `docker-compose` directory.

```bash
# Clone the repository
git clone https://github.com/OneGround/ZGW-APIs.git

# Navigate to the docker-compose directory
cd ZGW-APIs/getting-started/docker-compose
```

### 2. Start the ZGW API Services

From the `docker-compose` directory, run the following command to start all the required services in the background.

```bash
docker compose --project-directory . --env-file ./.env -f docker-compose.oneground-packages.yml up -d
```

### 3. Install the Local SSL Certificate

For your browser to trust the local services, you need to install the generated SSL certificate. After the services start, a new folder named `oneground-certificates` will appear in your current directory. This folder should contain those files:

* `oneground.local.pem` - The public certificate
* `oneground.local.key` - The private key
* `oneground.local.combined.pem` - A combination of the key and certificate

> **Note:** The generated SSL certificate is valid for 365 days.

Follow the steps for your operating system.

#### For Windows (using PowerShell)

1. **Open PowerShell as an Administrator.**
    * Click the Start menu, type "PowerShell", right-click on "Windows PowerShell", and select "Run as administrator".
2. **Navigate to the certificate installer directory.**

    ```powershell
    cd ../../tools/oneground-certificates-installer
    ```

3. **Run the installation script.**

    ```powershell
    .\install-oneground-certificate.ps1
    ```

    The script will import the certificate into the Windows "Trusted Root Certification Authorities" store.

#### For macOS and Linux (using Bash)

1. **Open your terminal.**
2. **Navigate to the certificate installer directory.**

    ```bash
    cd ../../tools/oneground-certificates-installer
    ```

3. **Make the script executable** (you only need to do this once).

    ```bash
    chmod +x ./install-oneground-certificate.sh
    ```

4. **Run the installation script.** You may be prompted for your password.

    ```bash
    ./install-oneground-certificate.sh
    ```

    This script installs the certificate into your system's keychain or trust store.

> **Note:** After installing the certificate, we recommend restarting your web browser to ensure the changes take effect.

### 4. Update Your `hosts` File

To access the services using friendly domain names (e.g., `zaken.oneground.local`), you need to add entries to your system's `hosts` file.

1. Open your `hosts` file as an administrator.
    * **Windows:** `C:\Windows\System32\drivers\etc\hosts`
    * **macOS/Linux:** `/etc/hosts`
2. Add the following lines to the end of the file and save it.

    ```txt
    127.0.0.1 zaken.oneground.local
    127.0.0.1 catalogi.oneground.local
    127.0.0.1 notificaties.oneground.local
    127.0.0.1 notificaties-receiver.oneground.local
    127.0.0.1 besluiten.oneground.local
    127.0.0.1 documenten.oneground.local
    127.0.0.1 autorisaties.oneground.local
    127.0.0.1 referentielijsten.oneground.local
    127.0.0.1 haproxy.oneground.local
    ```

### 5. Configure API Authentication

To make authorized requests to the APIs, you first need to get a client secret from Keycloak and then use it to request a Bearer token.

#### Step 5.1: Get the Client Secret from Keycloak

1. Navigate to the Keycloak admin console: `http://localhost:8080/admin/master/console/#/OneGround/`
2. Log in using the credentials:
    * **Username**: `admin`
    * **Password**: `admin`
3. From the navigation on the left, select **Clients**.
4. Select the `oneground-000000000` client from the list.
    > **Note on the Default Client:** This local setup is configured with a single default client, `oneground-000000000`, which has full administrative access to all APIs. If you wish to add more clients with specific permissions, you must first create them in Keycloak by following the [Keycloak Setup Guide](../../localdev/keycloak/KeycloakSetup/README.md). After creating a new client, you must also configure its permissions using the Autorisaties API or by updating the [autorisaties service's seed data](../oneground-services-data/ac-data/applicaties.json).
5. Go to the **Credentials** tab.
6. Copy the value from the **Client Secret** field. This is your `<oneground-client-secret>`.

#### Step 5.2: Update Environment File and Restart Services

1. Return to the `ZGW_APIs/getting-started/docker-compose` directory in your terminal.
2. Open the `.env` file in a text editor.
3. Find the following line and replace the placeholder with the secret you copied from Keycloak:

    ```text
    ZgwServiceAccounts__Credentials__0__ClientSecret=<oneground-client-secret>
    ```

4. Save the `.env` file.
5. Restart the Docker containers to apply the new configuration.

    ```bash
    docker compose --project-directory . --env-file ./.env -f docker-compose.oneground-packages.yml up -d
    ```

#### Step 5.3: Request an Access Token

Now you can exchange the client credentials for a temporary access token. Use the command for your operating system, replacing `<oneground-client-secret>` with your actual secret. The default client ID is `oneground-000000000`.

##### For Windows (PowerShell)

```powershell
$response = Invoke-WebRequest -Uri "http://localhost:8080/realms/OneGround/protocol/openid-connect/token" -Method POST -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} -Body "grant_type=client_credentials&client_id=oneground-000000000&client_secret=<oneground-client-secret>"
$response.Content
```

##### For Linux, macOS, or WSL (cURL)

```bash
curl --location --request POST 'http://localhost:8080/realms/OneGround/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'grant_type=client_credentials' \
--data-urlencode 'client_id=oneground-000000000' \
--data-urlencode 'client_secret=<oneground-client-secret>'
```

You will receive a JSON response containing the `access_token`. You can now use this token as a `Bearer` token to authorize your API requests.

```json
{
    "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAi......",
    "expires_in": 300,
    "refresh_expires_in": 0,
    "token_type": "Bearer",
}
```

## Stopping the Services

To stop all running Docker containers, run the following command from the `docker-compose` directory:

```bash
docker compose --project-directory . -f docker-compose.oneground-packages.yml down
```

## Service Endpoints and Tools

Here is a reference list of all the services and tools running in this Docker setup.

### ZGW API Services

| Service                   | Swagger UI                                                 | Health Check                                               |
| :------------------------ | :--------------------------------------------------------- | :--------------------------------------------------------- |
| **Autorisaties API** | `https://autorisaties.oneground.local/swagger`             | `https://autorisaties.oneground.local/health`              |
| **Besluiten API** | `https://besluiten.oneground.local/swagger`                | `https://besluiten.oneground.local/health`                 |
| **Catalogi API** | `https://catalogi.oneground.local/swagger`                 | `https://catalogi.oneground.local/health`                  |
| **Documenten API** | `https://documenten.oneground.local/swagger`               | `https://documenten.oneground.local/health`                |
| **Notificaties API** | `https://notificaties.oneground.local/swagger`             | `https://notificaties.oneground.local/health`              |
| **Referentielijsten API** | `https://referentielijsten.oneground.local/swagger`        | `https://referentielijsten.oneground.local/health`         |
| **Zaken API** | `https://zaken.oneground.local/swagger`                    | `https://zaken.oneground.local/health`                     |

### Hosted Tools

| Tool              | URL                       | Notes                                       |
| :---------------- | :------------------------ | :------------------------------------------ |
| **HAProxy Stats** | `https://haproxy.oneground.local` | View statistics for the reverse proxy.      |
| **Keycloak** | `http://localhost:8080`     | Identity and Access Management.             |
| **RabbitMQ** | `http://localhost:15672`    | Message broker management UI.               |
| **PostgreSQL** | `localhost:5432`          | Database server. Connect with any SQL client. |
