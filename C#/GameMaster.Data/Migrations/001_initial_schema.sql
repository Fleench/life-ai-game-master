CREATE TABLE IF NOT EXISTS players (
    id TEXT PRIMARY KEY,
    display_name TEXT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS currency_pools (
    currency_id TEXT PRIMARY KEY,
    balance INTEGER NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS inventory_items (
    item_id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    quantity INTEGER NOT NULL,
    metadata TEXT NOT NULL,
    awarded_by_app TEXT NOT NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS connected_apps (
    app_id TEXT PRIMARY KEY,
    app_name TEXT NOT NULL,
    platform INTEGER NOT NULL,
    api_key_hash TEXT NOT NULL,
    android_uid INTEGER,
    registered_at DATETIME NOT NULL,
    last_seen_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS app_permissions (
    app_id TEXT NOT NULL,
    resource INTEGER NOT NULL,
    action INTEGER NOT NULL,
    granted INTEGER NOT NULL,
    updated_at DATETIME NOT NULL,
    PRIMARY KEY (app_id, resource, action),
    FOREIGN KEY(app_id) REFERENCES connected_apps(app_id) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS paired_devices (
    device_id TEXT PRIMARY KEY,
    device_name TEXT NOT NULL,
    platform INTEGER NOT NULL,
    pair_code_hash TEXT NOT NULL,
    last_sync_at DATETIME NOT NULL,
    sync_vector_clock TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sync_log (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entity_type TEXT NOT NULL,
    entity_id TEXT NOT NULL,
    conflict_details TEXT NOT NULL,
    logged_at DATETIME NOT NULL
);
