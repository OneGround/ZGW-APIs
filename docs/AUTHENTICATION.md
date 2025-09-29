# Authentication for OneGround ZGW APIs

This guide explains how to authenticate against the ZGW APIs during local development and demos.

## Supported token types

- **OAuth2 access tokens (recommended)**
  - Issued via standard flows (e.g. client credentials as in this guide).
  - Tokens are verified by checking the signature with the Identity Provider's public keys (JWKS), enabling offline validation and reducing network calls.
  - Tokens are issued in one place (the Identity Provider), so you can centrally enforce short expiration times, rotate keys, and revoke compromised clients.
  - Any standards‑compliant OAuth2 Identity Provider can be used to issue access tokens for these APIs. Keycloak is used in this repository only as an example.
  - Each access token must include an `rsin` claim that contains the organization's RSIN. APIs use this claim for tenant/organization context. In this local Keycloak setup, `rsin` claim is added via hardcoded claim mapper on the api client. When using a different Identity Provider, configure an equivalent claim/attribute mapping so that issued access tokens contain the `rsin` claim.

- **ZGW standard tokens (legacy/backwards compatibility)**
  - Self‑issued JWTs signed with HS256 (HMAC‑SHA256) using the Keycloak client's secret.
  - Must include a `client_id` claim that matches a client in the Keycloak realm.
  - Supported only with Keycloak together with the custom token introspection plugin. See the project for details: [Keycloak-ZGW-Token-Introspection](https://github.com/OneGround/Keycloak-ZGW-Token-Introspection).
  - Use short token lifetimes. Tokens without an `exp` claim are treated as active, but it's not recommended to use non expiring access tokens.

## API Authentication using OAuth2 access tokens

### Get the Client Secret from Keycloak

1. Navigate to the Keycloak admin console: [http://localhost:8080/admin/master/console/#/OneGround/](http://localhost:8080/admin/master/console/#/OneGround/)
2. Log in using the credentials:
    - **Username**: `admin`
    - **Password**: `admin`
3. From the navigation on the left, select **Clients**.
4. Select the `oneground-000000000` client from the list.
    > **Note on the Default Client:** This local setup is configured with a single default client, `oneground-000000000`, which has full administrative access to all APIs. If you wish to add more clients with specific permissions, you must first create them in Keycloak by following the [Keycloak Setup Guide](./localdev/keycloak/KeycloakSetup/README.md). After creating a new client, you must also configure its permissions using the Autorisaties API or by updating the [autorisaties service's seed data](./localdev/oneground-services-data/ac-data/applicaties.json).
5. Go to the **Credentials** tab.
6. Copy the value from the **Client Secret** field. This is your `<oneground-client-secret>`.

### Request an Access Token

Now you can exchange the client credentials for a temporary access token. Use the command for your operating system, replacing `<oneground-client-secret>` with your actual secret. The default client ID is `oneground-000000000`.

#### For Windows (PowerShell)

- Open Windows PowerShell and execute this command:

    ```powershell
    $response = Invoke-WebRequest `
        -Uri "http://localhost:8080/realms/OneGround/protocol/openid-connect/token" `
        -Method POST `
        -Headers @{"Content-Type" = "application/x-www-form-urlencoded"} `
        -Body "grant_type=client_credentials&client_id=oneground-000000000&client_secret=<oneground-client-secret>"
    ```

- Then take an access token from `$response`:

    ```powershell
    $response.Content
    ```

#### For Linux, macOS, or WSL (cURL)

- Open terminal and execute this command:

```bash
curl --location --request POST 'http://localhost:8080/realms/OneGround/protocol/openid-connect/token' \
--header 'Content-Type: application/x-www-form-urlencoded' \
--data-urlencode 'grant_type=client_credentials' \
--data-urlencode 'client_id=oneground-000000000' \
--data-urlencode 'client_secret=<oneground-client-secret>'
```

You will receive a JSON response containing the `access_token`. You can now use this token as a `Bearer` token to authorize your API requests.

```json
{
    "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCIgOiAi......",
    "expires_in": 300,
    "refresh_expires_in": 0,
    "token_type": "Bearer"
}
```

> **Tip: How to Increase Token Expiration Time (For Testing Only)**
>
> **Warning:** Extending access token lifespans reduces security. Long-lived tokens are easier to steal and misuse and increase the impact of any leak because they remain valid for longer. Only increase token lifespans for local testing in non-production environments.
> By default, the access token expires in 5 minutes (300 seconds). To increase this time:
>
> 1. Navigate directly to the **Tokens** settings page in Keycloak: [http://localhost:8080/admin/master/console/#/OneGround/realm-settings/tokens](http://localhost:8080/admin/master/console/#/OneGround/realm-settings/tokens).
> 2. In the `Access Token Lifespan` field, set a longer duration (e.g., `30 minutes` or `1 hour`).
> 3. Click **Save**.
>
> You will need to request a new token for this change to take effect.
