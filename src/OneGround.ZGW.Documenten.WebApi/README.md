# OneGround Documenten API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/documenten-api:<version>
 ```

You can retrieve a list of all available tags for OneGround Documenten API in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags)

## About

This is the official container image for the **OneGround Documenten API**. It's an open-source project that implements the official [VNG Documenten API](https://vng-realisatie.github.io/gemma-zaken/standaard/documenten) standard used in the Netherlands.

## What is OneGround Documenten API?

This OneGround implementation provides a standardized interface for storing, managing, and retrieving documents and other information objects. Designed for seamless integration with other core ZGW components such as the Zaken API, Besluiten API, and Catalogi API, this process ensures that all relevant documents are securely linked to their corresponding cases and decisions. By centralizing document management, the OneGround Documenten API guarantees a single source of truth, making information easily accessible, auditable, and secure.

Adherence to VNG standards promotes interoperability and a unified approach to data management, creating a cohesive data landscape for government operations.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## ZGW API Versions Supported by OneGround

You can find all supported versions in the [OneGround API Versions list](https://dev.oneground.nl/docs/api-versions).

## Dependencies

The OneGround Besluiten API depends on:

- [OneGround Autorisaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- [OneGround Besluiten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/besluiten-api)
- [OneGround Catalogi API](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- [OneGround Notificaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)
- [OneGround Zaken API](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/documenten-api:<version>
```

## Configuration

For a real-world setup, you will need to provide environment variables for connecting to a database and other services. It's recommended to use docker-compose for a more robust setup.

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the project, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose)

## Feedback

To provide feedback on this project visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
