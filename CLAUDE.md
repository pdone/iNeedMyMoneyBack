# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

C# WPF 桌面应用 —— 中国股票实时监控工具。从腾讯股票 API 拉取数据，在可定制桌面悬浮窗中展示。单项目结构，无 CI/CD。

## 构建命令

```bash
# 使用批处理脚本（推荐）
RunBuild.bat          # 默认 Debug 编译
RunBuild.bat Release  # Release 编译

# 或手动在 src/ 目录下编译
msbuild iNeedMyMoneyBack.sln /p:Configuration=Release

# 或在 Visual Studio 2022+ 中打开 iNeedMyMoneyBack.sln
```

- 目标框架：.NET Framework 4.7.2（旧式 csproj，非 SDK 风格）
- C# LangVersion：12.0
- 输出：单个独立 EXE，Costura.Fody 在构建时将全部依赖 DLL 合并
- 不支持 `dotnet build` —— 需要 MSBuild 或 Visual Studio

## 运行测试

```bash
# 使用批处理脚本（推荐）
RunTest.bat

# 或手动在 src/ 目录下
msbuild iNeedMyMoneyBack.Tests/iNeedMyMoneyBack.Tests.csproj /p:Configuration=Debug
vstest.console iNeedMyMoneyBack.Tests/bin/Debug/iNeedMyMoneyBack.Tests.dll
# 或者使用 Visual Studio Test Explorer
```

## 架构

```
src/
  App.xaml.cs              — 入口点，单实例互斥锁，程序集元数据
  MainWindow.xaml.cs       — 主窗口：轮询循环、数据格式化、托盘图标、快捷键
  ConfigWindow.xaml.cs     — 配置界面（股票管理/显示字段/更多设置 三Tab页），吸附在主窗口旁
  Utils.cs                 — 多语言字典、JSON 工具、CJK 等宽填充、交易时间判断、Win32 互操作、Logger
  Entity/
    Config.cs              — Config + StockConfig：所有设置持久化为 JSON，带版本号自动迁移
    StockInfo.cs           — 解析腾讯 qt.gtimg.cn 管道分隔响应，字段索引用命名常量
  Resources/               — App.ico、App.png、Tray.ico
  iNeedMyMoneyBack.Tests/  — NUnit 单元测试项目（StockInfoTests.cs, UtilsTests.cs）
fonts/                     — 内嵌的阿里巴巴普惠体 3.0、CascadiaMono TTF 字体
```

### NuGet 依赖

- **Costura.Fody** (6.0.0) — 构建时将依赖 DLL 合并到单个 EXE
- **Hardcodet.NotifyIcon.Wpf** (2.0.1) — 系统托盘图标
- **Pdone.Updater.UI** (1.0.1) — 自动更新功能
- **RestSharp** (112.1.0) — HTTP 客户端，调用股票 API
- **System.Text.Json** (9.0.2) — JSON 序列化/反序列化

## 关键模式与约定

### 数据来源
- 接口：`http://qt.gtimg.cn`（腾讯股票 API），响应为 `~` 分隔的字段
- 解析逻辑在 `StockInfo.Get()`，使用命名常量（`IDX_NAME`、`IDX_CURRENT_PRICE` 等）索引字段，有 `MIN_FIELD_COUNT` 长度校验

### 股票代码格式
- A 股：`sh600519`、`sz300750`、`bj899050`（前缀：sh/sz/bj）
- 美股：`usAAPL`（需先启用 `EnableUS`）
- 港股：`hk00700`（需先启用 `EnableHK`）
- 内置指数（不可添加监控，仅配置中启用显示）：上证指数、深证成指、创业板指、沪深300、北证50、上证50、中证500、中证1000、科创50、恒生指数、国企指数、红筹指数、道琼斯、纳斯达克100

### 市场启用控制（v2.2 新增）
- `EnableUS` 和 `EnableHK` 默认 `false`，需在"更多设置"中启用
- 未启用时：无法添加对应市场股票，对应指数自动隐藏，已有该市场个股自动过滤
- 配置版本升级至 v6

### 股票排序功能（v2.3 新增）
- `SortField`：排序字段，值为 `default`/`changePercent`/`buyPrice`/`cost`/`marketValue`/`dayMake`/`allMake`/`yield`，默认 `default`
- `SortOrder`：排序方式，值为 `asc`/`desc`，默认 `desc`
- `default` 表示按配置列表顺序显示，其他字段支持升序/降序排序
- 配置版本升级至 v7

### 配置持久化
- 目录：`%AppData%/iNeedMyMoneyBack/`（`config.json` + `stocks.json`）
- 首次运行自动写入默认值，`Config` 有 `Version` 字段支持迁移
- `Config.Load()` 和 `StockConfigArray.Load()` 静默吞异常（仅记日志），返回空/默认对象

### 多语言
- `Utils.cs` 中的内联字典 `LanguageDatas`，键为 `cn`/`en`
- 新增字符串在该字典中添加，**不要用资源文件**

### 线程模型
- `BackgroundWorker` 轮询循环，`Thread.Sleep()` 阻塞等待
- 共享状态通过 `g_dataLock` 同步
- UI 更新通过 `Dispatcher.Invoke`

### 全局快捷键
- `Ctrl+~`（反引号）切换显示/隐藏主窗口
- 通过 Win32 `RegisterHotKey` 在 `MainWindow.OnSourceInitialized` 中注册

### 窗口拖动
- 不使用 WPF 拖动，通过 P/Invoke `SendMessage(WM_SYSCOMMAND, SC_MOVE)` 实现

## 添加菜单项的步骤

1. 在 `MainWindow.xaml` 的 ContextMenu 中添加 `<MenuItem Name="menu_xxx" ...>`
2. 在 `MainWindow.xaml.cs` 的 `OnMenuItemClick(MenuItem)` 中添加 case 处理
3. 在 `Utils.cs` 的 `LanguageDatas` 中添加多语言键 `"menu_xxx"`
4. 在 `InitLang()` 中调用 `SetMenuItemHeader(menu_xxx, "X")`

## 注意事项

- `IsTradingTime()` 不检查中国法定节假日，仅判断周一至周五 + 时间窗口
- `fonts/` 中的 TTF 文件作为资源内置，不会安装到系统
- 测试项目通过 `InternalsVisibleTo` 访问主项目的 internal 类型
- 美股和港股数据因接口限制存在延迟，不具备实时参考意义
- 程序日志与配置文件位于 `%AppData%/iNeedMyMoneyBack/`
