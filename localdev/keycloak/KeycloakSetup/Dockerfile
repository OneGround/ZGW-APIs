FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

COPY . /src

WORKDIR /src/KeycloakSetup

RUN dotnet publish KeycloakSetup.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["./KeycloakSetup"] 