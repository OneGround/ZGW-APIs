# OneGround Catalogi API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/catalogi-api:<version>
 ```

You can retrieve a list of all available tags for OneGround Catalogi API in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags)

## About

This is the official container image for the **OneGround Catalogi API**. It's an open-source project that implements the official [VNG Catalogi API](https://vng-realisatie.github.io/gemma-zaken/standaard/catalogi) standard used in the Netherlands.

## What is OneGround Catalogi API?

This OneGround implementation provides a standardized interface for managing and exposing different types of catalogs, allowing applications to retrieve information about case types, decision types, and information object types. Designed for seamless integration with other core ZGW components such as the Zaken API, Besluiten API, and Documenten API, this process ensures that all government processes are based on the correct and up-to-date definitions. By adhering to VNG standards, the OneGround Catalogi API promotes interoperability and a unified approach to data management, creating a cohesive data landscape for government operations.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## Dependencies

The OneGround Zaken API depends on:

- [OneGround Autorisaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- [OneGround Catalogi API](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- [OneGround Notificaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)
- [OneGround Referentielijsten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/referentielijsten-api)
- [OneGround Zaken API](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/catalogi-api:<version>
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
