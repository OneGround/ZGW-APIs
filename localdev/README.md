# Docker Hosted Services

- [Docker Hosted Services](#docker-hosted-services)
  - [Docker hosted tools \& services](#docker-hosted-tools--services)
    - [Postgres](#postgres)
    - [RabbitMQ](#rabbitmq)
    - [Keycloack](#keycloack)
  - [Docker hosted ZGW APIs](#docker-hosted-zgw-apis)
    - [Start and stop ZGW APIs](#start-and-stop-zgw-apis)
    - [Install the SSL Certificate Guide](#install-the-ssl-certificate-guide)
      - [For Windows Users (PowerShell)](#for-windows-users-powershell)
      - [For macOS and Linux Users (Bash)](#for-macos-and-linux-users-bash)
      - [Recommendation](#recommendation)
    - [Update hosts file with those lines](#update-hosts-file-with-those-lines)
    - [Authentication Guide](#authentication-guide)
      - [Step 1: Get the Client Secret from Keycloak](#step-1-get-the-client-secret-from-keycloak)
      - [Step 2: Update Environment and Restart Services](#step-2-update-environment-and-restart-services)
      - [Step 3: Request an Access Token](#step-3-request-an-access-token)
        - [For Windows (PowerShell)](#for-windows-powershell)
        - [For Linux, macOS, or WSL](#for-linux-macos-or-wsl)
        - [Expected Result](#expected-result)
    - [HaProxy status page](#haproxy-status-page)
    - [Zaken Api](#zaken-api)
    - [Documenten Api](#documenten-api)
    - [Autorisaties Api](#autorisaties-api)
    - [Catalogi Api](#catalogi-api)
    - [Besluiten Api](#besluiten-api)
    - [Notificaties Api](#notificaties-api)
    - [Referentielijsten Api](#referentielijsten-api)

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

## Docker hosted ZGW APIs

### Start and stop ZGW APIs

- Run command from localdev folder to start:

    ```bash
    docker compose --project-directory .\ --env-file .\.env -f docker-compose.oneground.yml up -d
    ```

- Run command from localdev folder to stop:

    ```bash
    docker compose --project-directory .\ -f docker-compose.oneground.yml down
    ```

### Install the SSL Certificate Guide

Upon successful start, you will find a new folder named `oneground-certificates` inside `localdev` containing the following files:

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
    cd path/to/your/project/localdev/oneground-certificates-installer
    ```

3. **Run the installation script.**

    ```powershell
    .\install-oneground-certificate.ps1
    ```

    The script will import the certificate into the Windows "Trusted Root Certification Authorities" store.

#### For macOS and Linux Users (Bash)

1. **Open your terminal.**
2. **Navigate to the certificate installer directory.**

    ```bash
    cd path/to/your/project/localdev/oneground-certificates-installer
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

#### Recommendation

After certificate installation, it is recommended to restart your web browser.

### Update hosts file with those lines

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

### Authentication Guide

Follow these two steps to get a Bearer token for the API.

#### Step 1: Get the Client Secret from Keycloak

1. Navigate to the OneGround client credentials page in Keycloak by clicking this link:

   - <http://localhost:8080/admin/master/console/#/OneGround/clients/f1a6fa82-656f-4b95-b29c-91ce86414e90/credentials>

2. Login if prompted.

   - **Username**: admin
   - **Password**: admin

3. Under the **Credentials** tab, copy the value in the `Client Secret` field. This is your `<oneground-client-secret>`.

#### Step 2: Update Environment and Restart Services

After getting your `oneground-client-secret-from-keycloak`, you need to add it to your project's environment file and restart your services for the change to take effect.

1. Update Your `default.env` File
   - Open the `default.env` file located in the localdev folder and replace its placeholder value with the secret you copied from Keycloak:

        ```text
        ZgwServiceAccounts__Credentials__ClientSecret=<oneground-client-secret>
        ```

2. Restart you Docker Containers
    - Run the following command form localdev folder:

        ```bash
        docker compose --project-directory .\ --env-file .\.env -f docker-compose.oneground.yml up -d
        ```

#### Step 3: Request an Access Token

Now, use the `<client_id>` and the `<oneground-client-secret>` you just copied to get an access token.

Choose the command for your operating system and replace `<client_id>` and `<oneground-client-secret>` with the value from Step 1.

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
    "not-before-policy": 0,
    "scope": "email profile"
}
```

You can now use this `access_token` as a Bearer token to authorize your API requests.

### HaProxy status page

- **Status page:** <https://haproxy.oneground.local>

### Zaken Api

- **Port:** 5005
- **Swagger:** <https://zaken.oneground.local/swagger>
- **Health:** <https://zaken.oneground.local/health>

### Documenten Api

- **Port:** 5007
- **Swagger:** <https://documenten.oneground.local/swagger>
- **Health:** <https://documenten.oneground.local/health>

### Autorisaties Api

- **Port:** 5009
- **Swagger:** <https://autorisaties.oneground.local/swagger>
- **Health:** <https://autorisaties.oneground.local/health>

### Catalogi Api

- **Port:** 5010
- **Swagger:** <https://catalogi.oneground.local/swagger>
- **Health:** <https://catalogi.oneground.local/health>

### Besluiten Api

- **Port:** 5013
- **Swagger:** <https://besluiten.oneground.local/swagger>
- **Health:** <https://besluiten.oneground.local/health>

### Notificaties Api

- **Port:** 5015
- **Swagger:** <https://notificaties.oneground.local/swagger>
- **Health:** <https://notificaties.oneground.local/health>

### Referentielijsten Api

- **Port:** 5018
- **Swagger:** <https://referentielijsten.oneground.local/swagger>
- **Health:** <https://referentielijsten.oneground.local/health>
