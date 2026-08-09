CREATE TABLE IF NOT EXISTS wgt_local_device (
    singleton_key INTEGER NOT NULL PRIMARY KEY CHECK (singleton_key = 1),
    device_identity TEXT NOT NULL,
    display_name TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS wgt_service_integrations (
    service_id TEXT NOT NULL PRIMARY KEY,
    global_enablement TEXT NOT NULL CHECK (global_enablement IN ('enabled', 'disabled'))
);

CREATE TABLE IF NOT EXISTS wgt_service_integration_device_overrides (
    service_id TEXT NOT NULL,
    device_identity TEXT NOT NULL,
    enablement TEXT NOT NULL CHECK (enablement IN ('enabled', 'disabled')),
    PRIMARY KEY (service_id, device_identity),
    FOREIGN KEY (service_id) REFERENCES wgt_service_integrations(service_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS wgt_integration_publications (
    service_id TEXT NOT NULL PRIMARY KEY,
    display_name TEXT NOT NULL,
    published_at_utc TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS wgt_capability_publications (
    service_id TEXT NOT NULL,
    capability_id TEXT NOT NULL,
    title TEXT NOT NULL,
    contract_version TEXT NOT NULL,
    ordinal INTEGER NOT NULL,
    PRIMARY KEY (service_id, capability_id),
    UNIQUE (service_id, ordinal),
    FOREIGN KEY (service_id) REFERENCES wgt_integration_publications(service_id) ON DELETE CASCADE
);
