# OneGround Besluiten API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/besluiten-api:<version>
 ```

You can retrieve a list of all available tags for OneGround Autorisaties API in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags)

## About

This is the official container image for the **OneGround Besluiten API**. It's an open-source project that implements the official [VNG Besluiten API](https://vng-realisatie.github.io/gemma-zaken/standaard/besluiten) standard used in the Netherlands.

## What is OneGround Besluiten API?

This OneGround implementation provides a standardized interface for registering and managing decisions, allowing applications to create, access, and link them directly to corresponding cases and information objects. Designed for seamless integration with other core ZGW components such as the Catalogi API, Documenten API, Notificaties API, and Zaken API, this process ensures that decision-making is transparent, auditable, and integrated with other government systems. By adhering to VNG standards, the OneGround Besluiten API promotes interoperability and a unified approach to data management, creating a cohesive data landscape for government operations.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## Dependencies

The OneGround Zaken API depends on:

- [OneGround Autorisaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- [OneGround Catalogi API](https://github.com/OneGround/ZGW-APIs/pkgs/container/catalogi-api)
- [OneGround Documenten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)
- [OneGround Documenten Listener](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-listener)
- [OneGround Notificaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)
- [OneGround Zaken API](https://github.com/OneGround/ZGW-APIs/pkgs/container/zaken-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/besluiten-api:<version>
```

## Configuration

For a real-world setup, you will need to provide environment variables for connecting to a database and other services. It's recommended to use docker-compose for a more robust setup.

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the tool, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose)

## Feedback

To provide feedback on this tool visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
