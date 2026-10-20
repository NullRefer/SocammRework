-- Socamm重工线业务主表，仅用于新库初始化。
CREATE TABLE IF NOT EXISTS flow (
    id INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    productCode TEXT NOT NULL,
    unScrewCaves TEXT,
    locations TEXT,
    resultJson TEXT DEFAULT '[]',
    isDeleted INTEGER NOT NULL DEFAULT 0,
    createTime DATETIME NOT NULL DEFAULT (datetime('now', 'localtime')),
    updateTime DATETIME NOT NULL DEFAULT (datetime('now', 'localtime'))
);
CREATE INDEX IF NOT EXISTS ix_flow_code_online ON flow(productCode, isDeleted, id);
CREATE INDEX IF NOT EXISTS ix_flow_time ON flow(createTime, id);
CREATE TRIGGER IF NOT EXISTS flow_update_time AFTER UPDATE ON flow
WHEN NEW.updateTime = OLD.updateTime
BEGIN
    UPDATE flow SET updateTime = datetime('now', 'localtime') WHERE id = NEW.id;
END;
