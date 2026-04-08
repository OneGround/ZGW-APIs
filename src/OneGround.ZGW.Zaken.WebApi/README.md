# OneGround Zaken API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/zaken-api:<version>
 ```

The complete list of available versions for the OneGround Zaken API is maintained on their [GitHub versions page](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api/versions).

> **⚠️ Security Notice: DataProtection Key Storage**
>
> By default, ASP.NET Core DataProtection keys are stored in the **same PostgreSQL database** as application data, separated only by a dedicated `data_protection` schema. Without additional configuration, these keys are stored **as plaintext**, which could expose sensitive data if the database is compromised.
>
> **Secure alternatives:**
>
> - **Certificate encryption** — Set `DataProtection:Certificate` to a Base64-encoded PFX certificate and optionally `DataProtection:CertificatePassword` to protect keys at rest.
> - **Azure Key Vault** — Use Azure Key Vault to manage and protect DataProtection keys.
> - **File-system storage** — Store keys on the file system with restricted OS-level permissions to limit access.
>
> For more information, see the [Microsoft ASP.NET Core DataProtection documentation](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/introduction).

## About

This is the official container image for the **OneGround Zaken API**. It's an open-source project that implements the official [VNG Zaken API](https://vng-realisatie.github.io/gemma-zaken/standaard/zaken) standard used in the Netherlands.

## What is OneGround Zaken API?

This OneGround implementation provides a standardized interface for registering and managing government cases, forming the core of any case-oriented working solution. Designed to be the central hub of the ZGW ecosystem, the Zaken API orchestrates processes by integrating seamlessly with other components. It uses the Catalogi API to define case types, links evidence from the Documenten API, connects formal outcomes from the Besluiten API, and broadcasts status updates via the Notificaties API. By creating a single, authoritative record for each case, the OneGround Zaken API ensures that all related information is centrally organized, auditable, and transparent.

Adherence to VNG standards promotes interoperability and a unified approach to process management, creating a cohesive data landscape for government operations.

## ZGW API Versions Supported by OneGround

You can find all supported versions in the [OneGround API Versions list](https://dev.oneground.nl/docs/api-versions).

## Dependencies

The OneGround Zaken API depends on:

- [OneGround Autorisaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- [OneGround Besluiten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/besluiten-api)
- [OneGround Catalogi API](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- [OneGround Documenten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)
- [OneGround Documenten Listener](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-listener)
- [OneGround Notificaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/zaken-api:<version>
```

## Configuration

For a real-world setup, you will need to provide environment variables for connecting to a database and other services. It's recommended to use docker-compose for a more robust setup.

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the project, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose).

## Feedback

To provide feedback on this project visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
