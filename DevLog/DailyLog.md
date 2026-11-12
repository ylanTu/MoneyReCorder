# 📋 MoneyReCorder 开发日志

> 项目起始日：2026-06-05
> 最后更新：2026-11-07

---

## 🚀 编译运行指南

### 前提条件
- .NET 10.0 SDK（`dotnet --version` 验证）
- 确保 `dotnet` 在 PATH 中（`C:\Program Files\dotnet\`）

### 标准构建步骤

```bash
# 1️⃣ 进入项目目录
cd f:\Project\MoneyReCorder

# 2️⃣ 恢复 NuGet 包（首次或包变更时）
dotnet restore src/MoneyReCorder/MoneyReCorder.csproj

# 3️⃣ 编译项目（验证代码正确性）
dotnet build src/MoneyReCorder/MoneyReCorder.csproj

# 4️⃣ 运行应用
dotnet run --project src/MoneyReCorder/MoneyReCorder.csproj
```

> **注意**: 如 `dotnet` 命令找不到，使用完整路径 `& "C:\Program Files\dotnet\dotnet.exe"`

### 常见编译问题及修复

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| `MC3074: 命名空间中不存在标记"CartesianChart"` | LiveCharts 包版本或命名空间大小写错误 | 使用 `LiveChartsCore.SkiaSharpView.WPF` (大写 PF) 并升级到 2.0.4+ |
| `CS0234: 命名空间"SkiaSharp.Views"中不存在"Wpf"` | 命名空间大小写 Wpf → WPF | 改为 `SkiaSharp.Views.WPF` |
| `NU1701: Package restored using .NETFramework` | 包未提供 net10.0 目标 | 不影响运行，仅警告 |
| `CS0103: 不存在名称"Path/Directory/File"` | 缺少 `using System.IO` | 添加 `using System.IO;` |
| Spnie 相关错误 | Spine 运行时未安装 | 在 csproj 中注释 `SPINE_ENABLED` |

### NuGet 包版本 (已验证兼容 .NET 10)

| 包名 | 版本 | 说明 |
|------|------|------|
| `CommunityToolkit.Mvvm` | 8.4.2 | MVVM 源生成器 |
| `Microsoft.Data.Sqlite` | 9.0.3 | SQLite 数据库 |
| `LiveChartsCore.SkiaSharpView.WPF` | 2.0.4 | ⚠️ 注意大小写: WPF 大写 |
| `Microsoft.Extensions.DependencyInjection` | 10.0.0-preview.3 | DI 容器 |

### 启动行为

应用启动后会同时打开 **3 个窗口**:
1. **主窗口** — 完整记账功能（默认隐藏，通过小组件点击 📂 打开）
2. **小组件** — 桌面悬浮余额窗口（右下角）
3. **桌宠** — Spine 桌宠窗口（左下角，无 Spine 时显示占位）

---

## 📁 项目文件索引与工作指引

### 1. 核心文件路径速查

| 分类 | 文件路径 | 说明 |
|------|---------|------|
| **项目文件** | `MoneyReCorder.sln` | 解决方案入口，用 VS 打开 |
| **项目配置** | `src/MoneyReCorder/MoneyReCorder.csproj` | NuGet 包、目标框架 |
| **应用入口** | `src/MoneyReCorder/App.xaml(.cs)` | DI 容器注册、窗口启动、数据库初始化 |
| **主题样式** | `src/MoneyReCorder/Styles/Themes.xaml` | 全局颜色、卡片、按钮、导航样式 |

### 2. 架构分层路径

```
src/MoneyReCorder/
├── Models/          ← 数据模型 (POCO)
│   ├── Account.cs
│   ├── Transaction.cs
│   ├── TransactionCategory.cs
│   └── Enums.cs
├── Data/            ← 数据访问层 (Repository 模式)
│   ├── DatabaseContext.cs         ← SQLite 建表 + 种子数据
│   ├── AccountRepository.cs
│   ├── TransactionRepository.cs
│   ├── CategoryRepository.cs
│   ├── IAccountRepository.cs
│   ├── ITransactionRepository.cs
│   └── ICategoryRepository.cs
├── Services/        ← 业务逻辑层
│   ├── AccountService.cs / IAccountService.cs
│   ├── TransactionService.cs / ITransactionService.cs
│   ├── CategoryService.cs / ICategoryService.cs
│   ├── ThemeService.cs / IThemeService.cs
│   ├── SyncService.cs / ISyncService.cs     ← 🔌 云同步预留
├── ViewModels/      ← MVVM ViewModel (CommunityToolkit.Mvvm)
│   ├── MainViewModel.cs         ← 总览 + 页面导航
│   ├── WidgetViewModel.cs       ← 桌面小组件逻辑
│   ├── AccountViewModel.cs      ← 账户管理
│   ├── TransactionViewModel.cs  ← 交易流水 + 记账
│   ├── StatisticsViewModel.cs   ← 统计图表
│   └── SettingsViewModel.cs     ← 设置
├── Views/           ← WPF 窗口/页面
│   ├── MainWindow.xaml(.cs)     ← 主窗口（侧边栏导航）
│   ├── WidgetWindow.xaml(.cs)   ← 桌面悬浮小组件
│   ├── PetWindow.xaml(.cs)      ← Spine 桌宠
│   ├── DashboardView.xaml       ← 总览页
│   ├── AccountView.xaml         ← 账户管理页
│   ├── TransactionView.xaml     ← 交易流水页
│   ├── StatisticsView.xaml      ← 统计图表页
│   └── SettingsView.xaml        ← 设置页
├── Controls/        ← 自定义控件
│   └── SpineView.cs             ← Spine 动画控件
├── Spine/           ← Spine 渲染引擎
│   └── SpineRenderer.cs         ← SkiaSharp 骨骼渲染器
├── Converters/      ← 值转换器
│   ├── BooleanToVisibilityConverter.cs
│   └── TransactionTypeToSymbolConverter.cs
└── Libs/            ← 外部库 (Spine.dll 等手动引用)
```

### 3. 关键约定与规范

#### 数据规范
- **金额存储**: 所有金额用 `long` (分) 存储，显示时 `/ 100m` 转 `decimal`
- **软删除**: 所有表使用 `IsDeleted` 字段标记删除，不物理删除
- **自动更新余额**: 添加/修改/删除交易时自动重新计算账户余额
- **种子数据**: 启动时自动初始化默认交易分类（11个支出 + 5个收入）

#### 命名规范
- 接口: `I` 前缀 (如 `IAccountService`)
- ViewModel: `XxxViewModel`，继承 `ObservableObject`
- 命令: `[RelayCommand]` 生成 `XxxCommand`
- 可观察属性: `[ObservableProperty]` 生成 `Xxx`

#### DI 注册 (App.xaml.cs)
- **Singleton**: DatabaseContext, 所有 Repository, 所有 Service
- **Transient**: 所有 ViewModel（每个页面独立实例）

#### 窗口生命周期
- 启动时：MainWindow(隐藏) + WidgetWindow(显示) + PetWindow(显示)
- 关闭主窗口 → 隐藏而非退出
- 关闭小组件/桌宠 → 若再无窗口则退出

---

## 🗺️ 已实现功能 (MVP v0.1)

- [x] 项目架构搭建 (.NET 10 WPF + MVVM)
- [x] SQLite 数据库 + Repository 数据层
- [x] 账户管理（CRUD + 小组件显隐控制）
- [x] 交易记录（记账 + 筛选 + 删除）
- [x] 桌面悬浮小组件（余额显示 + 紧凑/锁定模式）
- [x] 主窗口总览页面（资产概览 + 账户列表 + 最近交易）
- [x] 统计分析（月度柱状图 + 分类饼图）
- [x] 主题系统（深色模式 + 主题色 + 透明度）
- [x] Spine 桌宠框架（SpineView 控件 + SkiaSharp 渲染）
- [x] 云端同步接口预留 (ISyncService)

---

## 🎯 后续开发步骤

### Phase 1 — 核心功能完善 (优先级: 🔴 高)

- [ ] **1.1 编译并运行调试**
  - 执行 `dotnet restore` 安装所有 NuGet 包
  - 执行 `dotnet build` 编译，修复编译错误
  - 执行 `dotnet run` 启动验证基本功能
- [ ] **1.2 WidgetWindow 交互完善**
  - 修复点击穿透逻辑（锁定/解锁切换）
  - 修复展开/折叠交互
  - 添加右键菜单（退出应用、打开设置）
- [ ] **1.3 交易列表优化**
  - 添加分页/虚拟滚动（大数据量支持）
  - 添加批量删除功能
  - 添加编辑交易（双击修改）
- [ ] **1.4 统计页面数据联动**
  - 年份选择触发图表刷新
  - 添加年度支出分类统计
  - 添加月度收入趋势对比

### Phase 2 — 体验增强 (优先级: 🟡 中)

- [ ] **2.1 账单导入**
  - 支持 CSV 格式导入（CsvHelper NuGet 包）
  - 支持 Excel 格式导入（EPPlus 或 ClosedXML）
  - 字段映射配置界面
- [ ] **2.2 桌面小组件增强**
  - 多皮肤/主题支持
  - 自定义显示字段
  - 动画过渡效果
- [ ] **2.3 数据导出**
  - 导出为 CSV/Excel
  - 导出月度报告 PDF
- [ ] **2.4 搜索过滤增强**
  - 全文搜索（备注/分类）
  - 高级筛选器（金额范围、标签）
  - 保存常用筛选条件

### Phase 3 — 高级功能 (优先级: 🟢 低)

- [ ] **3.1 Spine 桌宠增强**
  - 获取 Spine C# Runtime 并集成
  - 准备 Spine 动画资源
  - 实现多动画状态机（空闲/开心/触摸）
  - 桌宠交互反馈（点击动画）
- [ ] **3.2 云端同步**
  - 实现 ISyncService 接口
  - 数据冲突解决策略
  - 同步状态指示器
- [ ] **3.3 预算管理**
  - 月度预算设置
  - 预算超支预警
  - 预算执行进度条
- [ ] **3.4 OCR 识别**
  - 集成 Tesseract OCR 或 Windows.Media.Ocr
  - 截图识别账单
  - 自动填充交易信息
- [ ] **3.5 多币种支持**
  - 扩展 Currency 字段
  - 汇率查询接口
  - 自动换算

### Phase 4 — 质量与发布 (优先级: 🔵 基础)

- [ ] **4.1 单元测试**
  - Service 层单元测试
  - Repository 层集成测试
  - ViewModel 测试
- [ ] **4.2 错误处理**
  - 全局异常捕获
  - 用户友好的错误提示
  - 日志记录 (Serilog/NLog)
- [ ] **4.3 性能优化**
  - 大数据量下交易列表虚拟化
  - 图表渲染优化
  - 数据库查询优化（索引）
- [ ] **4.4 打包发布**
  - 配置图标
  - 创建安装包 (WiX / Squirrel)
  - 自动更新机制

---

## 📝 开发记录

### 2026-11-07 — 皮肤系统重做 / 窗口透明度 / 浮窗不遮挡 / 设置页重构

#### 1. 🎨 皮肤系统（Themes.xaml + ThemeService 重做）
- 新增 `Services/ThemePreset.cs`：**10 套皮肤**（5 组明暗配对）
  - 薄荷晨光 ↔ 森林夜曲、海洋微澜 ↔ 深海夜蓝、樱花初雪 ↔ 玫瑰暮色
  - 日暮琥珀 ↔ 琥珀余晖、极简纸白 ↔ 玄墨石墨
  - 每套皮肤包含：主题色、窗口背景、卡片背景、次级表面、主/次文字、边框
- **修复致命问题**：`Themes.xaml` 里的画刷用 `Color="{DynamicResource ...}"` 声明，
  资源字典内的 Freezable **不会**跟随 Color 资源变化重算 →
  之前「深色模式 / 主题色」实际上只有渐变等直接引用的地方变色，整体界面不变。
  - 现在 `ThemeService.ApplyAllColors()` 同时更新 Color 资源 **和** 直接改写画刷实例的 `Color`
  - 全部视图中画刷引用由 `StaticResource` 改为 `DynamicResource`（29 处），保证画刷被替换时也能同步
- 派生色算法：`PrimaryDark = 主题色×0.8`，`PrimaryLight = 主题色混入背景色 86%`（深浅主题都协调）
- `ResetToDefault()` / `SetDarkMode()`（自动切配对皮肤）/ `SetAccent()` 全部即时生效

#### 2. 🪟 窗口透明度（原滑块是摆设）
- 原 `WidgetOpacity` 从未被任何窗口绑定，滑块拖动无效果
- 新增三档透明度并真正生效：**小组件**（0.2–1.0，默认 0.92）、**主窗口**（0.6–1.0）、**桌宠**（0.2–1.0）
- 小组件通过 `Window.Opacity` 绑定 VM；主窗口在 code-behind 订阅 `AppearanceChanged` 同步；桌宠同理
- 设置保存改为 **350ms 防抖写盘**，拖滑块不再高频 IO

#### 3. 🧩 浮窗不再悬浮遮挡视野
- `PetWindow` 移除硬编码 `Topmost="True"`；小组件 / 桌宠的置顶改为**可选开关**（默认关闭）
- 新增「打开主窗口时自动隐藏浮窗」（默认开启）：主窗口显示 → 浮窗让位；关闭/最小化 → 自动恢复
- 桌宠原来在没有 Spine 资源时是 **200×300 全透明却可点击的置顶窗口**（会挡住桌面点击）
  - 改为 116×136 的圆形占位形象（主题色描边 + 💰 图标），尺寸与可见内容一致
- 小组件「锁定」逻辑重写：原来默认 `IsHitTestVisible=false` 会导致整个浮窗无法点击且无法解锁（死锁）
  - 现在锁定 = 固定位置（禁止拖动），按钮始终可点
- 新增 **托盘图标**（`UseWindowsForms` + `NotifyIcon`）：打开主窗口 / 显示小组件 / 显示桌宠 / 退出
  - 解决「浮窗全隐藏后找不到入口」的死路
- 小组件补全右键菜单 + 标题栏按钮（展开收起 / 锁定 / 打开主窗口 / 更多），并补上原先缺失的命令绑定

#### 4. ⚙️ 设置页重构（更清晰）
- 左栏：皮肤画廊（10 张带配色预览的卡片，选中高亮）→ 深色模式 → 主题色（含当前色值徽标）→ 桌面浮窗开关组
- 右栏：实时预览（模拟桌面背景 + 小组件透明效果）→ 三档透明度滑杆
- **常驻底部状态栏**：状态提示 + 恢复默认，滚动时也始终可见
- 移除页面内重复标题（顶部栏已显示「设置」），所有开关用自定义 Switch 样式、滑杆用自定义模板

#### 5. 🐞 顺带修复
- **总览页数据全为 0**：`DashboardView` 自己 `GetService<MainViewModel>()`（Transient）导致拿到另一个空实例 →
  改为继承 MainWindow 的 DataContext，现在与侧边栏共享同一份数据
- 顶部栏显示的页面名是英文 key（`Settings`）→ 新增 `MainViewModel.CurrentPageTitle` 映射为中文
- 侧边栏导航高亮固定在第一项 → 新增 `PageEqualsConverter`，`IsChecked` 跟随 `CurrentPage`
- 深色模式下多处写死的浅色（`#F8F9FA` 行背景、`#F0F4F8` 悬停、`#4CAF50/#FF6B6B` 金额色）→ 统一改用主题资源
- 新增资源：`SurfaceColor/SurfaceBrush`（行背景、悬停、滑杆槽）
- 新增样式：`SwitchStyle`、`AppSliderStyle`、`ColorSwatchButtonStyle`、`WidgetIconButtonStyle`

