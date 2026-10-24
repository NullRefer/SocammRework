# Socamm 重工线

基于 tm.ant 3.2.0 的 Windows WPF 上位机，负责 Socamm 重工线的扫码、SFC 校验、需拆穴位下发、拆除结果保存及下料过站。仓库目录名为 `SocammRework`，解决方案沿用 `Tm.Ant.LgaRework.slnx`。

## 技术与项目结构

| 项目 | 目标框架 | 职责 |
| --- | --- | --- |
| `src/Tm.Ant.Shell` | `net10.0-windows` | WPF 启动入口、部署配置及框架 Shell |
| `src/Tm.Ant.Module.Rework` | `net10.0-windows` | 工站 Flow、业务服务、数据实体、页面及后台任务 |
| `src/Tm.Ant.Communication.Tightening.Vanguard` | `net10.0` | Vanguard VLib 拧紧设备的 Modbus TCP 驱动 |

Shell 引用业务模块，业务模块引用 Vanguard 驱动。业务模块依赖 `Tm.Ant.CommonViewWpf 3.2.0`；驱动依赖 `Tm.Ant.Communication.Tightening.Abstractions 3.2.0` 和私有生成器包 `Tm.Ant.SourceGenerators 3.2.0`。

```text
src/Tm.Ant.Module.Rework/
├── Flows/             上料、拆螺丝、下料交互
├── Models/Pto/        PLC 地址组
├── Interfaces/Common/ 业务接口
├── Services/Common/   IO、SFC、产品数据、报表、显示快照
├── Entities/          flow 主表映射与逐穴质量判定
├── Jobs/              螺丝批结果和曲线接收任务
├── Provider/          本地曲线文件保存
├── Views/             WPF 页面
├── ViewModels/        页面状态与命令
├── Behaviors/         螺丝显示标定交互
└── RegisterLayouts/   首页布局注册
doc/                   业务说明、通讯协议、SQL 与历史验证记录
```

## 构建与运行

开发需要 .NET 10 SDK；运行与界面验收需要 Windows。项目已启用 `EnableWindowsTargeting`，可在其他系统尝试交叉编译，但不能据此认定 WPF 界面可运行。仓库未提供 `global.json`。

`NuGet.Config` 清除了默认源，配置了：

- `TM-Local`：相对仓库根目录的 `../Lpu4223/nupkgs`。
- `nuget.org`：公共依赖源。

还原前确保可取得项目指定的 3.2.0 私有包。仓库工作目录中的 `nupkg/` 并未配置为默认源，且 `*.nupkg` 被 Git 忽略；不能假设克隆仓库后就会带齐私有依赖。若使用其他私有包位置，应按本机环境配置源。

在仓库根目录执行：

```powershell
dotnet restore Tm.Ant.LgaRework.slnx
dotnet build Tm.Ant.LgaRework.slnx --no-restore
```

在 Windows 完成本机配置后运行：

```powershell
dotnet run --project src/Tm.Ant.Shell/Tm.Ant.Shell.csproj
```

发布 Windows x64 的框架依赖版本：

```powershell
dotnet publish src/Tm.Ant.Shell/Tm.Ant.Shell.csproj -c Release -r win-x64 --self-contained false
```

目标机器需要匹配的 .NET 10 Windows Desktop Runtime。Shell 启动时将工作目录固定为 `AppContext.BaseDirectory`，相对配置和数据路径以程序部署目录为基准。

## 首次部署配置

1. 核对部署目录下的 `Config/appsettings.json`，按现场环境设置运行参数和数据库连接。当前源码配置的 `RunMode` 为 `Online`；配置中的连接信息须按部署环境核对。
2. 在框架本机配置中设置 `LocalConfigOption.FlowName`，从下表选择本机工站。工站选择不是 `ModuleConfig.Modules` 中的菜单开关。
3. 在设备管理中设置设备 ID、类型及连接参数。PLC、扫码枪的连接信息不写在 Flow 中。
4. 通过框架配置管理设置 SFC 参数及 `Shield` 开关。设备首次配置完成后重新启动相关任务或软件。
5. 按选用的数据库核对表结构，再进行隔离验证与设备联调。

| 机台 | FlowName | 实现 | 设备 ID |
| --- | --- | --- | --- |
| 上料机 | `LD` | `LDFlow` | `MainPlc`、`ProductScanner1` |
| 拆螺丝机 1 | `UNSCREW1` | `UnscrewFlow1` | 同上，曲线接收另需 `Tightening1`、`Tightening2` |
| 拆螺丝机 2 | `UNSCREW2` | `UnscrewFlow2` | 同上，曲线接收另需 `Tightening1`、`Tightening2` |
| 下料机 | `ULD` | `UldFlow` | `MainPlc`、`ProductScanner1` |

VLib 设备子类型为 `VanguardVLib`，配置类型为 `VanguardTighteningDeviceConfig`；端口默认 `4545`，超时默认 `1000 ms`。`ToolNo` 是设备标识，业务曲线关联使用两个订阅入口对应的编号 1/2。详细生命周期见 [VLib 设备接入](doc/VLib设备接入.md)。

配置文件与持久化配置分工如下：

| 位置或配置键 | 用途 |
| --- | --- |
| `Config/appsettings.json` | 运行模式、MockHost、MySQL/SQLite 连接字符串等 |
| `Config/Unchanged/appsettings.ModuleConfig.json` | 框架菜单模块开关 |
| `Config/Unchanged/appsettings.nlog.json` | 日志配置 |
| `Config/littleAnt.db` | 随 Shell 复制的 SQLite 文件 |
| `SfcConfigOption` | `CheckUrl`、`RecordUrl`、`Group`、`Emp`、`MachineCode`，由 `IConfigService` 管理 |
| `Shield` | `Sfc` 开关；`true` 表示启用 SFC |
| `ScrewDisplayCalibration` | 下料螺丝显示的 24 个归一化标定点，由 `IConfigService` 管理 |

