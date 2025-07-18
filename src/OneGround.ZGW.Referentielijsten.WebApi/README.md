# OneGround Referentielijsten API

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/referentielijsten-api:<version>
 ```

The complete list of available versions for the OneGround Referentielijsten API is maintained on their [GitHub versions page](https://github.com/OneGround/ZGW-APIs/pkgs/container/referentielijsten-api/versions).

## About

This is the official container image for the **OneGround Referentielijsten API**. It's an open-source project that implements the official [VNG Referentielijsten API](https://redocly.github.io/redoc/?url=https://raw.githubusercontent.com/VNG-Realisatie/VNG-referentielijsten/master/src/openapi.yaml&nocors) standard used in the Netherlands.

## What is OneGround Referentielijsten API?

This OneGround implementation provides a standardized interface for managing and serving controlled vocabularies, also known as reference lists. Designed to ensure data consistency across the entire ZGW landscape, this API offers a single source of truth for simple, predefined lists. By allowing other components and applications to retrieve these standardized values, the Referentielijsten API prevents data entry errors and discrepancies in systems like the Zaken API and Documenten API.

Adherence to VNG standards promotes data integrity and interoperability, creating a more reliable and unified data landscape for government operations.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/referentielijsten-api:<version>
```

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the project, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose).

## Feedback

To provide feedback on this project visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
