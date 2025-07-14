@echo off
echo Regenerating Keycloak client...
dotnet kiota generate --clean-output -o ./src --openapi https://www.keycloak.org/docs-api/latest/rest-api/openapi.json --language csharp -c KeycloakClient -n Keycloak.Client

echo Keycloak client updated successfully!