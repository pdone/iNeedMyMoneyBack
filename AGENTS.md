# iNeedMyMoneyBack

C# WPF 桌面应用（中国股票监控工具）。单项目，无 CI。

## 构建

```
# 在 src/ 目录下
msbuild iNeedMyMoneyBack.sln /p:Configuration=Release
# 或用 Visual Studio 2022+ 打开 iNeedMyMoneyBack.sln
```

- 目标框架：.NET Framework 4.7.2（旧式 csproj，非 SDK 风格）
- C# LangVersion 12.0
- 输出：单个 EXE，通过 Costura.Fody（IL 合并所有 DLL 到 exe 中）
- 不支持 `dotnet build` —— 需要完整 MSBuild / Visual Studio

## 架构

```
src/
  App.xaml.cs          — 入口点，单实例互斥锁，程序集元数据
  MainWindow.xaml.cs   — 主窗口：轮询循环、数据格式化、托盘图标、快捷键
  ConfigWindow.xaml.cs — 配置界面（DataGrid + 复选框），吸附在主窗口旁边
  Utils.cs             — 多语言字典、JSON 工具、CJK 可视宽度填充、交易时间判断、Win32 互操作、Logger
  Entity/
    Config.cs          — Config + StockConfig + StockConfigArray（全部持久化为 JSON，支持版本迁移）
    StockInfo.cs       — 解析腾讯 qt.gtimg.cn 管道分隔响应（字段索引使用命名常量）
  Resources/           — App.ico、App.png、Tray.ico
  iNeedMyMoneyBack.Tests/ — NUnit 单元测试项目
fonts/                 — 内置阿里巴巴普惠体 3、CascadiaMono TTF 字体
```

## 运行测试

```
# 在 src/ 目录下，需要 vstest.console.exe 或 Visual Studio Test Explorer
msbuild iNeedMyMoneyBack.Tests/iNeedMyMoneyBack.Tests.csproj /p:Configuration=Debug
vstest.console iNeedMyMoneyBack.Tests/bin/Debug/iNeedMyMoneyBack.Tests.dll
```

## 关键模式

- **数据来源**：`http://qt.gtimg.cn` —— 腾讯股票 API，返回 `~` 分隔的字段。响应解析在 `StockInfo.Get()` 中，使用命名常量索引，有长度校验。
- **股票代码格式**：`sh600519`、`sz300750`、`bj899050`（需要前缀）。内置指数：`sh000001`、`sz399001`、`sz399006`、`sz399300`、`bj899050`。
- **配置持久化**：`%AppData%/iNeedMyMoneyBack/config.json` 和 `stocks.json`。首次运行时自动写入默认值。Config 有版本号字段，支持迁移。
- **多语言**：`Utils.cs` 中的内联字典（`LanguageDatas`），键为 `cn`/`en`。新增字符串在该字典中添加，不要用资源文件。
- **线程**：`BackgroundWorker` 轮询循环，`Thread.Sleep()` 阻塞。共享状态通过 `g_dataLock` 同步。UI 更新通过 `Dispatcher.Invoke`。
- **全局快捷键**：`Ctrl+~`（反引号/切换显示），通过 Win32 `RegisterHotKey` 在 `MainWindow.OnSourceInitialized` 中注册。
- **窗口拖动**：不使用 WPF 拖动 —— 通过 P/Invoke 调用 Win32 `SendMessage(WM_SYSCOMMAND, SC_MOVE)`。

## 修改股票数据解析

`StockInfo.Get()`（`src/Entity/StockInfo.cs`）使用命名常量（`IDX_NAME`、`IDX_CURRENT_PRICE` 等）映射腾讯 API 字段。修改时更新对应常量即可。`MIN_FIELD_COUNT` 定义了最少字段数，不足时返回 null。

## 添加新菜单项

1. 在 `MainWindow.xaml` 的 ContextMenu 中添加 `<MenuItem Name="menu_xxx" ...>`
2. 在 `MainWindow.xaml.cs` 的 `OnMenuItemClick(MenuItem)` 中添加 case 处理
3. 在 `Utils.cs` 的 `LanguageDatas` 中添加多语言键 `"menu_xxx"`
4. 在 `InitLang()` 中调用 `SetMenuItemHeader(menu_xxx, "X")`

## 注意事项

- `Config.Load()` 和 `StockConfigArray.Load()` 静默吞掉异常（仅记录日志），返回空/默认对象。
- `Costura.Fody` 编织器在构建时嵌入所有依赖 —— 输出目录只有一个大的 `.exe` 文件。
- `IsTradingTime()` 不检查中国法定节假日 —— 仅判断周一到周五 + 时间窗口。
- `fonts/` 目录中的 TTF 文件作为资源内置，不会安装到系统。
- 测试项目引用主项目，通过 `InternalsVisibleTo` 访问 internal 类型。