#### 验证
- `dotnet build` 通过，0 error / 0 CS 警告
- 用 RenderTargetBitmap 离屏截图逐帧核对：浅色设置页、深色皮肤、总览页数据均正确
- 注意：同时启用 WPF + WinForms 时需 `<Using Remove="System.Windows.Forms" />`、`System.Drawing`，否则类型歧义

---

### 2026-11-07（二）— 桌面浮窗改为「小球 + 悬停展开 + 双击进设置」

#### 1. 🟢 三种可切换形态（`WidgetDisplayMode`）
- 新增 `Models/WidgetDisplayMode`：`Ball`（默认）/ `Capsule` / `Card`，随设置持久化到 settings.json
- 默认形态 = **小球**：56px 圆形，主题色渐变 + 💰 + 缩写金额（`¥1.2万` / `¥98`）
- 胶囊 = 横向小长条，直接显示完整金额；完整卡片 = 常驻展开（旧版行为）
- 切换入口有两处：设置页「悬浮窗形态」三选一卡片；浮窗右键菜单「悬浮窗形态」子菜单

#### 2. 🖱️ 悬停展开 / 离开自动收起
- `WidgetViewModel.IsExpanded` + `CanExpand`，派生 `ShowBall / ShowCapsule / ShowCard` 控制三个视觉层的可见性
- `WidgetWindow` 用 `MouseEnter` → 展开、`MouseLeave` → 180ms 延时收起（`IsMouseOver` 二次确认，避免移向按钮时闪收）
- 展开时卡片淡入 130ms（QuadraticEase）
- 窗口改为 `SizeToContent="WidthAndHeight"`，尺寸随形态自适应（68×68 / 112×52 / 292×180）

