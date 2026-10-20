# SocammRework 开发约定

- 基线：.NET 10、WPF、所有 Tm.Ant NuGet 包固定为 3.2.0。业务 Module 不引用框架源码。
- 依据 `doc/Socamm重工线-PLC通讯协议.xlsx` 和 `doc/业务流程说明.md` 开发；不得擅自新增 PLC 地址或改变反馈含义。
- 应用 tm-ant-code-style、flow-organization、flow-interaction、logging、device-io、configuration、wpf；初始化与部署骨架参考 project-init。
- 不新增单元测试项目。构建、静态核对和临时隔离冒烟用于验证，不连接生产数据库、SFC 或设备。
- `LDFlow`、`UldFlow` 直接实现 `IFlowBase`；两台拆螺丝机继续共用 `ScrewFlowBase`。各站业务交互直接在 Flow 中展开，不另设工站 FlowService 接口或实现；IO、数据库、SFC 等基础能力保留独立服务。
- 地址按工站存于 `Models/Pto`。曲线为同一模型的两套地址，仅本地文件存储，不回写或清零 PLC；D168 保存主表成功后才清零。
- 设备通过 `IDeviceFactory` 和 `DeviceIds` 获取。禁止重新引入 IPlcFactory、ITmSocketFactory 或业务层品牌连接、自动重连。
- Vanguard 驱动在独立 Communication 实现项目中，实现 `ITighteningDevice`，生命周期遵守框架 Device 接入手册。
- 配置使用 `IConfigService`；品牌、IP、端口在框架设备管理配置。不要在 Flow 或业务配置页重复提供设备连接参数。
- 仅使用 `flow` 主表，软删表示下线；报表包含软删记录。禁止历史表和业务归档搬移。
- 使用中文业务日志，DB 变更、设备交互与工站汇总各负责自身记录。业务接口继承生命周期标记，由框架自动扫描。
- C# 四空格、Allman 花括号、UTF-8 BOM/CRLF；自动属性保持同行。公共业务契约与重要类型使用三行中文 summary。
- 验证命令：`dotnet restore Tm.Ant.LgaRework.sln`、`dotnet build Tm.Ant.LgaRework.sln --no-restore`、`dotnet format whitespace --folder src --verify-no-changes`、`git diff --check`。
- macOS 下不能把 Windows 目标编译当成 GUI 或现场联调验收。
