# OneGround ZGW APIs: Docker Compose Setup

- [OneGround ZGW APIs: Docker Compose Setup](#oneground-zgw-apis-docker-compose-setup)
  - [About This Guide](#about-this-guide)
  - [Prerequisites](#prerequisites)
  - [Getting Started](#getting-started)
    - [1. Download the Repository](#1-download-the-repository)
    - [2. Start the ZGW API Services](#2-start-the-zgw-api-services)
    - [3. Install the Local SSL Certificate](#3-install-the-local-ssl-certificate)
      - [For Windows (using PowerShell)](#for-windows-using-powershell)
      - [For macOS and Linux (using Bash)](#for-macos-and-linux-using-bash)
    - [4. Update Your `hosts` File](#4-update-your-hosts-file)
    - [5. Configure API Authentication](#5-configure-api-authentication)
      - [Step 5.1: Get the Client Secret from Keycloak](#step-51-get-the-client-secret-from-keycloak)
      - [Step 5.2: Update Environment File and Restart Services](#step-52-update-environment-file-and-restart-services)
      - [Step 5.3: Request an Access Token](#step-53-request-an-access-token)
        - [For Windows (PowerShell)](#for-windows-powershell)
        - [For Linux, macOS, or WSL (cURL)](#for-linux-macos-or-wsl-curl)
    - [6. Stopping the Services](#6-stopping-the-services)
  - [Service Endpoints and Tools](#service-endpoints-and-tools)
    - [ZGW API Services](#zgw-api-services)
    - [Hosted Tools](#hosted-tools)

## About This Guide

This guide provides instructions for launching a complete, local demonstration of the OneGround ZGW APIs using the provided **Docker Compose** setup. The goal is to get you up and running quickly so you can explore the entire suite of services.

This setup includes:

- All core ZGW API services running in Docker containers.
- HAProxy for routing services to user-friendly local domain names.
- Keycloak for authentication, pre-configured for the services.
- Scripts to automatically generate and install a local SSL certificate.

Follow the instructions below to launch the stack, authenticate, and interact with the live APIs.

## Prerequisites

Before you begin, ensure you have the following software installed:

- [Docker Engine](https://docs.docker.com/engine/install/) (Desktop or Server)
- [Docker Compose](https://docs.docker.com/compose/install/) (This is often included with Docker Desktop)

## Getting Started

### 1. Download the Repository

1. Download the repository as a ZIP file by opening this URL: [https://github.com/OneGround/ZGW-APIs/archive/refs/heads/main.zip](https://github.com/OneGround/ZGW-APIs/archive/refs/heads/main.zip)
2. Unzip the downloaded file. This will create a folder named `ZGW-APIs-main`.
3. Open your terminal and navigate into the `docker-compose` directory:

```bash
cd ZGW-APIs-main/getting-started/docker-compose
```

### 2. Start the ZGW API Services

From the `docker-compose` directory, run the following command to start all the required services in the background.

```bash
docker compose --env-file ./.env  up -d
```

### 3. Install the Local SSL Certificate

For your browser to trust the local services, you need to install the generated SSL certificate. After the services start, a new folder named `oneground-certificates` will appear in your current directory. This folder should contain these files:

- `oneground.local.pem` - The public certificate
- `oneground.local.key` - The private key
- `oneground.local.combined.pem` - A combination of the key and certificate

> **Note:** The generated SSL certificate is valid for 365 days.

Follow the steps for your operating system.

#### For Windows (using PowerShell)

1. **Open PowerShell as an Administrator.**
   - Click the Start menu, type "PowerShell", right-click on "Windows PowerShell", and select "Run as administrator".
2. **Navigate to the certificate installer directory.**

   ```powershell
   cd ZGW-APIs-main/tools/oneground-certificates-installer
   ```

3. **Run the installation script.**

   ```powershell
   .\install-oneground-certificate.ps1 -RelativeCertPath "..\..\getting-started\docker-compose\oneground-certificates\oneground.local.pem"
   ```

   The script will import the certificate into the Windows "Trusted Root Certification Authorities" store.

#### For macOS and Linux (using Bash)

1. **Open your terminal.**
2. **Navigate to the certificate installer directory.**

   ```bash
   cd ZGW-APIs-main/tools/oneground-certificates-installer
   ```

3. **Make the script executable** (you only need to do this once).

   ```bash
   chmod +x ./install-oneground-certificate.sh
   ```

4. **Run the installation script.** You may be prompted for your password.

   ```bash
   ./install-oneground-certificate.sh ../../getting-started/docker-compose/oneground-certificates/oneground.local.pem
   ```

   This script installs the certificate into your system's keychain or trust store.

> **Note:** After installing the certificate, we recommend restarting your web browser to ensure the changes take effect.

### 4. Update Your `hosts` File

To ensure all services can communicate with each other and are accessible in your browser, you need to map the service hostnames to your local machine. This is a crucial step for the local domain names (e.g., `zaken.oneground.local`) to work correctly.

1. Open your `hosts` file as an administrator.
   - **Windows:** `C:\Windows\System32\drivers\etc\hosts`
   - **macOS/Linux:** `/etc/hosts`
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

1. Navigate to the Keycloak admin console: [http://localhost:8080/admin/master/console/#/OneGround/](http://localhost:8080/admin/master/console/#/OneGround/)
2. Log in using the credentials:
   - **Username**: `admin`
   - **Password**: `admin`
3. From the navigation on the left, select **Clients**.
4. Select the `oneground-000000000` client from the list.
   > **Note on the Default Client:** This local setup is configured with a single default client, `oneground-000000000`, which has full administrative access to all APIs. If you wish to add more clients with specific permissions, you must first create them in Keycloak by following the [Keycloak Setup Guide](../../localdev/keycloak/KeycloakSetup/README.md). After creating a new client, you must also configure its permissions using the Autorisaties API or by updating the [autorisaties service's seed data](../oneground-services-data/ac-data/applicaties.json).
5. Go to the **Credentials** tab.
6. Copy the value from the **Client Secret** field. This is your `<oneground-client-secret>`.

#### Step 5.2: Update Environment File and Restart Services

1. Return to the `ZGW-APIs-main/getting-started/docker-compose` directory in your terminal:

    ```bash
    cd ZGW-APIs-main/getting-started/docker-compose
    ```

2. Open the [ZGW-APIs-main/getting-started/docker-compose/default.env](./default.env) file in a text editor.
3. Find the following line and replace the placeholder with the secret you copied from Keycloak:

   ```text
   ZgwServiceAccounts__Credentials__0__ClientSecret=<oneground-client-secret>
   ```

4. Save the `default.env` file.
5. Restart the Docker containers to apply the new configuration:

   ```bash
   docker compose --env-file ./.env  up -d
   ```

#### Step 5.3: Request an Access Token

This process uses the standard OAuth 2.0 Client Credentials grant type to obtain an access token. Now you can exchange the client credentials for a temporary access token. Use the command for your operating system, replacing `<oneground-client-secret>` with your actual secret. The default client ID is `oneground-000000000`.

##### For Windows (PowerShell)

- Open Windows PowerShell and execute this command:

    ```powershell
    # Assign the web request result to the $response variable
    $response = Invoke-WebRequest `
        -Uri "http://localhost:8080/realms/OneGround/protocol/openid-connect/token" `
        -Method POST `
        -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} `
        -Body "grant_type=client_credentials&client_id=oneground-000000000&client_secret=<oneground-client-secret>"
    ```

- Then take an access token from `$response`

    ```powershell
    # Display the content of the response
    $response.Content
    ```

##### For Linux, macOS, or WSL (cURL)

- Open terminal and execute this command:

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
    "token_type": "Bearer"
}
```

> **Tip: How to Increase Token Expiration Time**
> By default, the access token expires in 5 minutes (300 seconds). To increase this time:
>
> 1. Navigate directly to the **Tokens** settings page in Keycloak: [http://localhost:8080/admin/master/console/#/OneGround/realm-settings/tokens](http://localhost:8080/admin/master/console/#/OneGround/realm-settings/tokens).
> 2. In the `Access Token Lifespan` field, set a longer duration (e.g., `30 minutes` or `1 hour`).
> 3. Click **Save**.
>
> You will need to request a new token for this change to take effect.

### 6. Stopping the Services

To stop all running Docker containers, run the following command from the `docker-compose` directory:

```bash
docker compose down
```

## Service Endpoints and Tools

Here is a reference list of all the services and tools running in this Docker setup.

### ZGW API Services

| Service | Port | Swagger UI | Health Check |
| :--- | :--- | :--- | :--- |
| **Autorisaties API** | 5009 | [https://autorisaties.oneground.local/swagger](https://autorisaties.oneground.local/swagger) | [https://autorisaties.oneground.local/health](https://autorisaties.oneground.local/health) |
| **Besluiten API** | 5013 | [https://besluiten.oneground.local/swagger](https://besluiten.oneground.local/swagger) | [https://besluiten.oneground.local/health](https://besluiten.oneground.local/health) |
| **Catalogi API** | 5011 | [https://catalogi.oneground.local/swagger](https://catalogi.oneground.local/swagger) | [https://catalogi.oneground.local/health](https://catalogi.oneground.local/health) |
| **Documenten API** | 5007 | [https://documenten.oneground.local/swagger](https://documenten.oneground.local/swagger) | [https://documenten.oneground.local/health](https://documenten.oneground.local/health) |
| **Notificaties API** | 5015 | [https://notificaties.oneground.local/swagger](https://notificaties.oneground.local/swagger) | [https://notificaties.oneground.local/health](https://notificaties.oneground.local/health) |
| **Referentielijsten API** | 5018 | [https://referentielijsten.oneground.local/swagger](https://referentielijsten.oneground.local/swagger) | [https://referentielijsten.oneground.local/health](https://referentielijsten.oneground.local/health) |
| **Zaken API** | 5005 | [https://zaken.oneground.local/swagger](https://zaken.oneground.local/swagger) | [https://zaken.oneground.local/health](https://zaken.oneground.local/health) |


### Hosted Tools

| Tool | URL | Notes |
| :--- | :--- | :--- |
| **HAProxy Stats** | [https://haproxy.oneground.local](https://haproxy.oneground.local) | View statistics for the reverse proxy. |
| **Keycloak** | [http://localhost:8080](http://localhost:8080) | Identity and Access Management (user: admin, password: admin). |
| **RabbitMQ** | [http://localhost:15672](http://localhost:15672) | Message broker management UI (user: guest, password: guest). |
| **PostgreSQL** | `http://localhost:5432` | Database server. Connect with any SQL client. |