#### 3. 📍 位置记忆 + 不漂移的展开锚点
- 记录「收起状态下窗口中心」作为锚点：展开时按锚点靠近屏幕哪一侧/哪一半，让小球贴着最近的角落长出来，**小球落点始终不变**
- 拖动采用 4px 阈值判定后才进入 `DragMove()`，单击不会误拖、也不影响双击识别
- 锚点写入 `settings.json`（`WidgetAnchorX/Y`），重启后回到原处；越界自动夹回工作区

#### 4. ⚙️ 双击 → 打开主窗口并进入设置页
- `App.OpenSettings` → `ShowMainWindowCore()` + `MainViewModel.CurrentPage = "Settings"`
- 卡片头部也补了 ⚙️ 按钮，悬停时不方便双击也能一键进设置

#### 5. 其他
- 新增 `Converters/StringFallbackConverter`：账户图标为空时回退 🏦（原来「微信支付」行图标是空白）
- 移除旧的「展开/收起」按钮（改由悬停接管），标题栏按钮精简为 锁定 / 主窗口 / 设置 / 更多
- 验证方式：临时在 `App.OnStartup` 挂自检（真实光标移到小球 → 抓屏 → 复位光标），
  结果 `68×68 → 292×180（expanded=True）→ 68×68`，主窗口 `page=Settings`，验证完已移除该代码

