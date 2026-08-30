-- Examples only. Replace identifiers, addresses, and UTC timestamps before use.

-- Every five seconds. The first execution is controlled by execute_at_utc.
INSERT INTO public.tasks (id, execute_at_utc, schedule, commands)
VALUES
(
    gen_random_uuid(),
    '2030-01-01T00:00:00Z',
    '{"type":"interval","unit":"seconds","value":5}'::jsonb,
    '[
        {
            "module":"program",
            "type":"program.structure.update-activity",
            "version":1,
            "target":{"marketingAddress":"EQ_REPLACE_ME"},
            "arguments":{"structureNumber":1}
        }
    ]'::jsonb
);

-- Every three months on the 15th at 00:00 UTC.
INSERT INTO public.tasks (id, execute_at_utc, schedule, commands)
VALUES
(
    gen_random_uuid(),
    '2030-01-15T00:00:00Z',
    '{
        "type":"calendar",
        "unit":"months",
        "interval":3,
        "dayOfMonth":15,
        "timeUtc":"00:00:00"
    }'::jsonb,
    '[
        {
            "module":"program",
            "type":"program.structure.compress",
            "version":1,
            "target":{"marketingAddress":"EQ_REPLACE_ME"},
            "arguments":{"structureNumber":1}
        }
    ]'::jsonb
);

-- Stop a task without deleting it:
-- UPDATE public.tasks SET execute_at_utc = NULL, updated_at_utc = CURRENT_TIMESTAMP
-- WHERE id = 'REPLACE_ME';
