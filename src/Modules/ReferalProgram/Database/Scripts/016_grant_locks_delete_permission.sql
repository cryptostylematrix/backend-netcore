-- Required by unlock-position command processing.
-- Replace the role when applying this to a differently named development user.

GRANT SELECT, INSERT, DELETE
    ON TABLE public.locks
    TO cs_programs_user;
