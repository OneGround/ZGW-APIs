FROM postgres:13-bookworm

# Install PostGIS 3 and wal2json
RUN apt-get update && apt-get install -y \
    postgresql-13-postgis-3 \
    postgresql-13-postgis-3-scripts \
    postgresql-13-wal2json \
    && rm -rf /var/lib/apt/lists/*

RUN echo "wal_level = logical" >> /usr/share/postgresql/postgresql.conf.sample
