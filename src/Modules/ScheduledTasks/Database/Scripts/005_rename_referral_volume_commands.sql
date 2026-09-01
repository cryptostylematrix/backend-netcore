-- Renames existing scheduled Program volume commands after deploying the
-- profile-volume implementation. Command order and all other JSON fields are
-- preserved.

BEGIN;

UPDATE public.tasks task
SET commands =
(
    SELECT jsonb_agg(
        CASE command.value ->> 'type'
            WHEN 'program.structure.calculate-personal-volume' THEN
                jsonb_set(
                    command.value,
                    '{type}',
                    '"program.structure.calculate-referral-volume"'::jsonb)
            WHEN 'program.structure.reset-personal-volume' THEN
                jsonb_set(
                    command.value,
                    '{type}',
                    '"program.structure.reset-referral-volume"'::jsonb)
            ELSE command.value
        END
        ORDER BY command.ordinality)
    FROM jsonb_array_elements(task.commands)
        WITH ORDINALITY AS command(value, ordinality)
)
WHERE EXISTS
(
    SELECT 1
    FROM jsonb_array_elements(task.commands) AS command(value)
    WHERE command.value ->> 'type' IN
    (
        'program.structure.calculate-personal-volume',
        'program.structure.reset-personal-volume'
    )
);

COMMIT;
