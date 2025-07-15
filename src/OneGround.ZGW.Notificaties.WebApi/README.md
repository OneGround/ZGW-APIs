# OneGround Notificaties API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/notificaties-api:<version>
 ```

You can retrieve a list of all available tags for OneGround Notificaties API in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags)

## About

This is the official container image for the **OneGround Notificaties API**. It's an open-source project that implements the official [VNG Notificaties API](https://vng-realisatie.github.io/gemma-zaken/standaard/notificaties) standard used in the Netherlands.

## What is OneGround Notificaties API?

This OneGround implementation provides a standardized interface that functions as a central message bus for publishing and subscribing to event notifications. Designed to be the core communication channel for the ZGW ecosystem, the Notificaties API allows components like the Zaken API, Documenten API, and Besluiten API to broadcast significant events as they occur. By enabling a decoupled, event-driven architecture, this API allows other applications and listeners to subscribe and react to these events asynchronously. This approach enhances system resilience, scalability, and flexibility.

By adhering to the VNG standard for notifications, the OneGround Notificaties API ensures that all inter-system communication is reliable and standardized, creating a responsive and interoperable data landscape for government operations.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## ZGW API Versions Supported by OneGround

You can find all supported versions in the [OneGround API Versions list](https://dev.oneground.nl/docs/api-versions).

## Dependencies

The OneGround Notificaties API depends on:

- [OneGround Autorisaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api)
- [OneGround Documenten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)
- [OneGround Documenten Listener](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-listener)
- [OneGround Notificaties Listener](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-listener)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/notificaties-api:<version>
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
