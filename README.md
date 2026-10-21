# SocammRework

Socamm 重工线，基于 .NET 10 / tm.ant.core 3.2.0。包含上料、两台同协议拆螺丝机、下料、主表报表、本地曲线保存、SFC 配置及手动过站。

## 构建

```sh
dotnet restore Tm.Ant.LgaRework.slnx
dotnet build Tm.Ant.LgaRework.slnx --no-restore
dotnet publish src/Tm.Ant.Shell/Tm.Ant.Shell.csproj -c Release -r win-x64 --self-contained false
```

`NuGet.Config` 使用相邻 `../Lpu4223/nupkgs` 作为 Tm.Ant 3.2.0 本地包源，并保留 nuget.org。Vanguard 的 Lang*.txt 资源由 Tm.Ant.SourceGenerators 3.2.0 在构建时生成，包源也需提供该包。其他环境应替换为可访问的同版本包源。

没有新增单元测试项目。跨平台支持构建，实际程序在 Windows 运行，部署需要 .NET 10 Desktop Runtime / ASP.NET Core Runtime。

## 配置

Shell 配置结构与 Lpu4223 一致：

- `Config/appsettings.json`：版本、运行模式、MockHost、FileServerHost、数据库连接串。MySQL 连接串按部署环境填写。
- `Config/littleAnt.db`：保留原本地数据库，按 Lpu4223 的方式复制，不另设种子目录或初始化复制目标。
- `Config/Unchanged/appsettings.ModuleConfig.json`、`appsettings.nlog.json`：固定 UI 与日志配置。

设备、机台、本机参数、任务开关与 SFC 业务参数通过框架配置管理生成和维护，不在 Shell 预置对应 JSON，也不在 appsettings 中展开默认值。发布沿用标准 PreserveNewest 复制规则；部署更新时保留现场配置和数据库。

- 工站 FlowName：`LD`、`UNSCREW1`、`UNSCREW2`、`ULD`。
- 设备 ID：`MainPlc`、`ProductScanner1`；拆螺丝机另有 `Tightening1`、`Tightening2`。
- Vanguard subtype：`VanguardVLib`，接口为 `ITighteningDevice`，两把工具 ToolNo 分别为 1、2。
- 首次配置完成后重新启动 FlowJob 或软件，完成设备检查和订阅。
- SFC 参数使用 `ReworkSfc` 本机配置，由框架配置管理自动生成管理页面并统一编辑；查询地址属性为 `CheckUrl`，过站地址为 `RecordUrl`。已有配置需将旧键 `ReworkUrl` 改为 `CheckUrl`；屏蔽开关使用共享字典 key `Shield`。
- Shield 仅保留 SFC 开关；关闭 SFC 后，在上料机首页屏蔽区域勾选 1–8 穴位并点击“应用需拆穴位”，后续上料使用已应用的标记。选择仅在当前进程内保留，重启后默认八穴均不选；零需拆集合下料判 NG。

## 数据库脚本

框架初始化与业务初始化分别执行：

| 数据库 | 框架表 | 业务 flow 表 |
|---|---|---|
| MySQL | `doc/mysql_common.sql` | `doc/1数据库创建初始化脚本.sql` |
| SQLite | `doc/sql/sqlite_common.sql` | `doc/sql/sqlite_flow.sql` |

脚本不包含 DROP TABLE 或生产数据，不自动连接数据库。现有本地库保持原文件，未自动更新结构；使用前按 core 3.2.0 核对现有列。初始化脚本不能替代存量库迁移，部署前备份。

仅使用 flow 主表，软删表示下线，报表包含软删记录。旧 `ReworkOption.Sfc` 参数迁入框架管理的 `ReworkSfc`；旧设备参数迁入设备管理。

曲线仅保存本地文件：通过 `IConfiguration.GetDataDirectory()` 获取数据根目录，保存到 `screw/yyyy-MM/`（例如 `screw/2026-10/`）。文件名包含完整日期时间，内容沿用原 JSON 字段；不提供曲线展示、缓存或历史查询。

交互逻辑直接位于 `LDFlow`、`ScrewFlowBase`、`UldFlow`，不额外设置工站 FlowService；基础 IO、产品数据、SFC 服务仍独立。两台拆螺丝机共用 `ScrewFlowBase`。拧紧枪的结果和曲线由独立 `ScrewReceiveJob` 订阅接收，任务仅在 `UNSCREW1` / `UNSCREW2` 运行，启停和开关由框架 Job 管理，与 FlowJob 独立；停止时退订全部回调。设备连接及重连仍由框架设备生命周期管理。

业务页面统一位于 Home：首页下可进入手动过站、产品报表；上料屏蔽控件仍显示在首页区域。下料结果展示、状态缓存和展示事件已移除，PCB 图片和穴位坐标资源保留，待后续需求明确。

## 文档与模型

- `doc/业务流程说明.md`：工站业务、PLC 交互与数据语义。
- `doc/Socamm重工线-PLC通讯协议.xlsx`：业务 PLC 协议。
- `doc/VLib设备接入.md`：设备生命周期、寄存器与参数。
- `doc/重构验证记录.md`：验证范围和环境限制。
- `Models/SFC/Check.cs`：需拆查询请求与响应（CheckRequest / CheckResponse）；`StationRecord.cs`：普通过站请求与响应，均为独立模型，不使用继承。

core 3.2.0 的传递依赖存在 NuGet 审计告警（ImageSharp、SQLitePCLRaw），本次保持指定版本。
