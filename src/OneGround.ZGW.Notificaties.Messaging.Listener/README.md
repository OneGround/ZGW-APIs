# OneGround Notificaties Listener

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/notificaties-listener:<version>
 ```

The complete list of available versions for the OneGround Notificaties Listener is maintained on their [GitHub versions page](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-listener/versions).

## About

This is the official container image for the **OneGround Notificaties Listener**. It's an open-source project that implements a background listener.

## What is OneGround Notificaties Listener?

This OneGround implementation provides a centralized, automated mechanism for capturing and processing the full stream of events from across the ZGW ecosystem. Designed as a universal subscriber, the Notificaties Listener connects directly to the Notificaties API to consume messages published by other core components, including the Zaken API, Documenten API, and Besluiten API. This allows for a comprehensive, real-time overview of all significant activities, such as case creation, decision registration, or document updates.

By providing a single point for monitoring and logging, this listener is essential for building robust audit trails, enabling system-wide analytics, and ensuring transparent government operations in compliance with VNG standards.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## Dependencies

The OneGround Notificaties Listener depends on:

- [OneGround Notificaties API](https://github.com/OneGround/ZGW-APIs/pkgs/container/notificaties-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/notificaties-listener:<version>
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
