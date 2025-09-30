# OneGround ZGW-APIs

## Production-ready APIs for Case Management (Zaakgericht Werken) in C#

[![C#](https://img.shields.io/badge/c%23-%23239120.svg?style=for-the-badge&logo=c-sharp&logoColor=white)](https://docs.microsoft.com/en-us/dotnet/csharp/)
[![.NET](https://img.shields.io/badge/.NET-%235C2D91.svg?style=for-the-badge&logo=.net&logoColor=white)](https://dotnet.microsoft.com/)

## About OneGround ZGW-APIs

This repository contains the source code and documentation for production-ready APIs implementing the VNG Standards for Case Management (Zaakgericht Werken).
This is a C# implementation of the ZGW-APIs standard.

## Case Management (Zaakgericht Werken)

Case Management (Zaakgericht Werken) is a process-oriented work method used by Dutch municipalities and increasingly by national government bodies to handle requests from citizens and businesses. This implementation provides the necessary APIs to support this way of working in a common ground architecture. Common ground architecture originally consisted of 5 layers:

- UI (not in this project)
- process logic (not in this project)
- middle ware (not in this project)
- APIs including business rules (in this project)
- storage (in this project)

## API Standards Implementation

This implementation follows the VNG Realisatie standard "APIs for Zaakgericht Werken" and includes the following core APIs:

- **Catalogi API** - For registering case type catalogs, case types, and all related types
- **Zaken API** - For case registration, including relationships with documents, decisions, and contacts
- **Documenten API** - For registration of information objects (documents, photos, videos, etc.)
- **Besluiten API** - For registration of decisions made in the context of case management

Supporting APIs:

- **Notifications API** - For managing subscriptions and notifications of data changes
- **Authorization API** - For managing application access to data
- **Reference API** - Process types and result types for archiving based on selection lists

## Technical Features

- **Built with C# and .NET** - Utilizing modern C# features and .NET best practices
- **Microservices Architecture** - Each API is designed as an independent microservice
- **Docker Support** - Containerized deployment ready
- **Swagger/OpenAPI** - Complete API documentation using Swagger/OpenAPI
- **Authentication & Authorization** - Security implementation according to the standard (machine to machine)
- **Audit Trail** - Logging of changes according to the standard
- **Archiving Support** - Built-in support for archiving according to Dutch standards
- **Ceph Support** - Built-in support for storage of document contents on Ceph
- **Multi-tenant** - One installation can serve multiple instances or organisations

## Getting Started

### Prerequisites

- **.NET**: 8.0
- **Docker**: Version 24.0.0 or higher
- **Docker Compose**: Version 2.20.0 or higher

### Installation

1. Clone the repository:

    ```bash
    git clone https://github.com/OneGround/ZGW-APIs.git
    cd ZGW-APIs
    ```

2. Build the solution:

    ```bash
    dotnet build ./src/ZGW.all.sln
    ```

3. Run the tests:

    ```bash
    dotnet test ./src/ZGW.UnitTests.slnf
    ```

### Docker Deployment

We offer two primary methods for running the ZGW APIs using Docker:

- **Running from Docker Images:** For a quick and easy setup, follow our recommended [Getting Started guide](./getting-started/docker-compose/README.md).
- **Running from Source Code:** For local development or contributing to the project, follow our [localdev setup guide](./localdev/README.md).

## Configuration

Configuration can be done through:

- appsettings.json files
- Environment variables
- Docker environment files

For more information, visit our [documentation portal](https://dev.oneground.nl).

## Project Structure

- src: source code
- localdev: everything needed to set up your own development environment inclusing Ceph and Databases

## Documentation

Complete documentation is available at:
[https://dev.oneground.nl](https://dev.oneground.nl)

### Additional Documentation

- **[General Documentation](./docs/README.md)** - Additional documentation resources
- **[Authentication Guide](./docs/AUTHENTICATION.md)** - Complete guide for authenticating against the ZGW APIs
- **[Logging Configuration](./docs/LOGS.md)** - Detailed logging strategy using Serilog

## Contributing

We welcome contributions to the OneGround ZGW-APIs. Please read our [contribution guidelines](https://dev.oneground.nl) for details on our code of conduct and the process for submitting pull requests.

## Links

- [VNG Standards for ZGW-APIs](https://vng-realisatie.github.io/gemma-zaken/)
- [OneGround Documentation](https://dev.oneground.nl)

## Support

For support questions, please contact:

- Email: <support@oneground.nl>
- [Create an issue](https://github.com/OneGround/ZGW-APIs/issues)

## Acknowledgments

- VNG Realisatie for the ZGW-APIs standard
- The Dutch municipalities contributing to the standard
- The open-source community for inspiration and tools

---

### Built with pleasure by Roxit
