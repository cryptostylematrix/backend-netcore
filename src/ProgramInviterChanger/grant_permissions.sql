-- Run this as postgres or as the owner of public.places and public.locks.
-- Replace the value below with the database role used by ProgramInviterChanger.
DO
$$
DECLARE
    v_username name := 'PROGRAM_INVITER_CHANGER_USER';
    v_can_select_places boolean;
    v_can_update_places boolean;
    v_can_select_locks boolean;
    v_can_update_locks boolean;
BEGIN
    IF NOT EXISTS
    (
        SELECT 1
        FROM pg_catalog.pg_roles
        WHERE rolname = v_username
    ) THEN
        RAISE EXCEPTION 'Database role % does not exist.', v_username;
    END IF;

    EXECUTE format(
        'GRANT SELECT, UPDATE ON TABLE public.places, public.locks TO %I',
        v_username);

    SELECT
        has_table_privilege(v_username, 'public.places', 'SELECT'),
        has_table_privilege(v_username, 'public.places', 'UPDATE'),
        has_table_privilege(v_username, 'public.locks', 'SELECT'),
        has_table_privilege(v_username, 'public.locks', 'UPDATE')
    INTO
        v_can_select_places,
        v_can_update_places,
        v_can_select_locks,
        v_can_update_locks;

    IF NOT
    (
        v_can_select_places
        AND v_can_update_places
        AND v_can_select_locks
        AND v_can_update_locks
    ) THEN
        RAISE EXCEPTION 'Permission verification failed for role %.', v_username;
    END IF;

    RAISE NOTICE
        'Verified ProgramInviterChanger permissions for role %: places SELECT=%, places UPDATE=%, locks SELECT=%, locks UPDATE=%.',
        v_username,
        v_can_select_places,
        v_can_update_places,
        v_can_select_locks,
        v_can_update_locks;
END
$$;
