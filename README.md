# MoneyReCorder

> 一个安静的 Windows 桌面记账工具 —— 手动记账、纯本地存储、不采集任何用户信息。

## 💡 为什么会有这个软件

平时我更习惯待在电脑前，手机 App 能用，但总觉得不顺手。而现实里的钱又散落在好几个地方：银行卡、微信、支付宝、各种零钱账户。想弄清「这个月到底花了多少、现在还剩多少」，得来回切好几个应用，翻完就忘了。

MoneyReCorder 就是把这件事收拢到一个地方：一台常驻桌角的 Windows 小工具，把分散在各处的收支手动记到一处，一眼看到全貌，不用为了看一个数字去开手机。

它对自己的边界很克制：

- **不采集任何用户信息** —— 没有账号体系、没有埋点、没有联网上报
- **不读你的账单** —— 不连接银行、不申请支付权限，记什么、记多少完全由你决定
- **数据只留在你自己的电脑上** —— SQLite 单文件存储，随时可备份、可迁移、可删除

一句话：你手动记，它安静帮你算。

## 📋 项目概述

一款 Windows 桌面个人账目管理软件，支持记账、账户管理、消费统计和桌面悬浮余额显示。

### 技术栈

- **框架**: WPF (.NET 10.0)
- **架构**: MVVM (CommunityToolkit.Mvvm)
- **数据库**: SQLite (Microsoft.Data.Sqlite)
- **图表**: LiveChartsCore (SkiaSharp)

## 🚀 快速开始

### 前置条件

- .NET 10.0 SDK 或更高版本
- Visual Studio 2022+ (推荐) 或 VS Code

### 构建与运行

```bash
# 克隆后进入项目目录
git clone https://github.com/ylanTu/MoneyReCorder.git
cd MoneyReCorder

# 恢复 NuGet 包
dotnet restore src/MoneyReCorder/MoneyReCorder.csproj

# 编译
dotnet build src/MoneyReCorder/MoneyReCorder.csproj

# 运行
dotnet run --project src/MoneyReCorder/MoneyReCorder.csproj
```

> **注意**: 如果 `dotnet` 命令找不到，直接使用完整路径：
> ```powershell
> & "C:\Program Files\dotnet\dotnet.exe" build src/MoneyReCorder/MoneyReCorder.csproj
> ```

### 启动后

应用启动后会打开 **托盘图标 + 桌面浮窗**（是否显示由设置决定）：
| 窗口 | 位置 | 说明 |
|------|------|------|
| 💳 **桌面小组件** | 右下角 | 默认一颗小球，鼠标悬停展开明细，双击进设置 |
| 📒 **主窗口** | 居中 | 完整记账功能，侧边栏导航（从小组件 / 托盘打开） |

> 打开主窗口时浮窗会自动隐藏（可在设置中关闭），关闭主窗口后自动恢复；
> 浮窗全部隐藏也不会失联，托盘图标随时可找回。

### 桌面浮窗默认行为

- 不置顶（`Topmost = false`），不会压在其他窗口之上影响视野，可在设置中开启置顶
- **默认形态是小球**：只占 56×56，不遮挡桌面；鼠标移上去展开详情卡片，移开自动收起
- 三种形态可在「设置 → 悬浮窗形态」或浮窗右键菜单切换：小球 / 胶囊 / 完整卡片
- **双击浮窗** → 打开主窗口并直接进入设置页
- 拖动可移动位置，落点会记住；锁定后位置固定
- 透明度独立可调（小组件 / 主窗口两档）

## 📁 项目结构

```
MoneyReCorder/
├── src/MoneyReCorder/
│   ├── App.xaml(.cs)           # 应用入口 + DI 容器
│   ├── MainWindow.xaml(.cs)    # 主窗口（侧边栏导航）
│   ├── WidgetWindow.xaml(.cs)  # 桌面悬浮小组件
│   ├── Models/                 # 数据模型
│   │   ├── Account.cs          # 账户模型
│   │   ├── Transaction.cs      # 交易记录
│   │   ├── TransactionCategory.cs  # 交易分类
│   │   └── Enums.cs            # 枚举定义
│   ├── ViewModels/             # ViewModel 层
│   │   ├── MainViewModel.cs    # 主窗口
│   │   ├── WidgetViewModel.cs  # 小组件
│   │   ├── AccountViewModel.cs # 账户管理
│   │   ├── TransactionViewModel.cs  # 交易管理
│   │   ├── StatisticsViewModel.cs   # 统计
│   │   └── SettingsViewModel.cs     # 设置
│   ├── Views/                  # UI 视图
│   │   ├── DashboardView.xaml  # 总览页面
│   │   ├── AccountView.xaml    # 账户管理
│   │   ├── TransactionView.xaml    # 交易流水
│   │   ├── StatisticsView.xaml     # 统计图表
│   │   └── SettingsView.xaml       # 设置
│   ├── Services/               # 业务逻辑
│   │   ├── AccountService.cs   # 账户服务
│   │   ├── TransactionService.cs   # 交易服务
│   │   ├── CategoryService.cs      # 分类服务
│   │   ├── ThemeService.cs         # 皮肤/主题/窗口设置
│   │   ├── ThemePreset.cs          # 内置皮肤库（10 套配色）
│   │   └── ISyncService.cs         # 云同步接口(预留)
│   ├── Data/                   # 数据访问
│   │   ├── DatabaseContext.cs  # SQLite 初始化
│   │   ├── AccountRepository.cs    # 账户仓储
│   │   ├── TransactionRepository.cs # 交易仓储
│   │   └── CategoryRepository.cs    # 分类仓储
│   ├── Converters/             # 值转换器
│   └── Styles/
│       └── Themes.xaml         # 主题样式
└── README.md
```

## 🎯 功能模块

### 💳 桌面悬浮组件
- 默认一颗 **56px 小球**，只显示总资产（`¥1.2万` 缩写），不遮挡桌面
- **悬停展开**：鼠标移上去展开详情卡片（总资产 + 各账户余额），移开自动收起
- **双击**：打开主窗口并进入设置页
- 三种形态可选：小球 / 胶囊 / 完整卡片（设置页或右键菜单）
- 可拖拽移动，落点记住；锁定后固定位置；支持独立透明度

### 📒 主窗口记账
- **总览**: 总资产、本月收支、最近交易
- **账户管理**: 添加/编辑/删除账户，小组件显隐控制
- **交易流水**: 按日期/账户/分类筛选，记账
- **统计**: 月度收支柱状图 + 分类支出饼图
- **设置**: 皮肤（10 套明暗配色）、主题色、窗口透明度、悬浮窗形态（小球/胶囊/卡片）、浮窗显隐与置顶

## 🔮 未来扩展

- [ ] 云端数据同步 (接口已预留 `ISyncService`)
- [ ] CSV/Excel 账单导入
- [ ] OCR 账单识别
- [ ] 多币种支持
- [ ] 账单导出 (PDF/Excel)
- [ ] 高级统计分析
- [ ] 多语言支持