以上四个 Shell 配置文件使用 `CopyToOutputDirectory=PreserveNewest`。部署更新前应备份并核对目标配置和数据库，不能将此复制策略理解为始终保留现场文件。

## 业务流程与关键约定

整线流程：上料扫码 → SFC 返回需拆位置 → 创建在线记录 → 拆螺丝扫码并下发八穴标记 → PLC 上传逐穴最终结果 → 下料逐穴判定 → 必要时 SFC PASS → 软下线。

- 上料确认使用 SFC 返回的 `GROUP`、`DEKIT_LOCATION`，不使用该接口的 `rc` 判定。站点组按逗号拆分后至少匹配一项；同号 L/J 位置必须成对出现才标记需拆。八穴全 0 时拒绝建档。
- 穴位 1～8 依次对应位置号 `172、170、168、212、174、176、178、180`，例如 `L172,J172` 对应穴位 1。
- `UnScrewCaves` 是按穴位排列的八个 0/1 标记。拆螺丝机下发完整需拆数组，不按已收到结果过滤。
- 质量依据 PLC 上传的穴位最终结果。要求穴位全部有 OK 结果才总 OK；缺失、NG 或零需拆集合均为总 NG，额外穴位不替代要求穴位。
- 只有总 OK 才调用 SFC PASS，过站响应按 `rc=0` 判断。SFC 失败保留在线记录；质量 NG 不上传 PASS，但处理完成后可以软下线。
- 下料 `D201` 表示处理成功 1 / 失败 2，不能作为产品质量位；质量 NG 正常下线仍反馈 1。

### PLC 交互

| 交互 | 关键地址 |
| --- | --- |
| 上料扫码 | 触发 `D100`，反馈 `D200`，产品码 `D201` |
| 拆螺丝扫码 | 触发 `D100`，反馈 `D200`，产品码 `D201`，更新标记 `D232`，八穴数组 `D233~D240` |
| 工位 2 最终结果 | 产品码 `D135`，穴位 `D166`，结果/触发 `D168`（1=OK、2=NG） |
| 下料扫码 | 触发 `D101`，反馈 `D201`，产品码 `D232` |

扫码先处理业务和写反馈，再由框架 Runtime 清零触发。最终结果交互处理完成后，Runtime 无论业务成功或失败均清零 `D168`；清零仅表示交互已处理。曲线关联地址只读，不回写或清零。完整地址以 `Models/Pto/` 为准。

IO 统一清理扫码及 PLC 字符串两端控制字符、空白和空字符；PLC 产品码读取显式传入长度 30，该长度不限制扫码码长。常规 PLC 写入在 IO 层最多尝试三次。

### 数据与曲线

业务只使用 `flow` 主表。在线查询按产品码和 `IsDeleted=0` 取最新记录；下线将本次记录 `IsDeleted` 置 1，数据继续保留在主表，报表包含已下线记录。

重复上料会新增记录，不自动清理同码旧在线记录；重复穴位结果追加到 `ResultJson`，最终判定取该穴位首次保存的结果。当前不单独持久化产品总 OK/NG 字段。

`ScrewReceiveJob` 仅在 `UNSCREW1/UNSCREW2` 运行，订阅两把螺丝批的结果和曲线。曲线保存到 `IConfiguration.GetDataDirectory()` 下的 `screw/yyyy-MM/*.txt`，文件内容为 JSON；无法关联的曲线以 `UNMATCHED` 标记保存。曲线不写业务主表，不参与最终质量判定，当前没有曲线展示或历史查询页面。

数据库初始化参考：

- SQLite 公共表：`doc/sql/sqlite_common.sql`；业务表：`doc/sql/sqlite_flow.sql`。
- MySQL 公共表：`doc/mysql_common.sql`；业务表：`doc/1数据库创建初始化脚本.sql`。

这些脚本用于新库初始化，不能替代存量数据库迁移。当前随仓库提供的 `littleAnt.db` 不含 `flow` 表；使用 SQLite 业务存储时需先核对并补齐业务结构，不应假设内置数据库已完成业务初始化。

## 页面与测试模式

- 首页提供手动过站和产品报表；`ULD` 额外注册螺丝显示，`LD` 注册 SFC 屏蔽区域。
- 产品报表按产品码包含匹配、上料时间半开区间 `[开始, 结束)` 筛选，ID 倒序，每页 20 条；默认今日 08:00 至次日 08:00，Excel 导出全部筛选结果。
- 下料取得最终结果后即发布最新显示快照，早于 SFC 上传和软下线；显示时间不代表过站成功时间。螺丝显示需按穴位顺序完成 24 点标定，每穴结果映射到三颗螺丝，未标定时不推测位置。
- 关闭 SFC 后，上料使用首页已应用的测试穴位，默认全 0，重启后重置；至少选择一个穴位才能上料。扫码设备返回失败且 SFC 关闭时生成 `Fake-...` 产品码，过站模拟成功。

## 验证与进一步阅读

仓库没有随附自动化测试项目。修改后的常规检查为构建、`git diff --check` 及必要的隔离验证；Windows 页面、真实 PLC 时序、扫码枪、螺丝批和 SFC 需要现场验收。

- [业务流程说明](doc/业务流程说明.md)
- [VLib 设备接入](doc/VLib设备接入.md)
- [历史重构验证记录](doc/重构验证记录.md)
- 开发协作约定见根目录 `AGENTS.md`。

历史验证记录包含后续已替换的方案及旧 `.sln` 命令；当前行为以源码为准，当前构建入口为 `.slnx`。
