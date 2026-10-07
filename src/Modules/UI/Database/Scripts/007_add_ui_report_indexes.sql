-- UI report indexes; apply to the configured UI database after 006.
BEGIN;
CREATE INDEX idx_wallet_profile_intents_latest_profile
    ON public.wallet_profile_intents (profile_addr, created_at DESC, id DESC) INCLUDE (wallet_addr);
CREATE INDEX idx_ton_connections_last_connected
    ON public.ton_connections (last_connected_at DESC, wallet_addr)
    WHERE last_connected_at IS NOT NULL;
COMMIT;
