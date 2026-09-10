-- Add status column to app_permissions (idempotent via INSERT OR IGNORE trick)
-- SQLite does not support IF NOT EXISTS for ALTER TABLE,
-- so we just run it and handle errors in the initializer.
ALTER TABLE app_permissions ADD COLUMN status TEXT NOT NULL DEFAULT 'granted';

-- Backfill: rows with granted=1 get status='granted', granted=0 get status='denied'
UPDATE app_permissions SET status = CASE WHEN granted = 1 THEN 'granted' ELSE 'denied' END WHERE status = 'granted' AND granted = 0;
