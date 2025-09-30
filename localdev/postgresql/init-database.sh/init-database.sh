#!/bin/sh
set -e

# Keycloak
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
	CREATE DATABASE keycloak
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "keycloak" <<-EOSQL
    CREATE SCHEMA IF NOT EXISTS keycloak AUTHORIZATION postgres;
EOSQL

# OneGround
grant_permissions() {
    local db_name=$1
    local admin_role="oneground_admin"
    local user_role="oneground_user"

    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db_name" <<-EOSQL
        GRANT CREATE, USAGE ON SCHEMA public TO ${admin_role};
        GRANT USAGE ON SCHEMA public TO ${user_role};

        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE, TRUNCATE, TRIGGER ON TABLES TO ${admin_role};
        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO ${user_role};

        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT SELECT, USAGE, UPDATE ON SEQUENCES TO ${admin_role};
        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT SELECT, USAGE ON SEQUENCES TO ${user_role};

        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO ${admin_role};
        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT EXECUTE ON FUNCTIONS TO ${user_role};

        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT USAGE ON TYPES TO ${admin_role};
        ALTER DEFAULT PRIVILEGES FOR ROLE ${admin_role} IN SCHEMA public GRANT USAGE ON TYPES TO ${user_role};
EOSQL
}

grant_hangfire_permissions() {
    local db_name=$1
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db_name" <<-EOSQL
        CREATE SCHEMA IF NOT EXISTS hangfire AUTHORIZATION oneground_user;
        GRANT ALL ON SCHEMA hangfire TO oneground_admin;
        GRANT ALL ON ALL TABLES IN SCHEMA hangfire TO oneground_admin;
        GRANT ALL ON ALL SEQUENCES IN SCHEMA hangfire TO oneground_admin;
        ALTER DEFAULT PRIVILEGES FOR ROLE oneground_admin IN SCHEMA hangfire GRANT ALL ON TABLES TO oneground_admin;
        ALTER DEFAULT PRIVILEGES FOR ROLE oneground_admin IN SCHEMA hangfire GRANT ALL ON SEQUENCES TO oneground_admin;
EOSQL
}

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE ROLE oneground_admin WITH LOGIN PASSWORD 'oneground_admin' NOSUPERUSER INHERIT NOCREATEDB NOCREATEROLE NOREPLICATION;
    CREATE ROLE oneground_user WITH LOGIN PASSWORD 'oneground_user' NOSUPERUSER INHERIT NOCREATEDB NOCREATEROLE NOREPLICATION;
EOSQL

DATABASES="ac_db brc_db drc_db nrc_db zrc_db ztc_db"

for db in $DATABASES; do
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "CREATE DATABASE $db;"
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$db" <<-EOSQL
        CREATE EXTENSION IF NOT EXISTS postgis;
        CREATE EXTENSION IF NOT EXISTS pgcrypto;
EOSQL
    grant_permissions "$db"
done

grant_hangfire_permissions "nrc_db"