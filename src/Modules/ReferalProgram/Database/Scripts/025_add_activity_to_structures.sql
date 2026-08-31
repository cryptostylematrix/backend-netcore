BEGIN;

ALTER TABLE public.structures
    ADD COLUMN activity jsonb NULL;

-- CryptoCash structures 1-4 already expose activate_place on chain.
UPDATE public.structures
SET activity = '{"set_active_on_activation": true}'::jsonb
WHERE marketing_addr IN
(
    'EQAba1dNyAbxm4t_dv5T1ARQXaQAAYcfJ4jcAWcw1PQ7q10b',
    'EQCFZmVrYR-tLGIWDHjBb-Oyk1tcePk2_ThcytEZA08dNLbO'
)
AND structure_number BETWEEN 1 AND 4;

COMMIT;
