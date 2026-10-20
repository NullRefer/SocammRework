# VLib 拧紧设备接入

## 实现与注册

实现项目：`src/Tm.Ant.Communication.Tightening.Vanguard`，目标框架 `net10.0`，引用 `Tm.Ant.Communication.Tightening.Abstractions 3.2.0`。

`VanguardTighteningDevice` 实现 `ITighteningDevice`、`IDisposable`，通过 `DeviceServiceAttribute` 注册：

- Category：框架 `Tightening`。
- Subtype：`VanguardVLib`。
- ConfigType：`VanguardTighteningDeviceConfig`。
- ServiceType：`ITighteningDevice`。

框架启动扫描所有 `Tm.Ant.*` 程序集，驱动 DLL 由 Module 项目引用并随 Shell 发布。业务仅通过 `IDeviceFactory` 获取设备，不直接创建品牌实现。

接入依据是 Communication 的 `doc/communication-device-architecture.md`，没有修改框架仓库。

## 配置与生命周期

IP、Port、ToolNo、TimeoutMs 均在设备管理中编辑，不放在工站 Flow 中。

- 配置中的 IP 必须填写；端口默认 4545，沿用原项目 VLib 协议。
- 两个设备 ID 分别为 `Tightening1`、`Tightening2`，ToolNo 必须分别为 1、2；业务会检查回调编号与入口配置一致。
- `Configure` 停止旧采集任务和连接，复制配置，保留同一 service 实例及外部事件订阅。
- `ConnectAsync` 建立一次 TCP/Modbus 连接并启动采集任务。没有永久自动重连线程。
- `CheckConnectionAsync` 仅报告当前健康状态，不主动重连。
- `DisconnectAsync` 取消采集、关闭 Socket、中断阻塞读、等待采集任务结束并释放 Modbus 资源；同一实例可重新连接。
- 连接失败和采集通信异常交由框架生命周期管理器按设备配置决定重连；没有业务层额外重连 Job。
- 订阅者异常会记录，不导致通信设备被误判为断线；断开过程会等待已开始的回调结束，避免旧配置继续发布数据。

## 沿用的线协议

这些是螺丝批内部 Modbus 地址，不是业务 PLC 的 D 地址。

| 动作 | Modbus 功能 | 地址 | 解释 |
|---|---|---|---|
| 工作状态 | Read Holding Registers | 0xC2，1字 | bit1 表示忙碌 |
| 结果就绪 | Read Input Registers | 0x30，1字 | 值为1时读取结果；最多检查10次，每次间隔30ms |
| 完成结果 | Read Input Registers | 0x31，6字 | 程序、圈数/10、扭矩/10、时间ms、结果码、原因码 |
| 曲线长度 | Read Input Registers | 0x3F，1字 | 已生成样本数 |
| 曲线数据 | Read Input Registers | 0x40起 | 每批最多120字，扭矩除以10；工作结束读取剩余样本 |

采集间隔沿用原 VLib 的10ms。只在忙碌到完成的转换时发布完成结果与完整曲线，不反复发布同一次结束状态。

原 VLib 的曲线横坐标每个点递增1；此版本保留该采样步进，不凭空换算角度步距或时间采样率。曲线页面显示“曲线采样坐标”和“设备原始扭矩单位”。`Times` 留空，硬件未提供结果ID时 `ResultId=0`，曲线关联ID为空，不制造控制器结果ID。

本线 PLC 控制实际加工，本驱动仅采集，不额外发送启动、停止、换程序命令。

## 业务订阅

两台拆螺丝机共用 `ScrewFlowBase`。Flow 启动时获取两把 `ITighteningDevice`，订阅曲线事件；停止时取消订阅。

曲线回调由 ScrewFlowBase 根据PLC两套曲线关联地址确定产品、穴位、螺丝编号和程序号。整个过程不清零、不回写业务 PLC，不更新生产主表。

有有效关联时保存本地 JSON 文件并发布展示事件；没有有效关联时将原始曲线保存为明确标记的 `UNMATCHED` 文件并记录告警，不伪造真实产品或有效穴位。

数据库的穴位质量仍以 PLC 工位2的 D168 上传为准，不能从螺丝批回调、曲线文件数量或工具 IsOk 推断产品总质量。
