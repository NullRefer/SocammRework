DROP TABLE IF EXISTS `bom_config`;
CREATE TABLE `bom_config` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `bomNumber` VARCHAR(128) NOT NULL COMMENT '物料编码',
    `bomName` VARCHAR(128) NOT NULL COMMENT '品名',
    `model` VARCHAR(128) NOT NULL COMMENT '型号',
    `brand` VARCHAR(32) NOT NULL COMMENT '品牌',
    `unit` VARCHAR(16) NOT NULL DEFAULT '' COMMENT '单位',
    `theoryLifetime` DOUBLE NOT NULL COMMENT '理论寿命',
    `lifetimeUnit` VARCHAR(16) NOT NULL DEFAULT '万次' COMMENT '寿命单位',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='设备基础配置表';

DROP TABLE IF EXISTS `bom_in_machine`;
CREATE TABLE `bom_in_machine` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `bomNumber` VARCHAR(128) NOT NULL COMMENT '物料编码',
    `machineName` VARCHAR(128) NOT NULL COMMENT '机台名',
    `moduleName` VARCHAR(128) NOT NULL COMMENT '模组名',
    `count` INT NOT NULL DEFAULT '1' COMMENT '数量',
    `position` INT NOT NULL DEFAULT '0' COMMENT '位置号',
    `lifetimeDecrement` DOUBLE NOT NULL DEFAULT '1' COMMENT '单产量消耗寿命单位',
    `actualLifetime` DOUBLE NOT NULL DEFAULT '0' COMMENT '实际寿命',
    `archivedLifetime` DOUBLE NOT NULL DEFAULT '0' COMMENT '已归档寿命',
    `archivedProductivity` DOUBLE NOT NULL DEFAULT '0' COMMENT '已归档产能',
    `isEnable` TINYINT(1) NOT NULL DEFAULT '1' COMMENT '是否启用',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='机台设备状态表';

DROP TABLE IF EXISTS `bom_maintain_record`;
CREATE TABLE `bom_maintain_record` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `bomInMachineBeforeId` INT NOT NULL COMMENT '机台物料维护前id',
    `bomNumberBefore` VARCHAR(128) NOT NULL COMMENT '机台物料维护前料号',
    `position` INT NOT NULL DEFAULT '0' COMMENT '位置号',
    `theoryLifetimeStringBefore` VARCHAR(128) NOT NULL COMMENT '维护前理论寿命',
    `actualLifetimeBefore` DOUBLE NOT NULL DEFAULT '0' COMMENT '维护前实际寿命',
    `usedLifetimeBefore` DOUBLE NOT NULL DEFAULT '0' COMMENT '维护前使用寿命',
    `bomInMachineAfterId` INT NOT NULL COMMENT '机台物料维护后id',
    `bomNumberAfter` VARCHAR(128) NOT NULL COMMENT '维护后料号',
    `actualLifetimeAfter` DOUBLE NOT NULL DEFAULT '0' COMMENT '维护后实际寿命',
    `productivitySnapshot` INT NOT NULL COMMENT '维护时产量快照',
    `operationType` VARCHAR(16) NOT NULL COMMENT '操作类型: Prolong, Exchange',
    `operatorName` VARCHAR(64) NOT NULL COMMENT '操作人',
    `reason` VARCHAR(128) NOT NULL COMMENT '维护原因',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='维护记录表';

