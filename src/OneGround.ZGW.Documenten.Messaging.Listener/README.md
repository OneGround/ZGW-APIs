# OneGround Documenten Listener

## Featured Tags

 ```bash
 docker pull ghcr.io/oneground/documenten-listener:<version>
 ```

You can retrieve a list of all available tags for OneGround Documenten Listener in our [GitHub tags](https://github.com/OneGround/ZGW-APIs/tags).

## About

This is the official container image for the **OneGround Documenten Listener**. It's an open-source project that implements background listener.

## What is OneGround Documenten Listner?

This OneGround implementation provides an automated mechanism that actively listens for and processes notifications about document-related events. Designed to work in conjunction with the core ZGW components, the Documenten Listener subscribes to the Notificaties API to receive real-time alerts whenever a document is created, updated, or otherwise modified within the Documenten API. This enables automated, event-driven workflows such as virus scanning, metadata extraction (OCR), or archiving, ensuring that essential background tasks are performed reliably and efficiently.

By leveraging the VNG standard for notifications, this listener promotes a decoupled, resilient, and highly automated approach to document management within the government's IT landscape.

For more details and implementation guidelines, visit the [OneGround ZGW APIs GitHub repository](https://github.com/OneGround/ZGW-APIs).

## Dependencies

The OneGround Documenten Listener depends on:

- [OneGround Documenten API](https://github.com/OneGround/ZGW-APIs/pkgs/container/documenten-api)

## How to use this image

```bash
docker run -it -p 8080:80 ghcr.io/oneground/documenten-listner:<version>
```

## Support

View issues related to this image in our [GitHub issues](https://github.com/OneGround/ZGW-APIs/issues).

If you encounter any bugs or security issues with the project, please file an issue in our [GitHub repo](https://github.com/OneGround/ZGW-APIs/issues/new/choose).

## Feedback

To provide feedback on this project visit our [GitHub repository](https://github.com/OneGround/ZGW-APIs) to file issues, open pull requests, contribute to discussions, and more.

## License

This project is licensed under the **BSD 3-Clause License**. See the [LICENSE](https://github.com/OneGround/ZGW-APIs/blob/main/LICENSE) file for more details.
