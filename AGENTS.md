# 仓库协作说明

本文适用于整个仓库。修改前先阅读 `README.md`，再阅读相关源码及 `doc/` 文档；历史验证记录包含已被后续实现替换的内容，行为判断以当前源码为准。

## 项目边界

- 当前解决方案为 `Tm.Ant.LgaRework.slnx`，不要使用历史文档中的 `.sln` 路径。
- Shell 和业务模块为 .NET 10 Windows WPF；Vanguard 驱动为 .NET 10 类库。tm.ant 直接包引用固定为 3.2.0。
- `Tm.Ant.Shell` 负责启动和部署配置，保持 `AppContext.BaseDirectory` 工作目录约定。
- `Tm.Ant.Module.Rework` 承载业务；`Tm.Ant.Communication.Tightening.Vanguard` 承载品牌设备协议。不要将品牌连接、Modbus 细节或自建重连线程放入业务 Flow。
- 普通服务、Flow、设备由框架扫描；`ReworkModule` 注册 UI 服务和上料屏蔽区域，`ReworkHomeLayout` 注册首页布局。遵循现有发现机制，避免重复手工注册。

## 代码组织与实现

- 工站交互放在 `Flows/`，PLC 地址组放在 `Models/Pto/`。两个拆螺丝入口共用 `ScrewFlowBase`，不要复制两套相同业务。
- IO、SFC、产品数据、报表及显示快照通过 `Interfaces/Common/` 与 `Services/Common/` 分工，构造注入所需接口。
- 数据访问通过 `IRepositoryFactory.CreateRepository<T>()`，使用 `using` 释放仓储；不在 ViewModel 或 Flow 中直接写 SQL。
- 预期业务失败返回带上下文的 `OperationResult`，保留底层错误码；不要把失败包装成成功。
- Flow 不增加外层通用 `try-catch`，未预期异常交给 Runtime。传播取消，避免吞掉 `OperationCanceledException` 或在异常分支补写成功反馈。
- 设备通过 `IDeviceFactory` 和 `DeviceIds` 获取；保持 `MainPlc`、`ProductScanner1`、`Tightening1`、`Tightening2` 稳定。IP、端口等由设备管理维护。
- 常规 PLC 写入的最多三次尝试由 `FlowIoService` 负责，业务调用方不要叠加重试。新增交互要检查每一步结果，避免失败后继续执行依赖动作。
- IO 边界使用框架 `TrimControlCharacters()` 清理码值；PLC 字符串读取必须显式传入长度。`Consts.PRODUCT_CODE_LEN=30` 仅用于当前 PLC 读取，不用于扫码长度校验。
- 静态宿主配置使用 `IConfiguration`，持久化业务配置使用 `IConfigService` 及现有配置模型；不要另建 JSON 读写路径绕过框架配置管理。
- 日志跟随职责记录设备、地址、产品、穴位或记录 ID 等上下文；避免每层重复记录同一错误。

## 必须保留的业务语义

未经明确业务需求，不改变以下规则：

1. `FlowName` 为 `LD`、`UNSCREW1`、`UNSCREW2`、`ULD`；两台拆螺丝机使用相同协议。
2. 上料 SFC 确认仅按 `GROUP` 和 `DEKIT_LOCATION` 判断，不读取 `rc`；站点组至少匹配一项。同号 L/J 成对位置映射为八穴 0/1，八穴全 0 拒绝建档。普通过站仍按 `rc=0` 判断。
3. 主表为 `flow`，在线查询取同产品码且 `IsDeleted=0` 的最新记录；下线软删除本次记录，不搬移历史表。
4. 重复上料新增记录，不自动清理旧在线记录；穴位结果追加，逐穴判定取首次结果。不要顺手增加覆盖、去重或返工策略。
5. 拆螺丝扫码下发上料保存的完整八穴数组，不按已上传结果过滤。
6. 最终质量按要求穴位逐一匹配，缺失或非 OK 为 NG；额外穴位无替代作用，零要求集合也为总 NG。曲线及螺丝批回调不替代 PLC 最终结果。
7. 总 OK 才请求 SFC PASS；SFC 失败保留在线记录。质量 NG 不请求 PASS，但可正常软下线。
8. 下料 `D201` 是处理反馈，成功 1、失败 2，质量 NG 下线成功也反馈 1；不得改作质量位。
9. 扫码先处理业务、写输出与反馈，再由 Runtime 清零触发。`D168` 最终结果交互处理完成后无论业务成功或失败均由 Runtime 清零；清零不代表保存成功或质量 OK。保持框架取消契约，不新增独立结果反馈地址。
10. 曲线关联地址只读，不清零或回写。`ScrewReceiveJob` 独立于 Flow 启停，仅在两台拆螺丝机运行，订阅与退订必须配对。
11. 曲线按订阅入口 1/2 匹配各自地址组，不使用设备配置 `ToolNo` 代替该编号。曲线仅保存本地文件，无法关联时保留 `UNMATCHED` 原始曲线。
12. SFC 屏蔽测试穴位仅存当前进程，默认全 0；只有扫码设备失败且 SFC 关闭时生成假码。成功扫码的 `Err`、`NG` 等文本不应被特判成假码。

