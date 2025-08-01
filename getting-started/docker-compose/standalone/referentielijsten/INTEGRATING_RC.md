# OneGround Referentielijsten API (RC) - Standalone Deployment

This repository contains a standalone Docker Compose setup for the **OneGround Referentielijsten API (RC)**. This guide provides all the necessary steps to get the API running.

## Table of Contents

- [OneGround Referentielijsten API (RC) - Standalone Deployment](#oneground-referentielijsten-api-rc---standalone-deployment)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Getting Started](#getting-started)
    - [Step 1: Obtain the Setup Files](#step-1-obtain-the-setup-files)
    - [Step 2: Understand the Files](#step-2-understand-the-files)
  - [Choosing an Image Version](#choosing-an-image-version)
  - [Running the Service](#running-the-service)
  - [Contributing](#contributing)
  - [License](#license)

---

## Prerequisites

Before you begin, ensure you have the following components installed on your local machine:

- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/install/)

---

## Getting Started

### Step 1: Obtain the Setup Files

Download the necessary configuration files as a ZIP archive and extract them to your local machine.

[**Download the `standalone/RC` directory**](https://download-directory.github.io/?url=https%3A%2F%2Fgithub.com%2FOneGround%2FZGW-APIs%2Ftree%2Fmain%2Fgetting-started%2F%2Fdocker-compose%2F%2Fstandalone%2F%2FRC)

After extracting, navigate into the created directory.

### Step 2: Understand the Files

You will be working with two primary files for this setup:

- `.env`: A template for optional environment variables. You will copy this to `.env`.
- `docker-compose.yml`: Defines the RC service, maps the port, and loads optional environment variables.

---

## Choosing an Image Version

The `docker-compose.yml` file is configured to use a specific version of the `referentielijsten-api` image (e.g., `1.0`). You can browse all available versions and select a different one if needed.

A complete list of published image versions can be found on the [**GitHub Packages page**](https://github.com/OneGround/ZGW-APIs/pkgs/container/referentielijsten-api).

To use a different version, simply update the `image` tag in your `docker-compose.yml` file.

---

## Running the Service

Once you are in the directory containing the `docker-compose.yml` file, start the service in detached mode from your terminal:

```bash
docker compose up -d
```

To verify that the service has started correctly and to follow its log output, use the following command:

```bash
docker compose logs -f
```

If the startup is successful, the Referentielijsten API will be running and accessible on port **5018** of your Docker host (e.g., `http://localhost:5018`). You can change this port mapping in the `docker-compose.yml` file if needed.

---

## Contributing

We welcome contributions to improve this project! To get started, please read our [contributing guidelines](https://github.com/OneGround/ZGW-APIs/blob/main/CONTRIBUTING.md).

---

## License

This project is licensed under the **MIT License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for details.
