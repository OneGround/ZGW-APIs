# Docker Hosted Services

- [Docker Hosted Services](#docker-hosted-services)
  - [Docker hosted tools \& services](#docker-hosted-tools--services)
    - [Postgres](#postgres)
    - [RabbitMQ](#rabbitmq)
  - [Docker hosted ZGW APIs](#docker-hosted-zgw-apis)
    - [Start ZGW APIs](#start-zgw-apis)
    - [Stop ZGW APIs](#stop-zgw-apis)
    - [Zaken Api](#zaken-api)
    - [Documenten Api](#documenten-api)
    - [Autorisaties Api](#autorisaties-api)
    - [Catalogi Api](#catalogi-api)
    - [Besluiten Api](#besluiten-api)
    - [Notificaties Api](#notificaties-api)
    - [Referentielijsten Api](#referentielijsten-api)

## Docker hosted tools & services

### Postgres

Postgres including all zgw services in docker

```txt
servername: zgw_db
server port: 5432
database(s): zrc_db
```

Postgres stand-alone

```txt
servername: postgres_docker_db
server port: 5432
database(s): zrc_db
```

### RabbitMQ

<http://localhost:15672/>

## Docker hosted ZGW APIs

### Start ZGW APIs

Run command from localdev folder

```txt
docker compose --project-directory .\ --env-file .\.env -f docker-compose.oneground.yml up -d
```

### Stop ZGW APIs

Run command from localdev folder

```txt
docker compose --project-directory .\ -f docker-compose.oneground.yml down
```

### Zaken Api

- **Port:** 5005
- **Swagger:** <http://localhost:5005/swagger>
- **Health:** <http://localhost:5005/health>

### Documenten Api

- **Port:** 5007
- **Swagger:** <http://localhost:5007/swagger>
- **Health:** <http://localhost:5007/health>

### Autorisaties Api

- **Port:** 5009
- **Swagger:** <http://localhost:5009/swagger>
- **Health:** <http://localhost:5009/health>

### Catalogi Api

- **Port:** 5010
- **Swagger:** <http://localhost:5010/swagger>
- **Health:** <http://localhost:5010/health>

### Besluiten Api

- **Port:** 5013
- **Swagger:** <http://localhost:5013/swagger>
- **Health:** <http://localhost:5013/health>

### Notificaties Api

- **Port:** 5015
- **Swagger:** <http://localhost:5015/swagger>
- **Health:** <http://localhost:5015/health>

### Referentielijsten Api

- **Port:** 5018
- **Swagger:** <http://localhost:5018/swagger>
- **Health:** <http://localhost:5018/health>
