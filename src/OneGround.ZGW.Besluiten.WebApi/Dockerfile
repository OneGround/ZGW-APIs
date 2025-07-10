FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS build

ARG Version=1.0.0

WORKDIR /src

COPY --link . .

RUN dotnet restore src/OneGround.ZGW.Besluiten.WebApi/ZGW.Besluiten.WebApi.csproj -p:PublishReadyToRun=true -r linux-x64

RUN dotnet publish src/OneGround.ZGW.Besluiten.WebApi/ZGW.Besluiten.WebApi.csproj --self-contained false -c Release -o /app/publish --no-restore -p:Version=$Version /p:PublishReadyToRun=true -r linux-x64

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-bookworm-slim

LABEL org.opencontainers.image.source="https://github.com/OneGround/ZGW-APIs" \
      org.opencontainers.image.description="ZGW Besluiten Web API for OneGround, providing decision-making services as part of the ZGW APIs suite." \
      org.opencontainers.image.licenses="BSD-3-Clause"

WORKDIR /app

COPY --from=build /app/publish .

RUN apt-get update -yq \
&& apt-get install curl -yq \
&& rm -rf /var/lib/apt/lists/*

USER $APP_UID

HEALTHCHECK --interval=60s --timeout=5s --retries=3 CMD curl -k -f http://localhost:5000/health || exit 1

ENTRYPOINT ["./OneGround.ZGW.Besluiten.WebApi"]