---

### 2026-11-07（三）— 深色皮肤补全系统控件 + 去掉重复悬浮球

#### 1. 🧩 深色下的「白块」根因
排查发现所有系统控件都用 Windows 默认浅色模板，换肤根本影响不到它们：
`ComboBox` / `ComboBoxItem` / `DatePicker` / `DatePickerTextBox` / `Calendar` / `CalendarItem` /
`CalendarDayButton` / `CalendarButton` / `ContextMenu` / `MenuItem` / `Separator` / `ListBox` /
`ListBoxItem` / `ScrollBar` / `Thumb` / `ToolTip`，另外还有**系统标题栏**。

#### 2. 🎨 在 `Styles/Themes.xaml` 新增「系统控件深色适配」段
- 下拉框：ToggleButton + ContentPresenter + Popup，圆角深色浮层，选中项用 `PrimaryLightBrush`
- 日期选择器：手写 `DatePicker` 模板（`PART_TextBox` / `PART_Button` / `PART_Popup`），输入框透明贴在圆角底板上
- 日历：完整重写 `CalendarItem` 模板（`PART_PreviousButton` / `PART_HeaderButton` / `PART_NextButton` /
  `PART_MonthView` 7×7 / `PART_YearView` 4×3），日期格用 `CalendarDayButton` 自定义模板（今天描边、选中填主题色）
