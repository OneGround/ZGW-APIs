FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

ARG Version=1.0.0

WORKDIR /src

COPY --link . .

RUN dotnet restore src/Roxit.ZGW.Referentielijsten.WebApi/ZGW.Referentielijsten.WebApi.csproj -p:PublishReadyToRun=true -r linux-x64

RUN dotnet publish src/Roxit.ZGW.Referentielijsten.WebApi/ZGW.Referentielijsten.WebApi.csproj --self-contained false -c Release -o /app/publish --no-restore -p:Version=$Version /p:PublishReadyToRun=true -r linux-x64 

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

WORKDIR /app

COPY --from=build /app/publish .

RUN apt-get update -yq \
&& apt-get install curl -yq \
&& rm -rf /var/lib/apt/lists/*

USER $APP_UID

HEALTHCHECK --interval=60s --timeout=5s --retries=3 CMD curl -k -f http://localhost:5000/health || exit 1

ENTRYPOINT ["./Roxit.ZGW.Referentielijsten.WebApi"]