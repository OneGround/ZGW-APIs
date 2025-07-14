# Keycloak Setup Automation Tool

This tool automates the initial configuration of Keycloak for OneGround IDP setup.

## What it does

The tool performs the following setup steps automatically:

1. **Creates a new realm**
2. **Creates an OIDC clients** with the specified configuration
3. **Creates a hardcoded claim mapper** to include RSIN in access tokens

## Prerequisites

- .NET SDK
- Running Keycloak instance (default: http://localhost:8080)
- Keycloak admin credentials

## Configuration
Edit the `appsettings.json` file in the project directory:

```json
{
  "Keycloak": {
    "BaseUrl": "http://localhost:8080",
    "AdminUsername": "admin",
    "AdminPassword": "admin",
    "RealmName": "OneGround",
    "Clients": [
      {
        "ClientId": "oneground-000000000",
        "ClientName": "Oneground API",
        "ClientDescription": "OIDC client for OneGround APIs",
        "Rsin": "000000000"
      }
    ]
  }
}
```

#### Client Configuration

Each client in the `Clients` array has the following properties:

- **ClientId**: Unique identifier for the client (used in OAuth flows)
- **ClientName**: Human-readable name for the client
- **ClientDescription**: Description of the client's purpose
- **Rsin**: Organization number (string) associated with the client

The tool will create:
- A separate OIDC client for each entry in the `Clients` array
- Hardcoded claim mappers directly on each client
- RSIN claims in access tokens with the organization's RSIN value

#### How RSIN Claims Work

The tool automatically configures RSIN claims by:
1. Adding a hardcoded claim mapper directly to each client with the RSIN value
2. Configuring the mapper to include the RSIN claim in access tokens, ID tokens, and userinfo tokens
3. The mapper is automatically included when the client authenticates

When a client authenticates, the access token will contain an `rsin` claim with the organization's RSIN number.

## Usage

### Build and Run

```bash
# Run the tool
dotnet run
```

## Error Handling

- The tool checks if resources already exist before creating them
- All API errors are logged with detailed information
- The tool returns appropriate exit codes (0 for success, 1 for failure)
- If a step fails, the tool stops and reports the error

## Using the Configured Keycloak

After running the setup tool, you can use the configured Keycloak instance to obtain access tokens using the client credentials flow.

### Client Credentials Flow

To get an access token from the created client:

```bash
curl -X POST \
  http://localhost:8080/realms/OneGround/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=oneground-000000000" \
  -d "client_secret=YOUR_CLIENT_SECRET"
```

The response will contain an access token that includes the RSIN claim:

```json
{
  "access_token": "eyJ...",
  "token_type": "Bearer",
  "expires_in": 300
}
```

### Alternative Identity Providers

**Important**: This Keycloak setup is just an example configuration. You can use any identity provider with oauth2 as long as **access tokens contains `rsin` claim with the organization's RSIN number.**




## Security Notes

- Store admin credentials securely in production environments
- Consider using environment variables instead of config files for sensitive data
- The tool requires admin privileges on the Keycloak instance

## Troubleshooting

### Common Issues

1. **Authentication Failed**: Check admin username/password and Keycloak URL
2. **Connection Refused**: Ensure Keycloak is running and accessible