FROM postgis/postgis:13-3.5

RUN apt-get update && apt-get install -y \
    postgresql-13-wal2json && \
    rm -rf /var/lib/apt/lists/*

RUN echo "wal_level = logical" >> /usr/share/postgresql/postgresql.conf.sample
