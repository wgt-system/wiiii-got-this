CREATE TABLE IF NOT EXISTS wgt_publication_refresh_states (
    service_id TEXT NOT NULL PRIMARY KEY,
    last_attempted_at_utc TEXT NOT NULL,
    latest_result TEXT NOT NULL CHECK (latest_result IN ('refreshed', 'adapter_failed', 'invalid_publication')),
    last_successful_refresh_at_utc TEXT NULL
);
