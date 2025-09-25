#!/bin/sh
set -e

# OneGround
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE ROLE oneground_admin WITH
    LOGIN
    PASSWORD 'oneground_admin'
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION;

    CREATE ROLE oneground_user WITH
    LOGIN
    PASSWORD 'oneground_user'
    NOSUPERUSER
    NOCREATEDB
    NOCREATEROLE
    NOREPLICATION;
EOSQL

psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    CREATE DATABASE ac_db;
    CREATE DATABASE brc_db;
    CREATE DATABASE drc_db;
    CREATE DATABASE nrc_db;
    CREATE DATABASE zrc_db;
    CREATE DATABASE ztc_db;
    CREATE DATABASE keycloak;
EOSQL

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

grant_permissions "ac_db"
grant_permissions "brc_db"
grant_permissions "drc_db"
grant_permissions "nrc_db"
grant_permissions "zrc_db"
grant_permissions "ztc_db"

grant_hangfire_permissions "nrc_db"

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "nrc_db" <<-EOSQL
#     CREATE SCHEMA IF NOT EXISTS hangfire AUTHORIZATION oneground_user;
#     GRANT ALL ON SCHEMA hangfire TO oneground_admin;
#     GRANT ALL ON ALL TABLES IN SCHEMA hangfire TO oneground_admin;
#     GRANT ALL ON ALL SEQUENCES IN SCHEMA hangfire TO oneground_admin;
#     ALTER DEFAULT PRIVILEGES FOR ROLE oneground_admin IN SCHEMA hangfire GRANT ALL ON TABLES TO oneground_admin;
#     ALTER DEFAULT PRIVILEGES FOR ROLE oneground_admin IN SCHEMA hangfire GRANT ALL ON SEQUENCES TO oneground_admin;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "ac_db" <<-EOSQL
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "brc_db" <<-EOSQL
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "drc_db" <<-EOSQL
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "nrc_db" <<-EOSQL
#     CREATE SCHEMA hangfire AUTHORIZATION oneground_user;
#     ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire GRANT USAGE ON SEQUENCES TO oneground_user;
#     ALTER DEFAULT PRIVILEGES IN SCHEMA hangfire GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "zrc_db" <<-EOSQL
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "ztc_db" <<-EOSQL
#     ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO oneground_user;
# EOSQL

# Keycloak
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "keycloak" <<-EOSQL
    CREATE SCHEMA keycloak AUTHORIZATION postgres;
    ALTER DEFAULT PRIVILEGES IN SCHEMA keycloak GRANT USAGE ON SEQUENCES TO postgres;
    ALTER DEFAULT PRIVILEGES IN SCHEMA keycloak GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO postgres;
EOSQL

