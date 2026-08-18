BEGIN;

ALTER TABLE public.wallet_profile_intent_events
    DROP CONSTRAINT IF EXISTS wallet_profile_intent_events_type_check;

ALTER TABLE public.wallet_profile_intent_events
    ADD CONSTRAINT wallet_profile_intent_events_type_check
        CHECK (event_type IN (
            'added',
            'removed',
            'ownership_lost',
            'ownership_gained'
        ));

COMMIT;