- 右键菜单 / 气泡提示 / 滚动条全部换主题色
- 关键坑 1：`CalendarItem` 模板里星期标题的 `DataTemplate` **键名必须是 `DayTitleTemplate`**（WPF 内部按此键查找）
- 关键坑 2：**不能**往 `PART_MonthView` 里塞自定义元素，Calendar 会按 Children 索引取日期按钮 → `NullReferenceException`
- 关键坑 3：WPF 自带的星期标题内容实际为空，改为在月视图上方单独画一行 `UniformGrid` 标题（7 等分，与 30px 日期格天然对齐）

#### 3. 🪟 系统标题栏跟随深浅色
`MainWindow.OnSourceInitialized` 调用 `DwmSetWindowAttribute(hwnd, 20 /*immutable dark mode*/, ...)`，
失败再回退属性 `19`（Win10 1809~1903）；`AppearanceChanged` 时重新应用。

#### 4. 📊 统计页图表
- `CartesianChart` / `PieChart` 设 `Background=Transparent`，不然是一整块白底
- `StatisticsViewModel` 注入 `IThemeService`，新增 `XAxes` / `YAxes` / `ChartLegendPaint`，
  按深浅色给 `LabelsPaint` / `SeparatorsPaint` 上色，皮肤切换时重建
- 饼图空数据占位由 `SKColors.LightGray` 改为主题灰（深色 `#2A3242`）
- 饼图宽度 400 → 340，修掉横向滚动条

