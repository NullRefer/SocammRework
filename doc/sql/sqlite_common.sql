-- Framework base schema from Tm.Ant 3.2.0 package revision 0c69d2f3aeb84fb2ee3587ed95f6945e0fa9a866.
-- New database initialization only; does not migrate existing incompatible tables.
CREATE TABLE IF NOT EXISTS bom_config (
    id             INTEGER  NOT NULL
                            PRIMARY KEY AUTOINCREMENT,
    bomNumber      TEXT     NOT NULL,
    bomName        TEXT     NOT NULL,
    model          TEXT     NOT NULL,
    brand          TEXT     NOT NULL,
    unit           TEXT     NOT NULL
                            DEFAULT '',
    theoryLifetime REAL     NOT NULL,
    lifetimeUnit   TEXT     NOT NULL
                            DEFAULT '万次',
    createTime     DATETIME NOT NULL
                            DEFAULT (datetime('now', 'localtime') ),
    updateTime     DATETIME NOT NULL
                            DEFAULT (datetime('now', 'localtime') ),
    isDeleted      INTEGER  NOT NULL
                            DEFAULT '0'
);

CREATE TABLE IF NOT EXISTS bom_in_machine (
    id                   INTEGER  NOT NULL
                                  PRIMARY KEY AUTOINCREMENT,
    bomNumber            TEXT     NOT NULL,
    machineName          TEXT     NOT NULL,
    moduleName           TEXT     NOT NULL,
    count                INTEGER  NOT NULL
                                  DEFAULT '1',
    position             INTEGER  NOT NULL
                                  DEFAULT '0',
    lifetimeDecrement    REAL     NOT NULL
                                  DEFAULT '1',
    actualLifetime       REAL     NOT NULL
                                  DEFAULT '0',
    archivedLifetime     REAL     NOT NULL
                                  DEFAULT '0',
    archivedProductivity REAL     NOT NULL
                                  DEFAULT '0',
    isEnable             INTEGER  NOT NULL
                                  DEFAULT '1',
    createTime           DATETIME NOT NULL
                                  DEFAULT (datetime('now', 'localtime') ),
    updateTime           DATETIME NOT NULL
                                  DEFAULT (datetime('now', 'localtime') ),
    isDeleted            INTEGER  NOT NULL
                                  DEFAULT 0
);

CREATE TABLE IF NOT EXISTS bom_maintain_record (
    id                         INTEGER  NOT NULL
                                        PRIMARY KEY AUTOINCREMENT,
    bomInMachineBeforeId       INTEGER  NOT NULL,
    bomNumberBefore            TEXT     NOT NULL,
    position                   INTEGER  NOT NULL
                                        DEFAULT '0',
    theoryLifetimeStringBefore TEXT     NOT NULL,
    actualLifetimeBefore       REAL     NOT NULL
                                        DEFAULT '0',
    usedLifetimeBefore         REAL     NOT NULL
                                        DEFAULT '0',
    bomInMachineAfterId        INTEGER  NOT NULL,
    bomNumberAfter             TEXT     NOT NULL,
    actualLifetimeAfter        REAL     NOT NULL
                                        DEFAULT '0',
    productivitySnapshot       INTEGER  NOT NULL,
    operationType              TEXT     NOT NULL,
    operatorName               TEXT     NOT NULL,
    reason                     TEXT     NOT NULL,
    createTime                 DATETIME NOT NULL
                                        DEFAULT (datetime('now', 'localtime') ),
    updateTime                 DATETIME NOT NULL
                                        DEFAULT (datetime('now', 'localtime') ),
    isDeleted                  INTEGER  NOT NULL
                                        DEFAULT 0
);

CREATE TABLE IF NOT EXISTS oee_hour_produce (
    id            INTEGER  NOT NULL
                           PRIMARY KEY AUTOINCREMENT,
    flowName      TEXT     NOT NULL,
    okQty         INTEGER  NOT NULL
                           DEFAULT '0',
    ngQty         INTEGER  NOT NULL
                           DEFAULT '0',
    throwQty      INTEGER  NOT NULL
                           DEFAULT '0',
    recordTime    DATETIME NOT NULL,
    hour          INTEGER  NOT NULL
                           DEFAULT '0',
    timeMarker    TEXT     NOT NULL,
    runDuration   INTEGER  NOT NULL
                           DEFAULT '0',
    alarmDuration INTEGER  NOT NULL
                           DEFAULT '0',
    waitDuration  INTEGER  NOT NULL
                           DEFAULT '0',
    energyJson    TEXT     NOT NULL
                           DEFAULT '',
    createTime    DATETIME NOT NULL
                           DEFAULT (datetime('now', 'localtime') ),
    updateTime    DATETIME NOT NULL
                           DEFAULT (datetime('now', 'localtime') ),
    isDeleted     INTEGER  NOT NULL
                           DEFAULT 0
);

