# SocammRework

Socamm 重工线，基于 .NET 10 / tm.ant.core 3.2.0。包含上料、两台同协议拆螺丝机、下料、主表报表、本地曲线查询、SFC 配置及手动过站。

## 构建

```sh
dotnet restore Tm.Ant.LgaRework.sln
dotnet build Tm.Ant.LgaRework.sln --no-restore
dotnet publish src/Tm.Ant.Shell/Tm.Ant.Shell.csproj -c Release -r win-x64 --self-contained false
```

`NuGet.Config` 目前使用相邻 `../Lpu4223/nupkgs` 作为 Tm.Ant 3.2.0 本地包源，并保留 nuget.org。其他环境应将该源替换为可访问的同版本包源。

没有新增单元测试项目。跨平台支持构建和非 WPF 代码验证，实际程序在 Windows 运行。框架依赖部署端的 .NET 10 Desktop Runtime / ASP.NET Core Runtime，具体要求见发布目录的 runtimeconfig.json。

## 启动配置

- `SingleModuleName`：`Tm.Ant.Module.Rework`。
- `LocalConfig.FlowName`：按机台选择 `LD`、`UNSCREW1`、`UNSCREW2`、`ULD`，FlowJob 已启用。
- 默认 `RunMode=Offline`，使用新空库 `Config/rework.db`。没有配置真实设备，不会虚构默认设备连接。
- MySQL 生产运行：配置真实连接串并设置 `RunMode=Online`，先由部署人员核对/准备数据库。
- 设备管理中配置 `MainPlc`、`ProductScanner1`；拆螺丝机另外配置 `Tightening1`、`Tightening2`。
- Vanguard 设备 subtype 为 `VanguardVLib`，业务接口为 `ITighteningDevice`，两把螺丝批的 ToolNo 分别配置为 1、2。
- 首次配置设备完成后重启软件或通过框架任务管理重新启动 FlowJob，让工站完成设备就绪检查与订阅。
- SFC 参数使用 `ReworkSfc` 本机配置，可通过“重工线SFC”页面或框架配置管理编辑。屏蔽开关沿用共享字典 key `Shield`。
- SFC 屏蔽时使用上料页面选择的模拟需拆穴位；默认八穴均不选。零需拆集合下料判 NG，不上传 PASS。

## 数据与迁移

- 新库脚本：`doc/sql/mysql_init.sql`、`doc/sql/sqlite_init.sql`，均不包含生产数据或 DROP TABLE。
- `assets/seed/rework.db` 是由 SQLite 脚本生成的空白种子。构建/发布只在目标文件不存在时复制，不覆盖运行库。
- `appsettings.json`、`appsettings.DeviceConfig.json`、`appsettings.ReworkSfc.json` 同样只在目标不存在时复制；重复构建或发布保留现场配置，模板更新需人工合并。
- 原 `src/Tm.Ant.Shell/Config/littleAnt.db` 保留原文件，已取消复制。它不是 3.2.0 新库，不能直接当新版本种子使用。
- 不自动连接或修改旧 MySQL，也不自动迁移旧历史表。部署前备份现有库；旧框架表的列差异需要单独核对，初始化脚本不替代存量数据库迁移。
- 旧 `ReworkOption.Sfc` 参数应迁入 `Config/appsettings.ReworkSfc.json` 的 `ReworkSfc` 节；旧设备 IP/端口应迁入设备管理，不再读取旧 ReworkOption 设备节点。
- 曲线默认保存在部署目录的 `Data/screw/日期/`。可用 `ReworkCurveDirectory` 指向原曲线目录；仍支持旧 JSON 曲线字段和按日期、产品码查询。

## 文档

- `doc/业务流程说明.md`：已实现业务、数据语义及保留策略。
- `doc/Socamm重工线-PLC通讯协议.xlsx`：业务 PLC 协议，不含 IP/PHM 表。
- `doc/VLib设备接入.md`：品牌实现、寄存器解析、生命周期与运维参数。
- `doc/重构验证记录.md`：实际验证范围及环境限制。

core 3.2.0 的传递依赖目前存在 NuGet 审计告警（ImageSharp、SQLitePCLRaw）；本次未擅自升级框架或其依赖基线。
