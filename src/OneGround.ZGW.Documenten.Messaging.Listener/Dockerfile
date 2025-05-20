FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

ARG Version=1.0.0

WORKDIR /src

COPY --link . .

RUN dotnet restore src/OneGround.ZGW.Documenten.Messaging.Listener/ZGW.Documenten.Messaging.Listener.csproj -p:PublishReadyToRun=true -r linux-x64

RUN dotnet publish src/OneGround.ZGW.Documenten.Messaging.Listener/ZGW.Documenten.Messaging.Listener.csproj --self-contained false -c Release -o /app/publish --no-restore -p:Version=$Version /p:PublishReadyToRun=true -r linux-x64

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

WORKDIR /app

COPY --from=build /app/publish .

USER $APP_UID

ENTRYPOINT ["./OneGround.ZGW.Documenten.Messaging.Listener"]