## WPF 页面约定

- 遵循 MVVM，View code-behind 仅初始化及绑定 DataContext；鼠标坐标换算等控件交互放入 Behavior，业务放入服务。
- 延续框架控件、`ObservableObject`、`[ObservableProperty]` 和 `[RelayCommand]`，不要手写重复的生成器代码。
- 产品报表服务共用筛选与 DTO 投影，返回 `PagedResultDto<ProductReportRowDto>`；查询包含软下线记录，按 ID 倒序，时间区间左闭右开。
- 报表默认今日 08:00 至次日 08:00，每页 20 条；导航进入重置筛选并查询第一页。不要无需求增加 IsBusy、导航取消编排或参数校验层。
- 报表服务只返回全部筛选结果列表；ViewModel 调用 `ExcelHelperWithDialog.ListToExcel`，由框架处理选路径、写入与成功提示。不要把文件对话框和 Excel 写盘搬入服务。
- 螺丝显示只在 `ULD` 注册。最新快照在质量判定后、SFC 上传前发布；通知异常不能阻断产品流程。
- 显示页面进入订阅并恢复最新快照，离开退订；保留标定加载/保存的导航会话取消及版本保护。不要将报表的简化规则套用到标定流程。
- 标定为 24 个归一化坐标，按穴位 1～8、每穴三颗顺序保存；未标定时不推测位置。不将旧 `RectangleData.json` 自动迁移成新标定数据。

## 格式与依赖

- 遵循 `.editorconfig`：UTF-8 BOM、CRLF；C# 四空格缩进、文件范围 namespace、Allman 大括号，沿用现有构造注入风格。
- 改动保持聚焦，不进行无关的全库格式化、依赖升级、解决方案改名或框架仓库改动。
- `NuGet.Config` 的私有源是 `../Lpu4223/nupkgs`，仓库内 `nupkg/` 不是默认源；缺失私有包时明确报告依赖问题，不通过升级包绕过。
- 不提交构建产物、日志、现场导出、临时验证器及真实连接凭据。不修改或重新生成 `Config/littleAnt.db` 来完成普通代码验证。
- SQL 初始化脚本不等同于迁移。变更数据结构时同时评估新库和存量库；当前内置 SQLite 文件不含业务 `flow` 表。
- Shell 四个配置文件以 `PreserveNewest` 复制；不要声称重新构建或发布一定保留目标数据库和现场配置。

## 验证与交付

在仓库根目录按变更范围执行：

```powershell
dotnet restore Tm.Ant.LgaRework.slnx
dotnet build Tm.Ant.LgaRework.slnx --no-restore
git diff --check
```

- C# 排版变更可使用 `dotnet format whitespace --folder src --verify-no-changes`。历史完整 solution 格式检查存在 WPF 工作区限制，失败时区分工具限制与源码错误。
- 修改发布或资源复制行为时验证 Shell 的 `win-x64` Release 发布及输出文件。
- 修改 XAML、项目文件或 JSON 时检查其解析和资源路径。纯文档变更检查事实、路径、链接和 diff 即可。
- 仓库没有自动化测试项目。重要业务变更使用替身 IO/SFC、临时数据库或模拟设备进行有针对性的隔离验证；不要连接生产服务完成普通验证。
- 修改质量判定至少覆盖完整 OK、缺失结果、NG、额外穴位、零需拆和重复结果；修改交互同时覆盖失败反馈、清零顺序及取消。
- 构建成功不等于 Windows GUI 或现场设备验收；区分此次实际执行的检查、历史记录及尚未验证部分，不沿用历史检查数量或告警数量作为本次结果。
- 行为、配置或部署方式变化时同步更新 `README.md` 及相关 `doc/` 说明。交付时说明改动、实际验证和剩余限制。