#### 5. 🟢 去掉多余的那个「球」
根因：没有 Spine 资源时，`PetWindow` 会被渲染成一个 💰 圆形占位——和悬浮球撞衫。
- `ThemeService.PetAvailable`（`#if SPINE_ENABLED`）+ `Normalize()` 里强制 `ShowPet = false`
- `ShowPet` 的 getter/setter 都带 `&& PetAvailable`，旧配置里的 `true` 会被吞掉
- `App` 里只在 `PetAvailable` 时才 `new PetWindow()`；托盘菜单也不再显示「显示桌宠」
- 设置页该项置灰，副标题写明「需要 Spine 骨骼资源，当前版本不可用」

#### 6. 其他
- `MainWindow` 根 `Grid` 补 `Background="{DynamicResource BgPrimaryBrush}"`（离屏渲染时也正确）
- 新增流水页「新增交易」表单上边距 48 → 58，不再压住「记一笔」按钮
- 验证方式：临时自检脚本（离屏 `RenderTargetBitmap` + 弹层单独抓屏），核对
  流水 / 账户 / 统计 / 设置 / 日历 / 下拉 / 右键菜单 在深色与浅色两套皮肤下的表现，验证完已移除

---

### 2026-06-05 — 项目启动与 MVP 搭建
- 完成需求分析与整理
- 创建 WPF 项目结构（5层架构：Model → Data → Service → ViewModel → View）
- 实现 SQLite 数据层（3个 Repository）
- 实现业务逻辑层（4个 Service + 1个预留接口）
- 实现 MVVM ViewModel 层（6个 ViewModel）
- 实现 UI 层（3个 Window + 5个 Page View）
- 实现桌面悬浮小组件（WidgetWindow）
- 实现 Spine 桌宠框架（SpineView + SpineRenderer）
- 实现主题系统（深色模式、主题色、透明度）

### 2026-06-06 — 项目结构确认与开发日志创建
- 创建 DevLog 开发日志目录
- 编写 DailyLog.md（文件索引 + 开发规范 + 后续步骤）
- 刷新项目文件结构认知

### 2026-06-06 — 编译修复与运行验证
- **问题**: `dotnet restore` 因 NuGet 源问题长时间挂起
- **解决**: 改用完整路径 `C:\Progra~1\dotnet\dotnet.exe` 执行
- **问题1**: LiveCharts `CartesianChart` XAML 找不到
  - **原因**: 命名空间 `LiveChartsCore.SkiaSharpView.Wpf` 应改为 `WPF` (大写)
  - **解决**: 升级 LiveCharts 2.0.0-rc3.3 → 2.0.4，修正 XML 命名空间大小写
