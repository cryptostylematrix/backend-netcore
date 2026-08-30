-- Creates and provisions the dedicated Scheduled Tasks database.
--
-- Run this script with psql while connected to an administrative database.
-- The runtime role must already exist; its password is managed outside SQL files.
-- Override both variables with psql --set for each environment.
-- This script does not create any tables; run 001_create_tasks.sql separately.

\set ON_ERROR_STOP on

\if :{?tasks_database}
\else
    \set tasks_database cs_tasks
\endif

\if :{?tasks_role}
\else
    \set tasks_role cs_tasks_user
\endif

SELECT format('CREATE DATABASE %I', :'tasks_database')
WHERE NOT EXISTS
(
    SELECT 1
    FROM pg_database
    WHERE datname = :'tasks_database'
)
\gexec

SELECT format(
    'REVOKE CONNECT ON DATABASE %I FROM PUBLIC',
    :'tasks_database')
\gexec

SELECT format(
    'GRANT CONNECT ON DATABASE %I TO %I',
    :'tasks_database',
    :'tasks_role')
\gexec

\connect :tasks_database

SELECT format(
    'GRANT USAGE ON SCHEMA public TO %I',
    :'tasks_role')
\gexec

SELECT format(
    'ALTER DEFAULT PRIVILEGES IN SCHEMA public '
    'GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO %I',
    :'tasks_role')
\gexec