CREATE TABLE IF NOT EXISTS phm_alarm_mapping (
    id                      INTEGER  NOT NULL
                                     PRIMARY KEY AUTOINCREMENT,
    flowName                TEXT     NOT NULL,
    code                    INTEGER  NOT NULL,
    bizAlarmCode            TEXT     NOT NULL,
    description             TEXT     NOT NULL,
    reason                  TEXT     NOT NULL
                                     DEFAULT '',
    solution                TEXT     NOT NULL
                                     DEFAULT '',
    machinePictureName      TEXT     NOT NULL
                                     DEFAULT '',
    machineAlarmLocation    TEXT     NOT NULL
                                     DEFAULT '',
    elementAlarmPictureName TEXT     NOT NULL
                                     DEFAULT '',
    elementAlarmLocation    TEXT     NOT NULL
                                     DEFAULT '',
    createTime              DATETIME NOT NULL
                                     DEFAULT (datetime('now', 'localtime') ),
    updateTime              DATETIME NOT NULL
                                     DEFAULT (datetime('now', 'localtime') ),
    isDeleted               INTEGER  NOT NULL
                                     DEFAULT 0
);

CREATE TABLE IF NOT EXISTS phm_alarm_record (
    id         INTEGER  NOT NULL
                        PRIMARY KEY AUTOINCREMENT,
    flowName   TEXT     NOT NULL,
    code       INTEGER  NOT NULL,
    duration   INTEGER  NOT NULL
                        DEFAULT '0',
    startTime  DATETIME NOT NULL,
    endTime    DATETIME NOT NULL,
    isUploaded INTEGER  NOT NULL
                        DEFAULT '0',
    createTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    updateTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    isDeleted  INTEGER  NOT NULL
                        DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sfc_check (
    id                INTEGER  NOT NULL
                               PRIMARY KEY AUTOINCREMENT,
    productCode       TEXT     NOT NULL,
    requestId         TEXT     NOT NULL,
    billNo            TEXT     NOT NULL
                               DEFAULT '',
    product           TEXT     NOT NULL
                               DEFAULT '',
    phase             TEXT     NOT NULL
                               DEFAULT '',
    config            TEXT     NOT NULL
                               DEFAULT '',
    stationId         TEXT     NOT NULL
                               DEFAULT '',
    lineName          TEXT     NOT NULL
                               DEFAULT '',
    sfcRepairParaInfo TEXT     NOT NULL
                               DEFAULT '',
    createTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    updateTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    isDeleted         INTEGER  NOT NULL
                               DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sfc_enter (
    id            INTEGER  NOT NULL
                           PRIMARY KEY AUTOINCREMENT,
    productCode   TEXT     NOT NULL,
    billNo        TEXT     NOT NULL
                           DEFAULT '',
    hashCode      TEXT     NOT NULL
                           DEFAULT '',
    equipmentNo   TEXT     NOT NULL
                           DEFAULT '',
    equipmentType TEXT     NOT NULL
                           DEFAULT '',
    station       TEXT     NOT NULL
                           DEFAULT '',
    passURL       TEXT     NOT NULL
                           DEFAULT '',
    repairURL     TEXT     NOT NULL
                           DEFAULT '',
    createTime    DATETIME NOT NULL
                           DEFAULT (datetime('now', 'localtime') ),
    updateTime    DATETIME NOT NULL
                           DEFAULT (datetime('now', 'localtime') ),
    isDeleted     INTEGER  NOT NULL
                           DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sys_dict_config (
    id         INTEGER  NOT NULL
                        PRIMARY KEY AUTOINCREMENT,
    [key]      TEXT     NOT NULL
                        UNIQUE,
    value      TEXT     NOT NULL
                        DEFAULT '',
    createTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    updateTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    isDeleted  INTEGER  NOT NULL
                        DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sys_event_log (
    id         INTEGER  NOT NULL
                        PRIMARY KEY AUTOINCREMENT,
    flowName   TEXT     NOT NULL,
    eventType  TEXT     NOT NULL
                        DEFAULT '',
    username   TEXT     NOT NULL
                        DEFAULT '',
    message    TEXT     NOT NULL,
    createTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    updateTime DATETIME NOT NULL
                        DEFAULT (datetime('now', 'localtime') ),
    isDeleted  INTEGER  NOT NULL
                        DEFAULT 0
);

CREATE TABLE IF NOT EXISTS sys_api_log (
    id                INTEGER  NOT NULL
                               PRIMARY KEY AUTOINCREMENT,
    sourceTag         TEXT     NOT NULL
                               DEFAULT '',
    url               TEXT     NOT NULL,
    requestBody       TEXT     NOT NULL,
    responseBody      TEXT     NOT NULL,
    statusCode        INTEGER  NOT NULL
                               DEFAULT 0,
    isSuccess         INTEGER  NOT NULL
                               DEFAULT 0,
    errorMessage      TEXT     NOT NULL
                               DEFAULT '',
    retryCount        INTEGER  NOT NULL
                               DEFAULT 0,
    createTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    updateTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    isDeleted         INTEGER  NOT NULL
                               DEFAULT 0
);

CREATE INDEX IF NOT EXISTS idx_sys_api_log_sourceTag ON sys_api_log (sourceTag);
CREATE INDEX IF NOT EXISTS idx_sys_api_log_isSuccess ON sys_api_log (isSuccess);
CREATE INDEX IF NOT EXISTS idx_sys_api_log_createTime ON sys_api_log (createTime);

CREATE TABLE IF NOT EXISTS sys_user_info (
    id                  INTEGER  NOT NULL
                                 PRIMARY KEY AUTOINCREMENT,
    flowName            TEXT     NOT NULL,
    staffId             TEXT     NOT NULL,
    username            TEXT     NOT NULL,
    department          TEXT     NOT NULL,
    role                TEXT     NOT NULL
                                 DEFAULT '',
    job                 TEXT     NOT NULL
                                 DEFAULT '',
    profession          TEXT     NOT NULL
                                 DEFAULT '',
    isKeyWorker         INTEGER  NOT NULL
                                 DEFAULT '0',
    isMachineWorker     INTEGER  NOT NULL
                                 DEFAULT '0',
    fingerprintTemplates TEXT     NOT NULL,
    passwordHash        TEXT     NOT NULL
                                 DEFAULT '',
    passwordSalt        TEXT     NOT NULL
                                 DEFAULT '',
    passwordIterations  INTEGER  NOT NULL
                                 DEFAULT '0',
    passwordAlgorithm   TEXT     NOT NULL
                                 DEFAULT '',
    failedPasswordLoginCount INTEGER  NOT NULL
                                 DEFAULT '0',
    isLocked            INTEGER  NOT NULL
                                 DEFAULT '0',
    createTime          DATETIME NOT NULL
                                 DEFAULT (datetime('now', 'localtime') ),
    updateTime          DATETIME NOT NULL
                                 DEFAULT (datetime('now', 'localtime') ),
    isDeleted           INTEGER  NOT NULL
                                 DEFAULT 0
);

CREATE TABLE IF NOT EXISTS te_faca_config (
    id                          INTEGER  NOT NULL
                                         PRIMARY KEY AUTOINCREMENT,
    primaryClassificationCode   TEXT     NOT NULL,
    primaryClassification       TEXT     NOT NULL,
    secondaryClassificationCode TEXT     NOT NULL,
    secondaryClassification     TEXT     NOT NULL,
    reclassify                  TEXT     NOT NULL
                                         NOT NULL
                                         DEFAULT '',
    errorCode                   TEXT     NOT NULL
                                         NOT NULL
                                         DEFAULT '',
    defectClassification        TEXT     NOT NULL
                                         DEFAULT '',
    sortLabel1                  TEXT     NOT NULL
                                         DEFAULT '',
    errorDescription            TEXT     NOT NULL
                                         DEFAULT '',
    sortLabel2                  TEXT     NOT NULL
                                         DEFAULT '',
    faultCause                  TEXT     NOT NULL
                                         DEFAULT '',
    faultHandling               TEXT     NOT NULL
                                         DEFAULT '',
    createTime                  DATETIME NOT NULL
                                         DEFAULT (datetime('now', 'localtime') ),
    updateTime                  DATETIME NOT NULL
                                         DEFAULT (datetime('now', 'localtime') ),
    isDeleted                   INTEGER  NOT NULL
                                         DEFAULT 0
);

CREATE TABLE IF NOT EXISTS te_faca_maintain (
    id                INTEGER  NOT NULL
                               PRIMARY KEY AUTOINCREMENT,
    flowName          TEXT     NOT NULL,
    alarmCode         TEXT     NOT NULL,
    alarmInfo         TEXT     NOT NULL
                               DEFAULT '',
    failureReason     TEXT     NOT NULL
                               DEFAULT '',
    failureSolution   TEXT     NOT NULL
                               DEFAULT '',
    alarmStartTime    DATETIME NOT NULL,
    alarmEndTime      DATETIME NOT NULL,
    alarmDuration     INTEGER  NOT NULL
                               DEFAULT '0',
    maintainStartTime DATETIME NOT NULL,
    maintainEndTime   DATETIME NOT NULL,
    maintainDuration  INTEGER  NOT NULL
                               DEFAULT '0',
    empNo             TEXT     NOT NULL
                               DEFAULT '',
    empName           TEXT     NOT NULL
                               DEFAULT '',
    department        TEXT     NOT NULL
                               DEFAULT '',
    phmAlarmRecordId  INTEGER  NOT NULL,
    createTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    updateTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    isDeleted         INTEGER  NOT NULL
                               DEFAULT 0
);

CREATE TABLE IF NOT EXISTS te_time_detail (
    id              INTEGER  NOT NULL
                             PRIMARY KEY AUTOINCREMENT,
    flowName        TEXT     NOT NULL,
    nodeType        INTEGER  NOT NULL
                             DEFAULT '1',
    sortNo          INTEGER  NOT NULL
                             DEFAULT '0',
    plcStartAddress TEXT     NOT NULL,
    ctDetails       TEXT     NOT NULL,
    createTime      DATETIME NOT NULL
                             DEFAULT (datetime('now', 'localtime') ),
    updateTime      DATETIME NOT NULL
                             DEFAULT (datetime('now', 'localtime') ),
    isDeleted       INTEGER  NOT NULL
                             DEFAULT 0
);-- 机台节能记录表 （定时从plc取数据更新插入该表）

CREATE TABLE IF NOT EXISTS sys_client (
    id                INTEGER  NOT NULL
                               PRIMARY KEY AUTOINCREMENT,
    flowName          TEXT     NOT NULL,
    ip                TEXT     NOT NULL,
    currentVersion    TEXT     NOT NULL,
    latestVersion     TEXT     NOT NULL,
    latestPackageUrl  TEXT     NOT NULL,
    latestVersionRemark TEXT   NOT NULL DEFAULT '',
    currentVersionAvailability TEXT NOT NULL DEFAULT '',
    currentVersionRemark TEXT  NOT NULL DEFAULT '',
    previousAvailableVersion TEXT NOT NULL DEFAULT '',
    previousAvailableVersionRemark TEXT NOT NULL DEFAULT '',
    isUpdateTriggered INTEGER  NOT NULL,
    createTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    updateTime        DATETIME NOT NULL
                               DEFAULT (datetime('now', 'localtime') ),
    isDeleted         INTEGER  NOT NULL
                               DEFAULT 0
);-- 客户端版本管理表

CREATE TABLE IF NOT EXISTS local_fault_record (
    id              INTEGER  NOT NULL
                             PRIMARY KEY AUTOINCREMENT,
    code            INTEGER,
    faultContent    TEXT,
    startTime       DATETIME,
    endTime         DATETIME,
    duration        INTEGER,
    createTime      DATETIME NOT NULL
                             DEFAULT (datetime('now', 'localtime') ),
    updateTime      DATETIME NOT NULL
                             DEFAULT (datetime('now', 'localtime') ),
    isDeleted       INTEGER  NOT NULL
                             DEFAULT 0
);
