CREATE TABLE IF NOT EXISTS stats (
    day TEXT NOT NULL,
    key TEXT NOT NULL,
    value TEXT NOT NULL,
    version TEXT NOT NULL,
    count INTEGER NOT NULL DEFAULT 0,
    PRIMARY KEY (day, key, value, version)
);

CREATE TABLE IF NOT EXISTS exceptions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    time TEXT NOT NULL,
    version TEXT NOT NULL,
    log TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS exceptions_time ON exceptions (time);

CREATE TABLE IF NOT EXISTS setups (
    code TEXT PRIMARY KEY,
    created TEXT NOT NULL,
    ip_hash TEXT NOT NULL,
    data TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS setups_created ON setups (created);
CREATE INDEX IF NOT EXISTS setups_ip_hash ON setups (ip_hash, created);
