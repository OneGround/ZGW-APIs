# Docker Hosted Services

- [Docker Hosted Services](#docker-hosted-services)
  - [Docker hosted tools \& services](#docker-hosted-tools--services)
    - [Postgres](#postgres)
    - [RabbitMQ](#rabbitmq)
  - [Docker hosted ZGW APIs](#docker-hosted-zgw-apis)
    - [Start and stop ZGW APIs](#start-and-stop-zgw-apis)
    - [Install the SSL certificate](#install-the-ssl-certificate)
    - [For Windows Users (PowerShell)](#for-windows-users-powershell)
    - [For macOS and Linux Users (Bash)](#for-macos-and-linux-users-bash)
    - [Verification](#verification)
    - [Update hosts file with those lines](#update-hosts-file-with-those-lines)
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

<http://localhost:15672/>

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

### Install the SSL certificate

Upon successful start, you will find a new folder named `oneground-certificates` inside `localdev` containing the following files:

- `oneground.local.pem` - The public certificate
- `oneground.local.key` - The private key
- `oneground.local.combined.pem` - A combination of the key and certificate

To make your browser and system trust the generated certificate, you need to install it into your system's trust store.

### For Windows Users (PowerShell)

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

### For macOS and Linux Users (Bash)

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

### Verification

After installation, it is recommended to restart your web browser.

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