DROP TABLE IF EXISTS `oee_hour_produce`;
CREATE TABLE `oee_hour_produce` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名',
    `okQty` INT NOT NULL DEFAULT '0' COMMENT 'ok 数量',
    `ngQty` INT NOT NULL DEFAULT '0' COMMENT 'ng 数量',
    `throwQty` INT NOT NULL DEFAULT '0' COMMENT '抛辅料数量',
    `recordTime` DATETIME(6) NOT NULL COMMENT '记录日期比如 2024-09-27 12:00:00',
    `hour` INT NOT NULL DEFAULT '0' COMMENT '小时数 时间点比如8',
    `timeMarker` VARCHAR(32) NOT NULL COMMENT '时间标记: 2024111908',
    `runDuration` INT NOT NULL DEFAULT '0' COMMENT '运行持续时长/s',
    `alarmDuration` INT NOT NULL DEFAULT '0' COMMENT '报警持续时长/s',
    `waitDuration` INT NOT NULL DEFAULT '0' COMMENT '等待持续时长/s',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_flowName` (`flowName`),
    KEY `idx_timeMarker` (`timeMarker`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='每小时产能记录表(定时从 plc 取数据更新插入该表)';

DROP TABLE IF EXISTS `phm_alarm_mapping`;
CREATE TABLE `phm_alarm_mapping` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名称',
    `code` INT NOT NULL COMMENT '报警代码',
    `bizAlarmCode` VARCHAR(32) NOT NULL COMMENT '业务告警代码',
    `description` VARCHAR(128) NOT NULL COMMENT '告警描述',
    `reason` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '报警原因',
    `solution` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '解决方案',
    `machinePictureName` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '机台机构图片名称包含后缀例如ld.png',
    `machineAlarmLocation` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '坐标例如:  10,12',
    `elementAlarmPictureName` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '模组里元素图片名称',
    `elementAlarmLocation` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '坐标例如:  10,12',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='报警映射表';

DROP TABLE IF EXISTS `phm_alarm_record`;
CREATE TABLE `phm_alarm_record` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名称',
    `code` INT NOT NULL COMMENT '报警代码',
    `duration` INT NOT NULL DEFAULT '0' COMMENT '持续时长(s)',
    `startTime` DATETIME(6) NOT NULL COMMENT '开始时间',
    `endTime` DATETIME(6) NOT NULL COMMENT '结束时间',
    `isUploaded` INT NOT NULL DEFAULT '0' COMMENT '上传标识',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_flowName` (`flowName`),
    KEY `idx_createTime` (`createTime`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='报警记录数据表';

DROP TABLE IF EXISTS `phm_machine_monitor`;
CREATE TABLE `phm_machine_monitor` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名称',
    `mainStatus` INT NOT NULL DEFAULT '1' COMMENT '主状态 1 - 运行; 2 - 待料; 3 - 报警; -5 - 保养',
    `attachStatus` INT NOT NULL DEFAULT '1' COMMENT '附加状态 1 - 更换耗材  2 - 设备维护保养 3 - 吃饭/休息 4 - 工艺调试',
    `mode` INT NOT NULL DEFAULT '1' COMMENT '模式 1 - 正常 2 - 调试 3 空跑/暖机 ',
    `statusTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) COMMENT '上次状态记录时间',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='机台监控记录表, 当状态发生变化时更新, 如果状态一直没变化, 则 10s 更新一次';

DROP TABLE IF EXISTS `phm_machine_energy`;
CREATE TABLE `phm_machine_energy` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名',
    `ffuDuration` INT NOT NULL DEFAULT '0' COMMENT 'ffu运行持续时长/min',
    `lightDuration` INT NOT NULL DEFAULT '0' COMMENT '照明持续时长/min',
    `lineDuration` INT NOT NULL DEFAULT '0' COMMENT '皮带线运行持续时长/min',
    `ccdDuration` INT NOT NULL DEFAULT '0' COMMENT 'ccd运行持续时长/min',
    `ionDuration` INT NOT NULL DEFAULT '0' COMMENT '离子风持续时长/min',
    `timeMarker` VARCHAR(32) NOT NULL COMMENT '时间按天标记: 20241119',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_flowName` (`flowName`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='机台节能记录表(定时从 plc 取数据更新插入该表)';

DROP TABLE IF EXISTS `sfc_check`;
CREATE TABLE `sfc_check` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `productCode` VARCHAR(64) NOT NULL COMMENT '条码',
    `requestId` VARCHAR(128) NOT NULL COMMENT '请求 id',
    `billNo` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '单据号',
    `product` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '产品码',
    `phase` VARCHAR(128) NOT NULL DEFAULT '' COMMENT 'sfc 阶段',
    `config` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '配置',
    `stationId` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '工位 id',
    `lineName` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '线体名',
    `sfcRepairParaInfo` VARCHAR(8192) NOT NULL DEFAULT '' COMMENT '重工接口返回的paraInfo数组',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_productCode` (`productCode`),
    KEY `idx_isDeleted` (`isDeleted`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='sfc 上料确认表';

DROP TABLE IF EXISTS `sfc_enter`;
CREATE TABLE `sfc_enter` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `productCode` VARCHAR(64) NOT NULL COMMENT '产品码',
    `billNo` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '系统返回的单号',
    `hashCode` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '哈希token代码',
    `equipmentNo` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '设备编号',
    `equipmentType` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '设备类型',
    `station` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '站点名称',
    `passURL` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '过站URL',
    `repairURL` VARCHAR(128) NOT NULL DEFAULT '' COMMENT '重工URL',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_productCode` (`productCode`),
    KEY `idx_isDeleted` (`isDeleted`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='SFC报站数据表';

DROP TABLE IF EXISTS `sys_dict_config`;
CREATE TABLE `sys_dict_config` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `key` VARCHAR(128) NOT NULL COMMENT '字典key',
    `value` VARCHAR(8192) NOT NULL DEFAULT '' COMMENT '配置信息(json)',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='字典配置表';

DROP TABLE IF EXISTS `sys_event_log`;
CREATE TABLE `sys_event_log` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名称',
    `eventType` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '事件类型',
    `username` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '用户名',
    `message` LONGTEXT NOT NULL COMMENT '事件消息',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_flowName` (`flowName`),
    KEY `idx_createTime` (`createTime`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='事件日志表';

DROP TABLE IF EXISTS `sys_api_log`;
CREATE TABLE `sys_api_log` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `sourceTag` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '日志来源标签',
    `url` VARCHAR(512) NOT NULL COMMENT '接口地址',
    `requestBody` LONGTEXT NOT NULL COMMENT '请求体',
    `responseBody` LONGTEXT NOT NULL COMMENT '响应体',
    `statusCode` INT NOT NULL DEFAULT '0' COMMENT 'HTTP状态码',
    `isSuccess` TINYINT(1) NOT NULL DEFAULT '0' COMMENT '是否成功',
    `errorMessage` VARCHAR(2048) NOT NULL DEFAULT '' COMMENT '错误信息',
    `retryCount` INT NOT NULL DEFAULT '0' COMMENT '重推次数',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`),
    KEY `idx_sourceTag` (`sourceTag`),
    KEY `idx_isSuccess` (`isSuccess`),
    KEY `idx_createTime` (`createTime`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='接口日志表';

DROP TABLE IF EXISTS `sys_user_info`;
CREATE TABLE `sys_user_info` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT 'FlowName',
    `staffId` VARCHAR(32) NOT NULL COMMENT '工号',
    `username` VARCHAR(32) NOT NULL COMMENT '用户名',
    `department` VARCHAR(32) NOT NULL COMMENT '部门',
    `role` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '角色',
    `job` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '工种',
    `profession` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '技能等级',
    `isKeyWorker` INT NOT NULL DEFAULT '0' COMMENT '是否关键岗位',
    `isMachineWorker` INT NOT NULL DEFAULT '0' COMMENT '是否机构工种',
    `fingerprintTemplate` VARCHAR(4096) DEFAULT '' COMMENT '指纹1特征(BASE64)',
    `fingerprintTemplate2` VARCHAR(4096) DEFAULT '' COMMENT '指纹2特征(BASE64)',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='用户信息表';

DROP TABLE IF EXISTS `te_faca_config`;
CREATE TABLE `te_faca_config` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `primaryClassificationCode` VARCHAR(32) NOT NULL COMMENT '一级分类代码',
    `primaryClassification` VARCHAR(64) NOT NULL COMMENT '一级分类',
    `secondaryClassificationCode` VARCHAR(32) NOT NULL COMMENT '二级分类代码',
    `secondaryClassification` VARCHAR(64) NOT NULL COMMENT '二级分类',
    `reclassify` VARCHAR(64) NOT NULL NOT NULL DEFAULT '' COMMENT '三级分类',
    `errorCode` VARCHAR(64) NOT NULL NOT NULL DEFAULT '' COMMENT '报警代码',
    `defectClassification` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '故障分类',
    `sortLabel1` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '分类标签1',
    `errorDescription` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '故障描述',
    `sortLabel2` VARCHAR(64) NOT NULL DEFAULT '' COMMENT '分类标签2',
    `faultCause` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '故障原因',
    `faultHandling` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '故障处理',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='FACA配置表';

DROP TABLE IF EXISTS `te_faca_maintain`;
CREATE TABLE `te_faca_maintain` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '流程名称',
    `alarmCode` VARCHAR(64) NOT NULL COMMENT '报警代码',
    `alarmInfo` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '报警信息',
    `failureReason` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '故障原因',
    `failureSolution` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '故障解决方案',
    `alarmStartTime` DATETIME(6) NOT NULL COMMENT '开始时间',
    `alarmEndTime` DATETIME(6) NOT NULL COMMENT '结束时间',
    `alarmDuration` INT NOT NULL DEFAULT '0' COMMENT '报警时长/s',
    `maintainStartTime` DATETIME(6) NOT NULL COMMENT '维护开始时间',
    `maintainEndTime` DATETIME(6) NOT NULL COMMENT '维护结束时间',
    `maintainDuration` INT NOT NULL DEFAULT '0' COMMENT '维护时长/s',
    `empNo` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '员工号',
    `empName` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '员工名称',
    `department` VARCHAR(32) NOT NULL DEFAULT '' COMMENT '所属部门',
    `phmAlarmRecordId` INT NOT NULL COMMENT '关联告警记录id',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='PHM报警维护记录表';

DROP TABLE IF EXISTS `te_time_detail`;
CREATE TABLE `te_time_detail` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) NOT NULL COMMENT '所在机台flowName',
    `nodeType` INT NOT NULL DEFAULT '1' COMMENT '1-模组; 2-零件',
    `sortNo` INT NOT NULL DEFAULT '0' COMMENT '顺序号',
    `plcStartAddress` VARCHAR(64) NOT NULL COMMENT 'PLC起始地址',
    `ctDetails` VARCHAR(1024) NOT NULL COMMENT 'ct详细数据字符串用英文逗号分隔',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='te机台动作CT明细表';

DROP TABLE IF EXISTS `sys_computer_param`;
CREATE TABLE `sys_computer_param` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `computerName` VARCHAR(64) COMMENT '计算机名称',
    `ip` VARCHAR(64) COMMENT '计算机ip地址',
    `flowName` VARCHAR(64) COMMENT '所在机台flowName',
    `cpuName` VARCHAR(64) NOT NULL COMMENT 'CPU名称',
    `gpuDisplayName` VARCHAR(512) COMMENT '显卡名称',
    `memTotalSize` DOUBLE COMMENT '内存总大小GB',
    `diskInfo` VARCHAR(640) COMMENT '磁盘信息',
    `currentVersion` VARCHAR(32) NOT NULL DEFAULT '1.0' COMMENT '当前版本号',
    `latestVersion` VARCHAR(32) NOT NULL DEFAULT '1.0' COMMENT '最新版本号',
    `latestPackageUrl` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '最新版本更新包下载地址',
    `isUpdateTriggered` INT NOT NULL DEFAULT '0' COMMENT '是否已触发更新',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='计算机基本参数配置表';

DROP TABLE IF EXISTS `sys_computer_monitor`;
CREATE TABLE `sys_computer_monitor` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `computerName` VARCHAR(64) COMMENT '计算机名称',
    `ip` VARCHAR(64) COMMENT '计算机ip地址',
    `flowName` VARCHAR(64) COMMENT '所在机台flowName',
    `cpuUsage` VARCHAR(64) COMMENT 'CPU使用率',
    `memUsage` VARCHAR(64) COMMENT '内存使用率',
    `diskUsage` VARCHAR(512) COMMENT '磁盘占用信息(JSON)',
    `topN` VARCHAR(640) COMMENT 'TopN进程资源使用记录(JSON)',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='计算机资源监控表';

DROP TABLE IF EXISTS `sys_client`;
CREATE TABLE `sys_client` (
    `id` INT NOT NULL AUTO_INCREMENT,
    `flowName` VARCHAR(64) COMMENT '所在机台flowName',
    `ip` VARCHAR(64) COMMENT '计算机ip地址',
    `currentVersion` VARCHAR(32) NOT NULL DEFAULT '1.0' COMMENT '当前版本号',
    `latestVersion` VARCHAR(32) NOT NULL DEFAULT '1.0' COMMENT '最新版本号',
    `latestPackageUrl` VARCHAR(256) NOT NULL DEFAULT '' COMMENT '最新版本更新包下载地址',
    `isUpdateTriggered` INT NOT NULL DEFAULT '0' COMMENT '是否已触发更新',
    `createTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6),
    `updateTime` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP (6) ON UPDATE CURRENT_TIMESTAMP (6),
    `isDeleted` INT NOT NULL DEFAULT '0',
    PRIMARY KEY (`id`)
)  ENGINE=INNODB DEFAULT CHARSET=UTF8MB4 COLLATE = UTF8MB4_UNICODE_CI COMMENT='客户端版本管理表';
