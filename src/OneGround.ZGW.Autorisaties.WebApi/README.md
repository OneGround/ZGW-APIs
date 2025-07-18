# OneGround Autorisaties API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/autorisaties-api:<version>
 ```

The complete list of available versions for the OneGround Autorisaties API is maintained on their [GitHub versions page](https://github.com/OneGround/ZGW-APIs/pkgs/container/autorisaties-api/versions).

## About

This is the official container image for the **OneGround Autorisaties API**. It's an open-source project that implements the official [VNG Autorisaties API](https://vng-realisatie.github.io/gemma-zaken/standaard/autorisaties) standard used in the Netherlands.

## What is OneGround Autorisaties API?

This OneGround implementation provides a standardized interface for managing and enforcing access control policies across all ZGW resources. Designed as a central security component, the Autorisaties API allows applications to define which users or systems have permission to view or modify specific cases, documents, and decisions. It integrates with other core APIs like the Zaken API and Documenten API to ensure that every data request is checked against the established access rights before being processed. By centralizing authorization logic, this API strengthens security, simplifies permission management, and guarantees that data privacy rules are applied consistently.

Adherence to VNG standards promotes a secure and interoperable framework, creating a trustworthy data landscape for government operations.

For more details and implementation guidelines, visit the main [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## ZGW API Versions Supported by OneGround

You can find all supported versions in the [OneGround API Versions list](https://dev.oneground.nl/docs/api-versions).

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/autorisaties-api:<version>
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
