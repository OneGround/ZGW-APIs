#!/bin/bash
echo "Regenerating Keycloak client..."
dotnet kiota generate --clean-output -o ./src --openapi https://www.keycloak.org/docs-api/26.3.1/rest-api/openapi.json --language csharp -c KeycloakClient -n KeycloakSetup.Client \
  --include-path "/admin/realms" \
  --include-path "/admin/realms/{realm-name}" \
  --include-path "/admin/realms/{realm-name}/clients" \
  --include-path "/admin/realms/{realm-name}/clients/{id}/protocol-mappers/models"
echo "Keycloak client regeneration completed."