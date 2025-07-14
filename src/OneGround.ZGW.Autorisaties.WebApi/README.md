# OneGround Autorisaties API

## Featured Tags

- ```docker pull ghcr.io/oneground/autorisaties-api:<version>```

## About

This is the official container image for the **OneGround Autorisaties API**. It's an open-source project that implements the official [VNG Autorisaties API](https://vng-realisatie.github.io/gemma-zaken/standaard/autorisaties) standard used in the Netherlands.

## What is OneGround Autorisaties API?

This OneGround implementation provides a standardized interface to define and verify user and application permissions, ensuring that only authorized entities can access or modify case-related information. It is designed to integrate seamlessly with other core ZGW components like the Zaken API, Documenten API, Catalogi API and Besluiten API.

For more details and implementation guidelines, visit the main [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## How to Use This Image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/autorisaties-api:<version>
```

## Configuration

For a real-world setup, you will need to provide environment variables for connecting to a database and other services. It's recommended to use docker-compose for a more robust setup.

## Full Tag Listing

You can retrieve a list of all available tags for OneGround Autorisaties API in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags)

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the project, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose)

## Feedback

To provide feedback on this project visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
