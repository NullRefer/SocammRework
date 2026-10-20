-- Socamm重工线业务主表，保留原字段，不增加历史表。
CREATE TABLE IF NOT EXISTS `flow` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `productCode` VARCHAR(64) NOT NULL,
    `unScrewCaves` VARCHAR(48),
    `locations` VARCHAR(640),
    `resultJson` TEXT,
    `isDeleted` INT NOT NULL DEFAULT 0,
    `createTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    `updateTime` DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `ix_flow_code_online` (`productCode`, `isDeleted`, `id`),
    KEY `ix_flow_time` (`createTime`, `id`)
) ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE=UTF8MB4_UNICODE_CI;
