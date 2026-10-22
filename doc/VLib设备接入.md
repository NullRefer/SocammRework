# VLib 拧紧设备接入

## 实现与注册

实现项目：`src/Tm.Ant.Communication.Tightening.Vanguard`，目标框架 `net10.0`，引用 `Tm.Ant.Communication.Tightening.Abstractions 3.2.0`。Modbus TCP 使用框架传递提供的 HslCommunication 11.3.2，不再依赖 NModbus4，也不新增包或授权信息。

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
- `DisconnectAsync` 取消采集，等待在途读取完成或按 TimeoutMs 超时、已开始的采集回调结束，再关闭连接并释放 Modbus 资源（默认读取超时约1000ms）；同一实例可重新连接。
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

站号固定为1，地址从0开始。工作状态使用 `194`（FC03），输入寄存器使用 `x=4;十进制地址`（FC04）。所有寄存器按无符号 UInt16 读取，检查通信结果和返回字数；首次失败立即结束采集并标记断线，不再读取，避免触发 Hsl 的后续请求自动重连。连接及接收超时均取 TimeoutMs；不使用会遗留后台 I/O 的取消包装。

采集间隔沿用原 VLib 的10ms。只在忙碌到完成的转换时发布完成结果与完整曲线，不反复发布同一次结束状态。

原 VLib 的曲线横坐标每个点递增1；此版本保留该采样步进，不凭空换算角度步距或时间采样率。曲线文件保留设备采样坐标和原始扭矩单位，不提供曲线页面。`Times` 留空，硬件未提供结果ID时 `ResultId=0`，曲线关联ID为空，不制造控制器结果ID。

本线 PLC 控制实际加工，本驱动仅采集，不额外发送启动、停止、换程序命令。

## 业务订阅

两台拆螺丝机共用 `ScrewFlowBase` 处理产品扫码与 PLC 穴位最终结果。独立 `ScrewReceiveJob` 通过 `IDeviceFactory` 获取两把 `ITighteningDevice`，订阅结果和曲线事件，Job 停止时取消订阅；不管理设备连接或自动重连。Job 仅在 UNSCREW1 / UNSCREW2 运行，由框架扫描并使用独立任务开关，与 FlowJob 启停解耦。

曲线回调由 ScrewReceiveJob 根据PLC两套曲线关联地址确定产品、穴位、螺丝编号和程序号。整个过程不清零、不回写业务 PLC，不更新生产主表。

有有效关联时保存本地 JSON 文件；没有有效关联时将原始曲线保存为明确标记的 `UNMATCHED` 文件并记录告警，不伪造真实产品或有效穴位。

数据库的穴位质量仍以 PLC 工位2的 D168 上传为准，不能从螺丝批回调、曲线文件数量或工具 IsOk 推断产品总质量。

曲线仅作本地文件保存，目录为 `IConfiguration.GetDataDirectory()/screw/yyyy-MM/`；没有曲线展示事件、缓存或历史查询接口。

## Hsl 替换验证（2026-10-22）

未新增测试项目。临时控制台通过项目引用运行实际 Vanguard 驱动，仅连接本机回环模拟 Modbus TCP 端点，48 项断言通过：

- FC03 / FC04、站号1、零基地址、只读请求；241点按120、120、1字读取，验证0、32768及65535边界、比例和曲线横坐标。
- 结果就绪等待、最多10次等待后仍发布曲线、结果先于曲线、NG结果与曲线事件保留。
- 对端断开、读取超时、Modbus异常响应、数据不足；工作状态、曲线长度、采样、结果就绪及结果读取首次失败后均不再请求，不自行重连；健康检查不发请求。
- 阻塞读取期间停止：TimeoutMs=250时实测约251ms；等待已开始的回调完成；活动实例重新配置后旧端点不再收请求，新配置保留订阅，同实例断开后可再次连接。

已执行方案要求的 restore、build、whitespace 和 diff 检查。构建0错误、18条既有传递依赖安全警告（ImageSharp、SQLitePCLRaw）。另向全新临时目录发布 win-x64、framework-dependent Shell，确认 HslCommunication.dll 和 Vanguard 驱动存在，发布文件及 deps.json 均不再包含 NModbus。

模拟验证不代表 Windows 实机或现场验收；正式使用前仍需验证默认超时下的停止耗时、连续采样和既有 Hsl 授权的持续运行情况。
