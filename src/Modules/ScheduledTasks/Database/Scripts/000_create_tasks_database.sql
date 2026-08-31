-- Run this script once as the postgres user in Query Tool while connected to
-- the postgres database. CREATE DATABASE cannot run inside a transaction, so
-- execute this statement by itself. The postgres user remains the database
-- owner; 001_create_tasks.sql grants the backend role only its runtime rights.
--
-- For development, change cs_tasks to dev_cs_tasks before execution.
-- PostgreSQL has no CREATE DATABASE IF NOT EXISTS; an existing database error
-- means this step has already been completed.

CREATE DATABASE cs_tasks;
