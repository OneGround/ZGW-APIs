# Docker Hosted OneGround Services

## About This Setup

This guide walks you through launching a complete, local demonstration of the OneGround ZGW APIs using the provided **Docker Compose** setup. The goal is to get you up and running quickly so you can explore the entire suite of services.

This setup includes:

- All core ZGW API services running in containers
- HAProxy for routing services to friendly local domains
- Keycloak for authentication, pre-configured for the services
- Scripts to automatically generate and install a local SSL certificate

Follow the instructions below to launch the stack and interact with the live APIs.

## Prerequisites

- [GIT](https://github.com/git-guides/install-git)
- [Docker Engine](https://docs.docker.com/engine/install/) (Desktop or Server)
- [Docker Compose](https://docs.docker.com/compose/install/) (sometimes comes bundled with Docker Engine)

## Getting started

### Clone the repository

1. Clone the respository:

    ```bash
    git clone https://github.com/OneGround/ZGW-APIs.git
    ```

2. Open directory:

    ```bash
    cd ZGW_APIs\getting-started\docker-compose
    ```

### Start ZGW APIs

Run command from `docker-compose` directory to start:

```bash
docker compose --project-directory .\ --env-file .\.env -f docker-compose.oneground-packages.yml up -d
```

### Install the SSL Certificate

Upon successful ZGW APIs start, you will find a new folder named `oneground-certificates` inside `docker-compose` containing the following files:

- `oneground.local.pem` - The public certificate
- `oneground.local.key` - The private key
- `oneground.local.combined.pem` - A combination of the key and certificate

To make your browser and system trust the generated certificate, you need to install it into your system's trust store.

#### For Windows Users (PowerShell)

1. **Open PowerShell with Administrator privileges.**
    - Click the Start menu.
    - Type "PowerShell".
    - Right-click on "Windows PowerShell" and select "Run as administrator".

2. **Navigate to the certificate installer directory.**

    ```powershell
    cd ZGW_APIs/tools/oneground-certificates-installer
    ```

3. **Run the installation script.**

    ```powershell
    .\install-oneground-certificate.ps1
    ```

    The script will import the certificate into the Windows `Trusted Root Certification Authorities` store.

#### For macOS and Linux Users (Bash)

1. **Open your terminal.**
2. **Navigate to the certificate installer directory.**

    ```bash
    cd ZGW_APIs/tools/oneground-certificates-installer
    ```

3. **Make the script executable** (you only need to do this once).

    ```bash
    chmod +x ./install-oneground-certificate.sh
    ```

4. **Run the installation script.** You may be prompted for your password to authorize the changes.

    ```bash
    ./install-oneground-certificate.sh
    ```

    This script will install the certificate into your system's keychain or trust store.

#### After certificate installation

After certificate installation, it is recommended to restart your web browser.

### Setup ZGW APIs Authentication

Follow these two steps to get a Bearer token for the API.

#### Step 1: Get the Client Secret from Keycloak

1. Navigate to the OneGround clients in Keycloak:

   - <http://localhost:8080/admin/master/console/#/OneGround/clients>

2. Login if prompted.

   - **Username**: admin
   - **Password**: admin

3. Select `oneground-000000000` from a list

4. Under the **Credentials** tab, copy the value in the `Client Secret` field. This is your `<oneground-client-secret>`.

#### Step 2: Update Environment variables and Restart Services

After getting your `oneground-client-secret`, you need to add it to your project's environment file and restart your services for the change to take effect.

1. Open dircetory:

    ```bash
    cd ZGW_APIs\getting-started\docker-compose
    ```

2. Update `default.env` file
   - Open the `default.env` file and replace its placeholder value with the secret you copied from Keycloak:

        ```text
        ZgwServiceAccounts__Credentials__0__ClientSecret=<oneground-client-secret>
        ```

3. Restart you Docker Containers
    - Run the following command form docker-compose folder:

        ```bash
        docker compose --project-directory .\ --env-file .\.env -f docker-compose.oneground-packages.yml up -d
        ```

#### Step 3: Request an Access Token

Now, use the `<client_id>` (default value for this setup is **oneground-000000000**) and the `<oneground-client-secret>` you just copied to get an access token.

Choose the command for your operating system and replace `<client_id>` and `<oneground-client-secret>` with the actual values.

##### For Windows (PowerShell)

Use this `Invoke-RestMethod` command in your PowerShell terminal

```bash
$response = Invoke-WebRequest -Uri "http://localhost:8080/realms/OneGround/protocol/openid-connect/token" -Method POST -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} -Body "grant_type=client_credentials&client_id=<oneground-client-id>&client_secret=<oneground-client-secret>"
```

and then

```bash
$response.Content
```

##### For Linux, macOS, or WSL

Use this curl command in your terminal.

```bash
curl --location --request POST 'http://localhost:8080/realms/OneGround/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'grant_type=client_credentials' \
--data-urlencode 'client_id=<oneground-client-id>' \
--data-urlencode 'client_secret=<oneground-client-secret>' \
```

##### Expected Result

Either command will return a JSON object containing the access_token.

```json
{
    "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAi......",
    "expires_in": 300,
    "refresh_expires_in": 0,
    "token_type": "Bearer",
    ...
}
```

You can now use this `access_token` as a Bearer token to authorize your API requests.

### Update hosts file

Update hosts file with those lines:

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

### Stop ZGW APIs

Run command to stop Docker containers:

```bash
docker compose --project-directory .\ -f docker-compose.oneground-packages.yml down
```

## Docker hosted ZGW APIs

### HaProxy status page

- **Status page:** <https://haproxy.oneground.local>

### Autorisaties Api

- **Port:** 5009
- **Swagger:** <https://autorisaties.oneground.local/swagger>
- **Health:** <https://autorisaties.oneground.local/health>

### Besluiten Api

- **Port:** 5013
- **Swagger:** <https://besluiten.oneground.local/swagger>
- **Health:** <https://besluiten.oneground.local/health>

### Catalogi Api

- **Port:** 5010
- **Swagger:** <https://catalogi.oneground.local/swagger>
- **Health:** <https://catalogi.oneground.local/health>

### Documenten Api

- **Port:** 5007
- **Swagger:** <https://documenten.oneground.local/swagger>
- **Health:** <https://documenten.oneground.local/health>

### Notificaties Api

- **Port:** 5015
- **Swagger:** <https://notificaties.oneground.local/swagger>
- **Health:** <https://notificaties.oneground.local/health>

### Referentielijsten Api

- **Port:** 5018
- **Swagger:** <https://referentielijsten.oneground.local/swagger>
- **Health:** <https://referentielijsten.oneground.local/health>

### Zaken Api

- **Port:** 5005
- **Swagger:** <https://zaken.oneground.local/swagger>
- **Health:** <https://zaken.oneground.local/health>

## Docker hosted tools & services

### Postgres

Postgres including all zgw services in docker

```txt
servername: zgw_db
server port: 5432
database(s): zrc_db
```

Postgres stand-alone

```txt
servername: postgres_docker_db
server port: 5432
database(s): zrc_db
```

### RabbitMQ

<http://localhost:15672>

### Keycloack

<http://localhost:8080>
