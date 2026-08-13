-- Removes all data belonging to the test CryptoCash referral program.
-- Run while connected to the correct programs database as the table owner
-- or as a role with DELETE permission on all affected tables.
--
-- The referal_program row is removed as well, allowing
-- setup_test_cryptocash_program.sql to be run again afterward.

BEGIN;

DO $$
DECLARE
    v_marketing_addr text :=
        'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
BEGIN
    DELETE FROM public.locks
    WHERE marketing_addr = v_marketing_addr;

    DELETE FROM public.places
    WHERE marketing_addr = v_marketing_addr;

    DELETE FROM public.structures
    WHERE marketing_addr = v_marketing_addr;

    DELETE FROM public.referal_program
    WHERE marketing_addr = v_marketing_addr;
END;
$$;

COMMIT;

-- Verification: every count should be zero.
SELECT 'referal_program' AS entity, COUNT(*) AS remaining
FROM public.referal_program
WHERE marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO'

UNION ALL

SELECT 'structures', COUNT(*)
FROM public.structures
WHERE marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO'

UNION ALL

SELECT 'places', COUNT(*)
FROM public.places
WHERE marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO'

UNION ALL

SELECT 'locks', COUNT(*)
FROM public.locks
WHERE marketing_addr =
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO';
