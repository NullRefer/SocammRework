/*主流程表*/
DROP TABLE IF EXISTS `flow`;
CREATE TABLE `flow` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `productCode` VARCHAR(64) NOT NULL COMMENT '产品码',
    `unScrewCaves` VARCHAR(48) COMMENT '需要拆除的穴位列表多个用英文逗号分隔，例如: 1,2,3',
    `locations` VARCHAR(640) COMMENT '需要拆除产品位置用英文逗号分隔，例如: J172,L168',
    `resultJson` VARCHAR(1024) COMMENT '穴拆螺丝结果json',
    `isDeleted` INT NOT NULL DEFAULT '0' COMMENT '逻辑删除 默认 0 - 正常; 1 - 已删除',
    `updateTime` DATETIME DEFAULT CURRENT_TIMESTAMP COMMENT '更新时间',
    `createTime` DATETIME DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (`id`),
    KEY `idx_isDeleted` (`isDeleted`) COMMENT '软删除索引'
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='主流程表';
