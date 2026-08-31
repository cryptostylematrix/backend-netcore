-- Removes direct access granted to PUBLIC and other non-owner application roles,
-- then grants one backend PostgreSQL role only the permissions required by
-- Scheduled Tasks. Role memberships are cluster-wide and are not changed.
-- Run as the postgres user while connected to the database named below.

DO $$
DECLARE
    v_database_name text := '';
    v_backend_role text := '';
    v_database_owner text;
    v_role_to_revoke text;
BEGIN
    IF btrim(v_database_name) = '' THEN
        RAISE EXCEPTION
            'Set v_database_name to the Tasks database before running this script.';
    END IF;

    IF btrim(v_backend_role) = '' THEN
        RAISE EXCEPTION
            'Set v_backend_role to the backend PostgreSQL username before running this script.';
    END IF;

    IF current_database() <> v_database_name THEN
        RAISE EXCEPTION
            'Connect Query Tool to database % before running this script. Current database: %.',
            v_database_name,
            current_database();
    END IF;

    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_roles
        WHERE rolname = v_backend_role
    ) THEN
        RAISE EXCEPTION 'PostgreSQL role % does not exist.', v_backend_role;
    END IF;

    SELECT pg_get_userbyid(datdba)
    INTO v_database_owner
    FROM pg_database
    WHERE datname = v_database_name;

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON DATABASE %I FROM PUBLIC',
        v_database_name);

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON SCHEMA public FROM PUBLIC');

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM PUBLIC');

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM PUBLIC');

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM PUBLIC');

    FOR v_role_to_revoke IN
        SELECT rolname
        FROM pg_roles
        WHERE rolname <> v_backend_role
          AND rolname <> v_database_owner
          AND rolname !~ '^pg_'
    LOOP
        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON DATABASE %I FROM %I',
            v_database_name,
            v_role_to_revoke);

        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON SCHEMA public FROM %I',
            v_role_to_revoke);

        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM %I',
            v_role_to_revoke);

        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM %I',
            v_role_to_revoke);

        EXECUTE format(
            'REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM %I',
            v_role_to_revoke);
    END LOOP;

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON DATABASE %I FROM %I',
        v_database_name,
        v_backend_role);

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON SCHEMA public FROM %I',
        v_backend_role);

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL TABLES IN SCHEMA public FROM %I',
        v_backend_role);

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public FROM %I',
        v_backend_role);

    EXECUTE format(
        'REVOKE ALL PRIVILEGES ON ALL FUNCTIONS IN SCHEMA public FROM %I',
        v_backend_role);

    EXECUTE format(
        'GRANT CONNECT ON DATABASE %I TO %I',
        v_database_name,
        v_backend_role);

    EXECUTE format(
        'GRANT USAGE ON SCHEMA public TO %I',
        v_backend_role);

    EXECUTE format(
        'GRANT SELECT, UPDATE ON TABLE public.tasks TO %I',
        v_backend_role);
END;
$$;