- **问题2**: Spine 代码引用了不存在的 Spine 运行时和 SkiaSharp.Views.Wpf
  - **解决**: 添加 `#if SPINE_ENABLED` 条件编译，默认不启用
- **问题3**: `System.IO` 命名空间在 .NET 10 中未隐式导入
  - **解决**: 在 DatabaseContext.cs 和 ThemeService.cs 添加 `using System.IO`
- **结果**: ✅ 编译成功，零错误

### 2026-06-06 — 5项问题优化修复

#### 1. 🪟 小组件悬浮层级
- **问题**: `Topmost="True"` 导致小组件悬浮在所有窗口之上
- **修复**: 移除 `Topmost="True"`，小组件现在仅在桌面层级显示

#### 2. 🎨 UI 主题重构 — 磨砂玻璃清新风
- **问题**: 卡片样式硬阴影、颜色不统一、无 Modern 感
- **修复**: 
  - 全面重写 `Styles/Themes.xaml`
  - 所有 `{StaticResource}` → `{DynamicResource}` 支持热切换
  - 卡片改用微阴影（`ShadowDepth="0"`, `BlurRadius="16"`）
  - 半透明玻璃质感：`BgCardBrush` 使用 `#FAFFFFFF`
  - 全局圆角统一定义：按钮 10px、卡片 14px
  - 新增 5 套预设配色：薄荷绿、海洋蓝、薰衣草、珊瑚粉、日暮橙

#### 3. 🎯 主题色热切换
- **问题**: 设置页更改主题色后不生效（`StaticResource` 只加载一次）
- **修复**:
  - ThemeService 新增 `ApplyAccent()` 方法，运行时更新 `Application.Current.Resources`
  - 自动计算 `PrimaryDarkColor`（加深 15%）和 `PrimaryLightColor`（淡化 70%）
  - 深色模式支持完整的深/浅色切换（文字/背景/卡片全变）
  - `SetAccentColor` 命令即时更新，无需点保存即可预览

#### 4. 🛡️ 崩溃保护
- **问题**: 定时器跨线程更新 UI 导致崩溃；无异常处理
- **修复**:
  - `WidgetViewModel`: `System.Threading.Timer` → `DispatcherTimer`（UI 线程安全）
  - `App.xaml.cs`: 添加三层全局异常捕获（`UnhandledException` / `DispatcherUnhandledException` / `UnobservedTaskException`）
  - `MainWindow`: 启动时 `LoadDataAsync` 添加 try-catch
  - 所有异步刷新方法增加 `SafeRefresh()` 包装

#### 5. 📐 其他修复
- 主题预设名从英文改为中文：薄荷绿/海洋蓝/薰衣草/珊瑚粉/日暮橙
- `PetWindow` 和 `WidgetWindow` 清理未使用的拖拽字段
- `SpineView` 警告修复（`IsLoaded` 加 `new` 关键字）

### 2026-06-08 — 主窗口无法打开修复

- **问题**: 主窗口打不开，点击小组件📂无反应
- **根因分析**:
  1. `Themes.xaml` 重写时删除了 `SurfaceBrush` 资源键，但 `MainWindow.xaml` 侧边栏和顶部栏仍引用 `{StaticResource SurfaceBrush}`
  2. 运行时找不到 `StaticResource` 导致 `XamlParseException`，MainWindow 构造失败
  3. 全局 `DispatcherUnhandledException` 吞掉了异常（`args.Handled = true`），导致静默失败无提示
- **修复**:
  - `MainWindow.xaml` 两处 `{StaticResource SurfaceBrush}` → `{DynamicResource BgCardBrush}`
  - `App.xaml.cs` 异常处理优化：XamlParseException 输出 `Debug.Fail`，其他异常记录详情
  - 添加 `using System.Diagnostics` 和 `using System.Windows.Threading`

---

> **维护说明**: 每次完成一个功能后，在此日志中更新状态（将 `- [ ]` 改为 `- [x]`），并添加开发记录。
