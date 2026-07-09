using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hardcodet.Wpf.TaskbarNotification;
using RestSharp;
using static iNeedMyMoneyBack.Utils;

namespace iNeedMyMoneyBack;

public partial class MainWindow : Window
{
    #region 公开成员变量
    public static Config g_conf = new();// 配置数据
    public static StockConfigArray g_conf_stocks = [];// 配置的股票集合
    public static List<StockConfig> g_conf_stocks_with_index = [];// 配置的股票集合 与 重要指数集合 的并集
    public static SolidColorBrush color_bg;// 背景色画刷
    public static SolidColorBrush color_fg;// 前景色画刷
    public static SolidColorBrush color_transparent = new(Colors.Transparent);// 透明画刷
    #endregion

    #region 私有成员变量
    private static StockInfo g_last_res = null;// 上一次请求响应
    private static List<StockInfo> g_last_res_set = [];// 上一次请求响应
    private RestClient g_client;
    private static int g_codeIndex = 0;
    private static readonly object g_dataLock = new();// 共享数据锁
    private readonly BackgroundWorker g_worker = new()
    {
        WorkerSupportsCancellation = true,
    };
    private ConfigWindow g_configWindow;// 配置窗口
    private bool ConfigWindowShow = false;
    private Pdone.Updater.UI.Main g_updater;// 更新程序
    private readonly ImageSource TaskbarIcon = new BitmapImage(new Uri($"pack://application:,,,/iNeedMyMoneyBack;component/Resources/App.ico", UriKind.Absolute));
    private readonly ImageSource PageImage = new BitmapImage(new Uri($"pack://application:,,,/iNeedMyMoneyBack;component/Resources/App.png", UriKind.Absolute));
    private readonly ImageSource TrayIconRes = new BitmapImage(new Uri($"pack://application:,,,/iNeedMyMoneyBack;component/Resources/Tray.ico", UriKind.Absolute));

    // Grid 结构缓存
    private bool _gridStructureDirty = true; // 标志位：Grid 结构是否需要重建
    private List<TextBlock> _headerTextBlocks = []; // 表头 TextBlock 缓存
    private List<List<TextBlock>> _dataTextBlocks = []; // 数据 TextBlock 缓存
    private int _cachedColumnCount; // 缓存的列数
    private int _cachedRowCount; // 缓存的行数
    private bool _cachedShowFieldName; // 缓存的表头显示状态
    private string _cachedExtendContent = ""; // 缓存的扩展字段内容

    public class StockDetailPage
    {
        /// <summary>
        /// 雪球
        /// </summary>
        /// <param name="arg">股票代码 如 sh000001</param>
        /// <returns></returns>
        public static string XueQiu(string arg) => $"https://xueqiu.com/S/{arg}";
        /// <summary>
        /// 同花顺
        /// </summary>
        /// <param name="arg">股票名称 如 上证指数</param>
        /// <returns></returns>
        public static string TongHuaShun(string arg) => $"https://www.iwencai.com/unifiedwap/result?w={arg}";
    }
    /// <summary>
    /// 增加菜单不透明度 避免与主界面重叠时显示不清除
    /// </summary>
    private const double MenuOpacityAdded = 0.3;
    /// <summary>
    /// 告警值 涨跌幅的绝对值大于此值时 界面高亮提示 单位 %
    /// </summary>
    private const int AlertValue = 9;

    #endregion

    #region 初始化界面
    public MainWindow()
    {
        InitializeComponent();
        InitGlobalData();
        InitUI();
    }

    /// <summary>
    /// 初始化全局数据
    /// </summary>
    private void InitGlobalData()
    {
        g_conf = Config.Load();
        g_conf_stocks = StockConfigArray.Load();

        if (g_conf != null && g_conf_stocks.Count == 0)
        {
            g_conf_stocks.Add(new StockConfig("sh600519"));
            g_conf_stocks.Add(new StockConfig("sz300750"));
        }

        // 根据配置过滤指数和个股
        var filteredIndexs = StockConfigArray.GetFilteredImportantIndexs(g_conf.EnableUS, g_conf.EnableHK);
        var filteredStocks = new StockConfigArray();
        foreach (var stock in g_conf_stocks)
        {
            if (stock.Code.StartsWith("us") && !g_conf.EnableUS) continue;
            if (stock.Code.StartsWith("hk") && !g_conf.EnableHK) continue;
            filteredStocks.Add(stock);
        }
        g_conf_stocks_with_index = filteredStocks.Union(filteredIndexs).ToList();

        if (g_client == null)
        {
            g_client = new RestClient(g_conf.Api);
            g_client.AddDefaultHeader("User-Agent", g_conf.UserAgent);
        }
    }

    /// <summary>
    /// 初始化界面
    /// </summary>
    private void InitUI()
    {
        // 界面属性赋值
        Width = g_conf.Width;
        Height = g_conf.Height;
        Left = g_conf.Left;
        Top = g_conf.Top;
        Opacity = g_conf.Opacity;
        Topmost = g_conf.Topmost;
        ShowInTaskbar = g_conf.ShowInTaskbar;
        menu_dark.IsChecked = g_conf.DarkMode;
        menu_topmost.IsChecked = g_conf.Topmost;
        menu_show_in_taskbar.IsChecked = g_conf.ShowInTaskbar;
        menu_data_roll.IsChecked = g_conf.DataRoll;
        menu_debug_mode.IsChecked = g_conf.Debug;
        menu_hide_border.IsChecked = g_conf.HideBorder;
        menu_transparent.IsChecked = g_conf.Transparent;
        DragBlock.Visibility = g_conf.Transparent ? Visibility.Visible : Visibility.Collapsed;

        Resources["BorderThickness"] = new Thickness(g_conf.HideBorder ? 0 : 1);
        Resources["MainOpacity"] = Math.Min(Opacity + MenuOpacityAdded, 1);

        // 界面事件绑定
        PreviewMouseDown += (_, __) => DragWindow(this);
        PreviewMouseWheel += OnPreviewMouseWheel;
        ContextMenu.PreviewMouseWheel += (_, e) => OnPreviewMouseWheel(e, false);
        ContextMenu.PreviewKeyDown += OnKeyDown;
        PreviewKeyDown += OnKeyDown;
        Closed += (_, __) => BeforeExit();
        g_worker.DoWork += DoWork;
        g_worker.RunWorkerAsync();
        MainGrid.PreviewMouseWheel += (_, e) => e.Handled = true;
        MainGrid.MouseDown += OnMouseDown_OpenStockDetail;
        // 托盘图标
        TrayIcon.IconSource = TrayIconRes;
        TrayIcon.ToolTipText = $"{App.ProductName} v{App.ProductVersion}";
        TrayIcon.TrayMouseDoubleClick += (_, __) => ShowOrHiden();

        // 依赖属性事件绑定
        DependencyPropertyDescriptor
            .FromProperty(TopProperty, typeof(Window))
            .AddValueChanged(this, ConfigWindowAdsorption);
        DependencyPropertyDescriptor
            .FromProperty(LeftProperty, typeof(Window))
            .AddValueChanged(this, ConfigWindowAdsorption);
        DependencyPropertyDescriptor
            .FromProperty(WidthProperty, typeof(Window))
            .AddValueChanged(this, ConfigWindowAdsorption);

        InitColor();
        InitLang();

        if (!g_conf.FontFamilyMain.IsNullOrWhiteSpace())
        {
            Application.Current.Resources["FontMain"] = new FontFamily(g_conf.FontFamilyMain);
        }
        if (!g_conf.FontFamilyConfig.IsNullOrWhiteSpace())
        {
            Application.Current.Resources["FontConfig"] = new FontFamily(g_conf.FontFamilyConfig);
        }
        if (!g_conf.FontFamilyMenu.IsNullOrWhiteSpace())
        {
            Application.Current.Resources["FontMenu"] = new FontFamily(g_conf.FontFamilyMenu);
        }
        FontSize = g_conf.FontSizeMain;
    }

    /// <summary>
    /// 初始化配色
    /// </summary>
    public void InitColor()
    {
        if (g_conf.DarkMode)
        {
            color_bg = new SolidColorBrush(Color.FromRgb(22, 22, 22));
            color_fg = new SolidColorBrush(Color.FromRgb(222, 222, 222));
        }
        else
        {
            color_bg = new SolidColorBrush(Color.FromRgb(222, 222, 222));
            color_fg = SystemColors.ControlTextBrush;
        }

        border.Background = g_conf.Transparent ? color_transparent : color_bg;
        MainTextBlock.Foreground = color_fg;
        Resources["TextColor"] = color_fg;
        Resources["SubMenuBackground"] = color_bg;

        // 更新 Grid 中所有 TextBlock 的前景色
        foreach (var child in MainGrid.Children)
        {
            if (child is TextBlock textBlock)
            {
                textBlock.Foreground = color_fg;
            }
        }

        var tempSubMenuMask = new SolidColorBrush
        {
            Color = color_bg.Color,
            Opacity = (double)Resources["MainOpacity"]
        };
        Resources["SubMenuMask"] = tempSubMenuMask;
        var tempHoverBg = new SolidColorBrush
        {
            Color = g_conf.DarkMode ? Color.FromRgb(51, 51, 51) : Color.FromRgb(187, 187, 187),
        };
        Resources["HoverBackground"] = tempHoverBg;
    }

    /// <summary>
    /// 初始化语言配置
    /// </summary>
    private void InitLang()
    {
        DragBlock.ToolTip = i18n[g_conf.Lang]["ui_drag_block"];
        menu_ver.Header = $"{i18n[g_conf.Lang][menu_ver.Name]} {App.ProductVersion}(_V)";
        SetMenuItemHeader(menu_exit, "X");
        SetMenuItemHeader(menu_hide_border, "H");
        SetMenuItemHeader(menu_dark, "D");
        SetMenuItemHeader(menu_topmost, "T");
        SetMenuItemHeader(menu_conf, "C");
        SetMenuItemHeader(menu_conf_file, "F");
        SetMenuItemHeader(menu_data_dir, "D");
        SetMenuItemHeader(menu_show_in_taskbar, "B");
        SetMenuItemHeader(menu_data_roll, "R");
        SetMenuItemHeader(menu_reset_reminder, "R");
        SetMenuItemHeader(menu_lang, "L");
        SetMenuItemHeader(menu_ui, "U");
        SetMenuItemHeader(menu_check_update, "U");
        SetMenuItemHeader(menu_debug_mode, "D");
        SetMenuItemHeader(menu_transparent, "A");
        menu_opacity.Header = string.Format(i18n[g_conf.Lang]["menu_opacity"], (Opacity * 100).ToString("f0"));
        menu_opacity.InputGestureText = i18n[g_conf.Lang]["menu_opacity_igt"];
        SetMenuItemHeader(tary_project_page, "P", "menu_project_page");
        SetMenuItemHeader(tray_ver, "C", "menu_check_update");
        SetMenuItemHeader(tray_exit, "X", "menu_exit");
    }
    #endregion

    #region 界面事件
    /// <summary>
    /// 退出前保存数据
    /// </summary>
    private void BeforeExit()
    {
        g_worker.CancelAsync();
        g_conf.Left = Left;
        g_conf.Top = Top;
        g_conf.Width = Width;
        g_conf.Height = Height;
        g_conf.Opacity = Opacity;
        g_conf.Save();
        g_conf_stocks.Save();

        var hwnd = new WindowInteropHelper(this).Handle;
        // 注销热键
        UnregisterHotKey(hwnd, BOSS_HOTKEY_ID);
    }

    /// <summary>
    /// 鼠标滚轮事件
    /// </summary>
    /// <param name="e"></param>
    /// <param name="needPressCtrl"></param>
    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        OnPreviewMouseWheel(e, true);
    }

    private void OnPreviewMouseWheel(MouseWheelEventArgs e, bool needPressCtrl = true)
    {
        if (needPressCtrl)
        {
            if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                return;
            }
        }

        if (e.Delta > 0)
        {
            Opacity = Math.Min(Opacity + 0.05, 1.0);
        }
        else
        {
            Opacity = Math.Max(Opacity - 0.05, 0.05);
        }
        if (g_configWindow != null)
        {
            g_configWindow.Opacity = Opacity;
            g_configWindow.Resources["MainOpacity"] = Opacity;
        }
        menu_opacity.Header = string.Format(i18n[g_conf.Lang]["menu_opacity"], (Opacity * 100).ToString("f0"));
        var tempOpacity = Math.Min(Opacity + MenuOpacityAdded, 1);
        Resources["MainOpacity"] = tempOpacity;
        var tempSubMenuMask = new SolidColorBrush
        {
            Color = color_bg.Color,
            Opacity = tempOpacity
        };
        Resources["SubMenuMask"] = tempSubMenuMask;
    }


    /// <summary>
    /// 双击股票行跳转详情页（雪球/同花顺）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnMouseDown_OpenStockDetail(object sender, MouseButtonEventArgs e)
    {
        if (g_conf.Transparent)
        {
            return;
        }
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }
        if (e.ClickCount != 2)
        {
            return;
        }
        if (sender is not Grid grid)
        {
            return;
        }
        if (_dataTextBlocks.Count == 0)
        {
            return;
        }
        var pos = e.GetPosition(grid);
        for (var i = 0; i < _dataTextBlocks.Count; i++)
        {
            var firstCell = _dataTextBlocks[i][0];
            var cellTop = firstCell.TranslatePoint(new Point(0, 0), grid);
            var cellBottom = cellTop.Y + firstCell.ActualHeight;
            if (pos.Y >= cellTop.Y && pos.Y <= cellBottom)
            {
                if (i >= g_conf_stocks.Count)
                {
                    return;
                }
                var stock = g_conf_stocks.ElementAt(i);
                if (stock.Code.IsNullOrWhiteSpace())
                {
                    return;
                }
                var url = g_conf.DoubleClickAction == "tonghuashun"
                    ? StockDetailPage.TongHuaShun(stock.Code)
                    : StockDetailPage.XueQiu(stock.Code);
                Process.Start(url);
                return;
            }
        }
    }

    /// <summary>
    /// 快捷键事件
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="e"></param>
    public void OnKeyDown(object obj, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            // border.Background = color_bg;
            var menuItemName = e.Key switch
            {
                Key.A => menu_transparent.Name,
                Key.B => menu_show_in_taskbar.Name,
                Key.C => menu_conf.Name,
                Key.D => menu_dark.Name,
                Key.F => menu_conf_file.Name,
                Key.H => menu_hide_border.Name,
                Key.U => menu_check_update.Name,
                Key.L => menu_lang.Name,
                Key.R => menu_data_roll.Name,
                Key.T => menu_topmost.Name,
                Key.X => menu_exit.Name,
                _ => ""
            };
            if (menuItemName.IsNullOrWhiteSpace())
            {
                return;
            }
            OnMenuItemClick(ContextMenu.FindMenuItem(menuItemName));
        }
    }

    /// <summary>
    /// 配置界面吸附主界面
    /// </summary>
    /// <param name="o"></param>
    /// <param name="eventArgs"></param>
    private void ConfigWindowAdsorption(object o, EventArgs eventArgs)
    {
        if (g_configWindow != null)
        {
            g_configWindow.Left = Left + Width;
            g_configWindow.Top = Top;
        }
    }

    /// <summary>
    /// 设置菜单文本
    /// </summary>
    /// <param name="menuItem"></param>
    private void SetMenuItemHeader(MenuItem menuItem, string shortcuts = null, string tag = null)
    {
        if (tag.IsNullOrWhiteSpace())
        {
            tag = menuItem.Name;
        }
        if (i18n.ContainsKey(g_conf.Lang) && i18n[g_conf.Lang].ContainsKey(tag))
        {
            if (shortcuts.IsNullOrWhiteSpace())
            {
                menuItem.Header = i18n[g_conf.Lang][tag];
            }
            else
            {
                menuItem.Header = i18n[g_conf.Lang][tag] + $"(_{shortcuts})";
            }
        }
    }
    #endregion

    #region 整理数据
    private string GetFieldName(string newline)
    {
        var fieldName = $"{newline}{i18n[g_conf.Lang]["ui_name"]}";
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value || kvp.Key == "ui_fieldname")
            {
                continue;
            }
            var fieldData = kvp.Key switch
            {
                "ui_yesterday_todayopen" => $" {i18n[g_conf.Lang]["ui_yesterday"]}{Symbols.ArrowRight}{i18n[g_conf.Lang]["ui_todayopen"]}",
                "ui_lowest_highest" => $" {i18n[g_conf.Lang]["ui_lowest"]}{Symbols.ArrowUpDown}{i18n[g_conf.Lang]["ui_highest"]}",
                "ui_limitup_limitdown" => $" {i18n[g_conf.Lang]["ui_limitdown"]}{Symbols.Wave}{i18n[g_conf.Lang]["ui_limitup"]}",
                _ => $" {i18n[g_conf.Lang][kvp.Key]}"
            };
            fieldName += fieldData;
        }
        return fieldName;
    }

    /// <summary>
    /// 获取字段表头名称列表（用于 Grid 列头）
    /// </summary>
    private List<string> GetFieldNames()
    {
        var names = new List<string> { i18n[g_conf.Lang]["ui_name"] };
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value || kvp.Key == "ui_fieldname")
            {
                continue;
            }
            var name = kvp.Key switch
            {
                "ui_yesterday_todayopen" => $"{i18n[g_conf.Lang]["ui_yesterday"]}{Symbols.ArrowRight}{i18n[g_conf.Lang]["ui_todayopen"]}",
                "ui_lowest_highest" => $"{i18n[g_conf.Lang]["ui_lowest"]}{Symbols.ArrowUpDown}{i18n[g_conf.Lang]["ui_highest"]}",
                "ui_limitup_limitdown" => $"{i18n[g_conf.Lang]["ui_limitdown"]}{Symbols.Wave}{i18n[g_conf.Lang]["ui_limitup"]}",
                _ => i18n[g_conf.Lang][kvp.Key]
            };
            names.Add(name);
        }
        return names;
    }

    /// <summary>
    /// 获取股票字段值列表（用于 Grid 显示）
    /// </summary>
    /// <param name="sc">股票配置</param>
    /// <param name="res">股票数据</param>
    /// <returns>字段值列表（包含名称和各字段值）</returns>
    private List<FieldValue> GetFieldValues(ref StockConfig sc, StockInfo res)
    {
        sc.Name = res.StockName;
        var fields = new List<FieldValue>
        {
            new("ui_name", sc.DisplayName, FieldAlignment.Left)
        };

        var makeMoney = res.CurrentPrice - sc.BuyPrice;
        sc.DayMake = res.PriceChange * sc.BuyCount;
        sc.AllMake = makeMoney * sc.BuyCount;
        sc.Cost = sc.BuyPrice * sc.BuyCount;
        sc.MarketValue = sc.Cost + sc.AllMake;
        var hold = sc.BuyCount > 0;
        if (hold)
        {
            sc.Yield = sc.AllMake / sc.Cost * 100;
        }

        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value || kvp.Key == "ui_fieldname")
            {
                continue;
            }
            var dot = res.StockName.EndsWith("ETF") ? "f3" : "f2";
            var alignment = kvp.Key switch
            {
                "ui_yesterday_todayopen" or "ui_lowest_highest" or "ui_limitup_limitdown" => FieldAlignment.Center,
                _ => FieldAlignment.Right
            };
            var fieldData = kvp.Key switch
            {
                "ui_price" => res.CurrentPrice.ToString(dot),
                "ui_change" => $"{res.PriceChangePercent:f2}%",
                "ui_buy_price" => hold ? $"{sc.BuyPrice:f2}" : "",
                "ui_num" => hold ? $"{sc.BuyCount}" : "",
                "ui_cost" => hold ? $"{sc.Cost:f0}" : "",
                "ui_market_value" => hold ? $"{sc.MarketValue:f0}" : "",
                "ui_yield" => hold ? $"{sc.Yield:f2}%" : "",
                "ui_day_make" => hold ? $"{sc.DayMake:f0}" : "",
                "ui_all_make" => hold ? $"{sc.AllMake:f0}" : "",
                "ui_yesterday_todayopen" => $"{res.YesterdayClose.ToString(dot)}{Symbols.ArrowRight}{res.TodayOpen.ToString(dot)}",
                "ui_lowest_highest" => $"{res.LowestPrice.ToString(dot)}{Symbols.ArrowUpDown}{res.HighestPrice.ToString(dot)}",
                "ui_limitup_limitdown" => res.PriceLimitDown != res.PriceLimitUp ? $"{res.PriceLimitDown.ToString(dot)}{Symbols.Wave}{res.PriceLimitUp.ToString(dot)}" : "",
                _ => ""
            };
            fields.Add(new FieldValue(kvp.Key, fieldData, alignment));
        }
        return fields;
    }

    /// <summary>
    /// 数据对齐需要填充的长度
    /// </summary>
    private readonly Dictionary<string, int> PadLengths = [];
    /// <summary>
    /// 字段数据
    /// </summary>
    private readonly Dictionary<string, string> FieldDatas = [];
    /// <summary>
    /// 整理数据
    /// </summary>
    /// <param name="sc">配置</param>
    /// <param name="res">最新数据</param>
    /// <returns></returns>
    private string StockInfoHandle(ref StockConfig sc, StockInfo res)
    {
        sc.Name = res.StockName;
        var info = sc.DisplayName;
        var makeMoney = res.CurrentPrice - sc.BuyPrice;
        sc.DayMake = res.PriceChange * sc.BuyCount;
        sc.AllMake = makeMoney * sc.BuyCount;
        sc.Cost = sc.BuyPrice * sc.BuyCount;
        sc.MarketValue = sc.Cost + sc.AllMake;
        var hold = sc.BuyCount > 0;// 是否当前持有
        if (hold)
        {
            sc.Yield = sc.AllMake / sc.Cost * 100;
        }
        // 整理字段数据 计算对齐所需填充长度
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value)// 启用字段
            {
                continue;
            }
            var dot = res.StockName.EndsWith("ETF") ? "f3" : "f2";
            var fieldData = kvp.Key switch
            {
                //"ui_fieldname" => "",
                "ui_price" => res.CurrentPrice.ToString(dot),
                "ui_change" => $"{res.PriceChangePercent:f2}%",
                "ui_buy_price" => hold ? $"{sc.BuyPrice:f2}" : "",
                "ui_num" => hold ? $"{sc.BuyCount}" : "",
                "ui_cost" => hold ? $"{sc.Cost:f0}" : "",
                "ui_market_value" => hold ? $"{sc.MarketValue:f0}" : "",
                "ui_yield" => hold ? $"{sc.Yield:f2}%" : "",
                "ui_day_make" => hold ? $"{sc.DayMake:f0}" : "",
                "ui_all_make" => hold ? $"{sc.AllMake:f0}" : "",
                "ui_yesterday_todayopen" => $"{res.YesterdayClose.ToString(dot)}{Symbols.ArrowRight}{res.TodayOpen.ToString(dot)}",
                "ui_lowest_highest" => $"{res.LowestPrice.ToString(dot)}{Symbols.ArrowUpDown}{res.HighestPrice.ToString(dot)}",
                "ui_limitup_limitdown" => res.PriceLimitDown != res.PriceLimitUp ? $"{res.PriceLimitDown.ToString(dot)}{Symbols.Wave}{res.PriceLimitUp.ToString(dot)}" : "",
                _ => ""
            };

            FieldDatas[kvp.Key] = fieldData;
            if (PadLengths.ContainsKey(kvp.Key))
            {
                PadLengths[kvp.Key] = Math.Max(PadLengths[kvp.Key], fieldData.Length);
            }
            else
            {
                PadLengths[kvp.Key] = fieldData.Length;
            }
        }
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value)// 启用字段
            {
                continue;
            }
            var val = FieldDatas[kvp.Key];// 字段数据
            var maxLen = PadLengths[kvp.Key];// 字段最大长度
            var leftLen = (maxLen - val.Length) / 2 + val.Length;// 字段左侧需要填充的长度
            var fieldData = kvp.Key switch
            {
                "ui_yesterday_todayopen" or "ui_lowest_highest" or "ui_limitup_limitdown"
                  => $" {val.PadLeft(leftLen).PadRight(maxLen)}",
                _ => $" {val.PadLeft(maxLen)}",
            };
            info += fieldData;
        }
        return info;
    }

    public void DoWork(object sender, EventArgs e)
    {
        while (true)
        {
            try
            {
                DataUpdate(false).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            Delay();// 请求间隔最低 2s
        }
    }
    #endregion

    #region 更新界面
    /// <summary>
    /// 更新界面显示内容
    /// </summary>
    /// <param name="useCache">是否使用上一次请求响应数据</param>
    public async Task DataUpdate(bool useCache = true)
    {
        var isTradingTime = IsTradingTime();
        if (!isTradingTime && !g_conf.Debug)// 非交易时间
        {
            SwitchToTextMode();
            UpdateUI(i18n[g_conf.Lang]["ui_nontrading"]);
            if (!useCache)// 不使用缓存时 计时器定时请求 在非交易时间时需要延迟30秒
            {
                Delay(30000);
            }
            return;
        }

        if (g_conf.DataRoll)// 单行数据滚动展示
        {
            int currentIndex;
            lock (g_dataLock)
            {
                if (g_codeIndex > g_conf_stocks.Count - 1)
                {
                    g_codeIndex = 0;
                    Delay();
                    return;
                }
                currentIndex = g_codeIndex;
            }

            var stock = g_conf_stocks.ElementAt(currentIndex);
            var res = await Request(stock, useCache);
            if (res == null)
            {
                SwitchToTextMode();
                UpdateUI(i18n[g_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                Delay(10000);
                return;
            }
            var text = StockInfoHandle(ref stock, res);
            var status = UIStatus.Normal;
            if (res.PriceChangePercent >= AlertValue)
            {
                status = UIStatus.UpLimit;
            }
            else if (res.PriceChangePercent <= -AlertValue)
            {
                status = UIStatus.DownLimit;
            }
            SwitchToTextMode();
            // 使用缓存的扩展字段内容
            UpdateUI(text, status, _cachedExtendContent);

            lock (g_dataLock)
            {
                g_codeIndex++;
                if (g_conf_stocks.Count < g_codeIndex + 1)
                {
                    g_codeIndex = 0;
                }
            }
        }
        else// 多行数据同步展示
        {
            var res = await Request(g_conf_stocks_with_index, useCache);
            if (res == null || res.Count == 0)
            {
                SwitchToTextMode();
                UpdateUI(i18n[g_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                Delay(10000);
                return;
            }
            var daymake = 0.0;// 总持日盈
            var allmake = 0.0;// 总持总盈
            var allcost = 0.0;// 总成本
            var allmarketvalue = 0.0;// 总市值
            var hasUpLimit = false;
            var hasDownLimit = false;
            var allStocksData = new List<List<FieldValue>>();// 所有股票的字段数据
            var allStocksCodes = new List<string>();// 并行记录股票代码，用于排序
            var reminder = "";// 提醒信息
            foreach (var info in res)
            {
                var stock = g_conf_stocks_with_index.FirstOrDefault(x =>
                {
                    if (x.Code.IsNullOrWhiteSpace() || x.Code.Length < 5) { return false; }
                    return x.Code.Trim().Remove(0, 2) == info.StockCode.TrimStart('.');
                });
                if (stock == null || stock.Code.IsNullOrWhiteSpace())
                {
                    continue;
                }
                if (StockConfigArray.ImportantIndexs.Any(x => x.Code == stock.Code))
                {
                    StockConfigArray.ImportantIndexs[stock.Code].IndexInfo = $"{i18n[g_conf.Lang][StockIndexPrefix + stock.Code]} {info.CurrentPrice:f2} {info.PriceChangePercent:f2}%";
                    continue;
                }
                var fieldValues = GetFieldValues(ref stock, info);
                allStocksData.Add(fieldValues);
                allStocksCodes.Add(stock.Code);
                daymake += stock.DayMake;
                allmake += stock.AllMake;
                allcost += stock.Cost;
                allmarketvalue += stock.Cost + stock.AllMake;
                if (info.PriceChangePercent < -AlertValue)
                {
                    hasDownLimit = true;
                }
                else if (info.PriceChangePercent > AlertValue)
                {
                    hasUpLimit = true;
                }

                if (stock.ReminderTimes > 0)
                {
                    if (stock.ReminderPriceUp > 0 && info.CurrentPrice >= stock.ReminderPriceUp)
                    {
                        reminder += $"{stock.DisplayName} {Symbols.RightUp} {stock.ReminderPriceUp:f2}{Environment.NewLine}";
                        stock.ReminderTimes--;
                    }
                    else if (stock.ReminderPriceDown > 0 && info.CurrentPrice <= stock.ReminderPriceDown)
                    {
                        reminder += $"{stock.DisplayName} {Symbols.RightDown} {stock.ReminderPriceDown:f2}{Environment.NewLine}";
                        stock.ReminderTimes--;
                    }
                }
            }

            // 排序
            if (g_conf.SortField != "default" && allStocksData.Count > 1)
            {
                var sortKeys = new double[allStocksData.Count];
                for (int i = 0; i < allStocksCodes.Count; i++)
                {
                    var sc = g_conf_stocks_with_index.FirstOrDefault(x => x.Code == allStocksCodes[i]);
                    if (sc == null) { sortKeys[i] = double.MinValue; continue; }
                    sortKeys[i] = g_conf.SortField switch
                    {
                        "changePercent" => res.FirstOrDefault(x => x.StockCode.TrimStart('.') == sc.Code.Trim().Remove(0, 2))?.PriceChangePercent ?? 0,
                        "buyPrice" => res.FirstOrDefault(x => x.StockCode.TrimStart('.') == sc.Code.Trim().Remove(0, 2))?.CurrentPrice ?? 0,
                        "cost" => sc.Cost,
                        "marketValue" => sc.MarketValue,
                        "dayMake" => sc.DayMake,
                        "allMake" => sc.AllMake,
                        "yield" => sc.Yield,
                        _ => 0
                    };
                }
                var indices = Enumerable.Range(0, allStocksData.Count).ToArray();
                Array.Sort(sortKeys, indices, g_conf.SortOrder == "asc" ? Comparer<double>.Default : Comparer<double>.Create((a, b) => b.CompareTo(a)));
                var sortedData = new List<List<FieldValue>>(allStocksData.Count);
                foreach (var idx in indices) sortedData.Add(allStocksData[idx]);
                allStocksData = sortedData;
            }

            var allyield = 0.0;
            var allyield_day = 0.0;
            if (allcost != 0.0)
            {
                allyield = allmake / allcost * 100;// 总收益率
                allyield_day = daymake / allcost * 100;// 总持日收益率
            }

            // 整理扩展字段内容
            var extendContent = "";
            var lastMarket = "";
            var hasIndexContent = false;
            var addedGap = false;
            foreach (var item in g_conf.ExtendControls)
            {
                if (!item.Key.StartsWith(StockIndexPrefix))
                {
                    if (!addedGap && hasIndexContent)
                    {
                        extendContent += Environment.NewLine;
                        addedGap = true;
                    }
                    if (!item.Visable)
                    {
                        if (item.NewLine) extendContent += Environment.NewLine;
                        continue;
                    }
                    if (!i18n[g_conf.Lang].TryGetValue(item.Key, out var field)) continue;
                    var nl = item.NewLine ? Environment.NewLine : "";
                    var temp = item.Key switch
                    {
                        "ui_all_stock_day_make" => $"{nl}{field} {daymake:f2} ",
                        "ui_all_stock_all_make" => $"{nl}{field} {allmake:f2} ",
                        "ui_all_cost" => $"{nl}{field} {allcost:f2} ",
                        "ui_all_market_value" => $"{nl}{field} {allmarketvalue:f2} ",
                        "ui_all_yield_day" => $"{nl}{field} {allyield_day:f2}% ",
                        "ui_all_yield" => $"{nl}{field} {allyield:f2}% ",
                        _ => ""
                    };
                    extendContent += temp;
                    continue;
                }
                // 指数：按市场分组，每个市场一行
                if (!item.Visable)
                {
                    if (item.NewLine) extendContent += Environment.NewLine;
                    continue;
                }
                var market = item.Key.Substring(StockIndexPrefix.Length);
                market = market.StartsWith("hk") ? "hk" : market.StartsWith("us") ? "us" : "a";
                // 检查市场是否启用
                if (market == "hk" && !g_conf.EnableHK) continue;
                if (market == "us" && !g_conf.EnableUS) continue;
                if (market != lastMarket)
                {
                    if (lastMarket != "") extendContent += Environment.NewLine;
                    lastMarket = market;
                }
                var idxNl = item.NewLine ? Environment.NewLine : "";
                var idxInfo = StockConfigArray.ImportantIndexs[item.Key]?.IndexInfo;
                if (idxInfo != null)
                {
                    extendContent += idxNl + idxInfo + " ";
                    hasIndexContent = true;
                }
            }

            var status = UIStatus.Normal;
            if (!g_conf.Transparent)
            {
                if (hasUpLimit)
                {
                    status = UIStatus.UpLimit;
                }
                else if (hasDownLimit)
                {
                    status = UIStatus.DownLimit;
                }
            }
            UpdateGrid(allStocksData, extendContent.TrimEnd(), status);
            if (reminder.IsNullOrWhiteSpace() == false)
            {
                TrayIcon.ShowBalloonTip("", reminder.TrimEnd(), BalloonIcon.None);
                g_conf.LastReminderTime = DateTime.Now;
                g_configWindow?.UpdateDataGrid();
            }
        }
    }

    private void Delay(int ms)
    {
        Thread.Sleep(ms);
    }

    private void Delay()
    {
        Delay(g_conf.Interval < 2 ? 2000 : g_conf.Interval * 1000);
    }

    private void UpdateUI(string msg, UIStatus status = UIStatus.Normal, string extendContent = "")
    {
        if (this == null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            MainTextBlock.Text = msg;
            
            // 更新扩展字段
            if (!string.IsNullOrEmpty(extendContent))
            {
                ExtendTextBlock.Text = extendContent;
                ExtendTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                ExtendTextBlock.Text = "";
                ExtendTextBlock.Visibility = Visibility.Collapsed;
            }
            
            if (status != UIStatus.Normal)
            {
                UpdateColor(status);
            }
        });
    }

    private void UpdateColor(UIStatus status = UIStatus.Normal)
    {
        var foreground = status switch
        {
            UIStatus.UpLimit => new SolidColorBrush(Colors.Yellow),
            UIStatus.DownLimit => new SolidColorBrush(Colors.YellowGreen),
            UIStatus.ProgramError => new SolidColorBrush(Colors.Yellow),
            _ => color_fg
        };
        var background = status switch
        {
            UIStatus.UpLimit => new SolidColorBrush(Colors.Red),
            UIStatus.DownLimit => new SolidColorBrush(Colors.Green),
            UIStatus.ProgramError => new SolidColorBrush(Colors.Purple),
            _ => color_bg
        };

        MainTextBlock.Foreground = foreground;
        border.Background = background;

        // 更新 Grid 中所有 TextBlock 的前景色
        if (MainTextBlock.Visibility == Visibility.Collapsed)
        {
            foreach (var child in MainGrid.Children)
            {
                if (child is TextBlock textBlock)
                {
                    textBlock.Foreground = foreground;
                }
            }
        }
    }

    /// <summary>
    /// 标记 Grid 结构需要重建（在配置改变时调用）
    /// </summary>
    public void MarkGridStructureDirty()
    {
        _gridStructureDirty = true;
    }

    /// <summary>
    /// 检查是否需要重建 Grid 结构
    /// </summary>
    private bool NeedRebuildStructure(List<List<FieldValue>> allStocksData)
    {
        var showFieldName = g_conf.FieldControls.TryGetValue("ui_fieldname", out var showHeader) && showHeader;
        var fieldNames = GetFieldNames();
        var columnCount = fieldNames.Count;
        var dataRowCount = allStocksData.Count;
        var totalRows = (showFieldName ? 1 : 0) + dataRowCount;

        return _gridStructureDirty ||
               columnCount != _cachedColumnCount ||
               totalRows != _cachedRowCount ||
               showFieldName != _cachedShowFieldName;
    }

    /// <summary>
    /// 重建 Grid 结构（清空并重新创建所有元素）
    /// </summary>
    private void RebuildGridStructure(List<List<FieldValue>> allStocksData)
    {
        MainGrid.Children.Clear();
        MainGrid.RowDefinitions.Clear();
        MainGrid.ColumnDefinitions.Clear();
        _headerTextBlocks.Clear();
        _dataTextBlocks.Clear();

        if (allStocksData.Count == 0)
        {
            _cachedColumnCount = 0;
            _cachedRowCount = 0;
            return;
        }

        // 获取表头
        var fieldNames = GetFieldNames();
        var columnCount = fieldNames.Count;
        _cachedColumnCount = columnCount;

        // 创建列定义
        for (var i = 0; i < columnCount; i++)
        {
            MainGrid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = GridLength.Auto
            });
        }

        // 检查是否显示表头行
        var showFieldName = g_conf.FieldControls.TryGetValue("ui_fieldname", out var showHeader) && showHeader;
        _cachedShowFieldName = showFieldName;
        var startRow = 0;

        // 创建表头行（如果启用）
        if (showFieldName)
        {
            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            for (var i = 0; i < fieldNames.Count; i++)
            {
                var headerBlock = new TextBlock
                {
                    Text = fieldNames[i],
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(g_conf.GridColumnSpacing, 0, g_conf.GridColumnSpacing, 0),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = color_fg
                };
                Grid.SetRow(headerBlock, 0);
                Grid.SetColumn(headerBlock, i);
                MainGrid.Children.Add(headerBlock);
                _headerTextBlocks.Add(headerBlock);
            }
            startRow = 1;
        }

        // 创建数据行
        for (var rowIndex = 0; rowIndex < allStocksData.Count; rowIndex++)
        {
            MainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var rowData = allStocksData[rowIndex];
            var rowBlocks = new List<TextBlock>();
            for (var colIndex = 0; colIndex < rowData.Count && colIndex < columnCount; colIndex++)
            {
                var field = rowData[colIndex];
                var textBlock = new TextBlock
                {
                    Text = field.Value,
                    Margin = new Thickness(g_conf.GridColumnSpacing, 0, g_conf.GridColumnSpacing, 0),
                    Foreground = color_fg
                };
                textBlock.HorizontalAlignment = field.Alignment switch
                {
                    FieldAlignment.Left => HorizontalAlignment.Left,
                    FieldAlignment.Center => HorizontalAlignment.Center,
                    _ => HorizontalAlignment.Right
                };
                Grid.SetRow(textBlock, rowIndex + startRow);
                Grid.SetColumn(textBlock, colIndex);
                MainGrid.Children.Add(textBlock);
                rowBlocks.Add(textBlock);
            }
            _dataTextBlocks.Add(rowBlocks);
        }

        _cachedRowCount = MainGrid.RowDefinitions.Count;
        _gridStructureDirty = false;
    }

    /// <summary>
    /// 只更新 Grid 数据（不重建结构）
    /// </summary>
    private void UpdateGridData(List<List<FieldValue>> allStocksData, string extendContent)
    {
        // 更新数据行
        for (var rowIndex = 0; rowIndex < allStocksData.Count && rowIndex < _dataTextBlocks.Count; rowIndex++)
        {
            var rowData = allStocksData[rowIndex];
            var rowBlocks = _dataTextBlocks[rowIndex];
            for (var colIndex = 0; colIndex < rowData.Count && colIndex < rowBlocks.Count; colIndex++)
            {
                var field = rowData[colIndex];
                var textBlock = rowBlocks[colIndex];
                textBlock.Text = field.Value;
                textBlock.HorizontalAlignment = field.Alignment switch
                {
                    FieldAlignment.Left => HorizontalAlignment.Left,
                    FieldAlignment.Center => HorizontalAlignment.Center,
                    _ => HorizontalAlignment.Right
                };
            }
        }

        // 更新扩展字段
        ExtendTextBlock.Text = extendContent;
        ExtendTextBlock.Visibility = string.IsNullOrEmpty(extendContent) ? Visibility.Collapsed : Visibility.Visible;
    }

    /// <summary>
    /// 更新 Grid 表格显示
    /// </summary>
    /// <param name="allStocksData">所有股票的字段数据</param>
    /// <param name="extendContent">扩展字段内容（指数、汇总等）</param>
    /// <param name="status">UI 状态</param>
    private void UpdateGrid(List<List<FieldValue>> allStocksData, string extendContent, UIStatus status = UIStatus.Normal)
    {
        if (this == null)
        {
            return;
        }

        // 缓存扩展字段内容
        _cachedExtendContent = extendContent;

        Application.Current.Dispatcher.Invoke(() =>
        {
            // 切换到 Grid 模式
            MainTextBlock.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;

            if (allStocksData.Count == 0)
            {
                MainGrid.Children.Clear();
                MainGrid.RowDefinitions.Clear();
                MainGrid.ColumnDefinitions.Clear();
                _headerTextBlocks.Clear();
                _dataTextBlocks.Clear();
                _cachedColumnCount = 0;
                _cachedRowCount = 0;
                ExtendTextBlock.Text = "";
                ExtendTextBlock.Visibility = Visibility.Collapsed;
                return;
            }

            // 检查是否需要重建结构
            if (NeedRebuildStructure(allStocksData))
            {
                RebuildGridStructure(allStocksData);
            }
            
            // 更新数据和扩展字段
            UpdateGridData(allStocksData, extendContent);

            if (status != UIStatus.Normal)
            {
                UpdateColor(status);
            }
        });
    }

    /// <summary>
    /// 切换到单行文本模式
    /// </summary>
    private void SwitchToTextMode()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MainTextBlock.Visibility = Visibility.Visible;
            MainGrid.Visibility = Visibility.Collapsed;
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();
            _gridStructureDirty = true; // 标记 Grid 结构需要重建，因为 MainGrid.Children 已被清空
            // 保留 ExtendTextBlock 可见性，单行模式下也可以显示扩展数据
        });
    }
    #endregion

    #region 请求数据
    internal async Task<StockInfo> VerifyStockCode(string code)
    {
        try
        {
            var request = new RestRequest();
            request.AddQueryParameter("q", code);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content;
                var result = StockInfo.Get(content);
                return result;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        return null;
    }

    public async Task<bool> VerifyApi(string api)
    {
        try
        {
            var client = new RestClient(api);
            client.AddDefaultHeader("User-Agent", g_conf.UserAgent);
            var request = new RestRequest();
            request.AddQueryParameter("q", "sh000001");
            var response = await client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content;
                var result = StockInfo.Get(content);
                return result != null;
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }
        return false;
    }

    public void UpdateRestClient(string api)
    {
        g_client = new RestClient(api);
        g_client.AddDefaultHeader("User-Agent", g_conf.UserAgent);
    }

    public void UpdateFontSize(double fontSize)
    {
        this.FontSize = fontSize;
    }

    private async Task<StockInfo> Request(StockConfig sc, bool useCache)
    {
        try
        {
            if (useCache)
            {
                lock (g_dataLock)
                {
                    if (g_last_res != null) return g_last_res;
                }
                return await Request(sc, false);
            }
            var request = new RestRequest();
            request.AddQueryParameter("q", sc.Code);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content;
                var result = StockInfo.Get(content);
                lock (g_dataLock)
                {
                    g_last_res = result;
                }
                return result;
            }
            else
            {
                Logger.Error($"HTTP Request Failed with Status Code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        return null;
    }

    private async Task<List<StockInfo>> Request(List<StockConfig> scs, bool useCache)
    {
        try
        {
            if (useCache)
            {
                lock (g_dataLock)
                {
                    if (g_last_res_set != null && g_last_res_set.Count > 0)
                        return g_last_res_set;
                }
                return await Request(scs, false);
            }
            var codes = string.Join(",", scs.Where(x => x.Code.IsNullOrWhiteSpace() == false).Select(x => x.Code));
            var request = new RestRequest();
            request.AddQueryParameter("q", codes);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var contents = response.Content.Split(';');
                if (contents.Length == 0)
                {
                    lock (g_dataLock) { return g_last_res_set; }
                }
                var newSet = new List<StockInfo>();
                foreach (var content in contents)
                {
                    if (content.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    var stock = StockInfo.Get(content.Trim());
                    if (stock != null)
                    {
                        newSet.Add(stock);
                    }
                }
                lock (g_dataLock)
                {
                    g_last_res_set = newSet;
                    return g_last_res_set;
                }
            }
            else
            {
                Logger.Error($"HTTP Request Failed with Status Code: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex);
        }

        return null;
    }
    #endregion

    #region 打开子窗口
    private void MakeConfigWindow()
    {
        g_configWindow ??= new ConfigWindow(this)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Left = Left + Width,
            Top = Top,
            Opacity = Opacity,
            ShowInTaskbar = ShowInTaskbar,
        };
        g_configWindow.PreviewMouseWheel += OnPreviewMouseWheel;
        g_configWindow.PreviewKeyDown += OnKeyDown;
        if (g_configWindow.Visibility == Visibility.Visible)
        {
            g_configWindow.Hide();
            ConfigWindowShow = false;
        }
        else
        {
            g_configWindow.Show();
            ConfigWindowShow = true;
        }
        Focus();
    }

    private void MakeUpdater()
    {
        g_updater?.Close();

        var param = new Pdone.Updater.UI.Parameter()
        {
            AppName = App.ProductName,
            DarkMode = g_conf.DarkMode,
            Language = g_conf.Lang == "cn" ? "zh-CN" : "en-US",
            CurrentVersion = $"v{App.ProductVersion}",
            TaskbarIcon = TaskbarIcon,
            PageImage = PageImage,
            RepoName = App.ProductName,
        };
        g_updater = new Pdone.Updater.UI.Main(param)
        {
            Owner = this,
            ShowInTaskbar = ShowInTaskbar,
            Topmost = Topmost,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };

        if (g_updater.Visibility == Visibility.Visible)
        {
            g_updater.Hide();
        }
        else
        {
            g_updater.Show();
        }
    }
    #endregion

    #region 单击菜单项
    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            OnMenuItemClick(item);
        }
    }

    public void OnMenuItemClick(MenuItem item)
    {
        var menuItemName = item.Name;
        switch (menuItemName)
        {
            case "menu_exit" or "tray_exit":
                Close();
                break;
            case "menu_hide_border":
                g_conf.HideBorder = !g_conf.HideBorder;
                item.IsChecked = g_conf.HideBorder;
                var tkns = new Thickness(g_conf.HideBorder ? 0 : 1);
                Resources["BorderThickness"] = tkns;
                border.BorderThickness = tkns;
                g_configWindow?.InitBorderThickess(g_conf.HideBorder);
                break;
            case "menu_dark":
                g_conf.DarkMode = !g_conf.DarkMode;
                item.IsChecked = g_conf.DarkMode;
                InitColor();
                g_configWindow?.InitColor();
                break;
            case "menu_topmost":
                g_conf.Topmost = !g_conf.Topmost;
                item.IsChecked = g_conf.Topmost;
                Topmost = g_conf.Topmost;
                BorderTwinkle(g_conf.Topmost);
                break;
            case "menu_conf":
                MakeConfigWindow();
                break;
            case "menu_conf_file":
                var fullPath = Path.Combine(UserDataPath, "config.json");
                Process.Start(fullPath);
                break;
            case "menu_data_dir":
                Process.Start(UserDataPath);
                break;
            case "menu_show_in_taskbar":
                g_conf.ShowInTaskbar = !g_conf.ShowInTaskbar;
                item.IsChecked = g_conf.ShowInTaskbar;
                ShowInTaskbar = g_conf.ShowInTaskbar;
                BorderTwinkle(g_conf.ShowInTaskbar);
                break;
            case "menu_data_roll":
                g_conf.DataRoll = !g_conf.DataRoll;
                item.IsChecked = g_conf.DataRoll;
                DataUpdate();
                BorderTwinkle(g_conf.DataRoll);
                break;
            case "menu_lang":
                g_conf.Lang = g_conf.Lang == "cn" ? "en" : "cn";
                InitLang();
                DataUpdate();
                g_configWindow?.InitLang();
                break;
            case "menu_debug_mode":
                g_conf.Debug = !g_conf.Debug;
                item.IsChecked = g_conf.Debug;
                DataUpdate();
                BorderTwinkle(g_conf.Debug);
                break;
            case "menu_check_update" or "tray_ver":
                MakeUpdater();
                break;
            case "menu_transparent":
                g_conf.Transparent = !g_conf.Transparent;
                item.IsChecked = g_conf.Transparent;
                border.Background = g_conf.Transparent ? color_transparent : color_bg;
                DragBlock.Visibility = g_conf.Transparent ? Visibility.Visible : Visibility.Collapsed;
                BorderTwinkle(g_conf.Transparent);
                break;
            case "menu_reset_reminder" or "tray_reset_reminder":
                g_conf_stocks.ForEach(x =>
                {
                    x.ReminderTimes = 1;
                });
                g_configWindow?.UpdateDataGrid();
                break;
            case "tary_project_page":
                Process.Start($"https://github.com/pdone/{App.ProductName}");
                break;
            default:
                MessageBox.Show(this, "Nothing happened...", i18n[g_conf.Lang]["ui_title_tip"]);
                break;
        }
    }
    #endregion

    #region 边框闪烁
    /// <summary>
    /// 边框闪烁
    /// </summary>
    /// <param name="onEnable"></param>
    /// <param name="times"></param>
    /// <param name="interval"></param>
    private void BorderTwinkle(bool onEnable, int times = 2, int interval = 200)
    {
        Brush color = onEnable ? new SolidColorBrush(Colors.GreenYellow) : new SolidColorBrush(Colors.OrangeRed);
        BorderTwinkle(color, times, interval);
    }

    private void BorderTwinkle(Brush color = null, int times = 2, int interval = 200)
    {
        color ??= g_conf.DarkMode ? color_fg : new SolidColorBrush(Colors.OrangeRed);
        Task.Run(() =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                border.Padding = new Thickness(0);
                border.BorderThickness = new Thickness(2);
                border.BorderBrush = color;
            });
            times--;
            while (times-- > 0)
            {
                Delay(interval);
                Application.Current.Dispatcher.Invoke(() => border.BorderBrush = new SolidColorBrush(Colors.Black));
                Delay(interval);
                Application.Current.Dispatcher.Invoke(() => border.BorderBrush = color);
            }
            Delay(interval);
            Application.Current.Dispatcher.Invoke(() =>
            {
                border.Padding = new Thickness(1);
                border.BorderThickness = new Thickness(g_conf.HideBorder ? 0 : 1);
                border.BorderBrush = new SolidColorBrush(Colors.Black);
            });
        });
    }
    #endregion

    #region 隐藏窗口快捷键
    private const int BOSS_HOTKEY_ID = 1;
    private int _currentBossMod = MOD_CTRL;
    private int _currentBossVk = (int)System.Windows.Forms.Keys.Oemtilde;

    /// <summary>
    /// 老板键选项：(配置键名, 多语言标签键)
    /// </summary>
    public static readonly (string ConfigKey, string LabelKey)[] BossKeyOptions =
    [
        ("Ctrl+Oemtilde", "hotkey_ctrl_tilde"),
        ("Ctrl+D1", "hotkey_ctrl_1"),
        ("Ctrl+D2", "hotkey_ctrl_2"),
        ("Alt+Oemtilde", "hotkey_alt_tilde"),
        ("Alt+D1", "hotkey_alt_1"),
        ("Alt+D2", "hotkey_alt_2"),
    ];

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        // 从配置读取老板键并注册
        ParseBossKey(g_conf.BossKey, out _currentBossMod, out _currentBossVk);
        var hwnd = new WindowInteropHelper(this).Handle;
        RegisterHotKey(hwnd, BOSS_HOTKEY_ID, _currentBossMod, _currentBossVk);

        var source = HwndSource.FromHwnd(hwnd);
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == 0x0312) // 0x0312 是 WM_HOTKEY 消息
        {
            if (wparam.ToInt32() == BOSS_HOTKEY_ID)
            {
                ShowOrHiden();
            }
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// 重新注册老板键（由配置窗口调用）
    /// </summary>
    /// <param name="newBossKey">新的老板键配置键名</param>
    public void ReRegisterBossKey(string newBossKey)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        UnregisterHotKey(hwnd, BOSS_HOTKEY_ID);

        ParseBossKey(newBossKey, out var mod, out var vk);
        if (RegisterHotKey(hwnd, BOSS_HOTKEY_ID, mod, vk) == 0)
        {
            // 注册失败，快捷键被占用，恢复旧的注册
            RegisterHotKey(hwnd, BOSS_HOTKEY_ID, _currentBossMod, _currentBossVk);
            var displayName = GetHotKeyDisplayName(newBossKey);
            var msg = string.Format(i18n[g_conf.Lang]["msg_hotkey_register_failed"], displayName);
            g_configWindow?.ShowMessage(msg, i18n[g_conf.Lang]["ui_title_warn"]);
            return;
        }

        _currentBossMod = mod;
        _currentBossVk = vk;
        g_conf.BossKey = newBossKey;
        g_conf.Save();
    }

    /// <summary>
    /// 解析老板键配置字符串为 Win32 modifier 和 vk
    /// </summary>
    private static void ParseBossKey(string bossKey, out int modifier, out int vk)
    {
        var parts = bossKey.Split('+');
        modifier = parts[0] == "Alt" ? MOD_ALT : MOD_CTRL;
        vk = parts[1] switch
        {
            "Oemtilde" => (int)System.Windows.Forms.Keys.Oemtilde,
            "D1" => (int)System.Windows.Forms.Keys.D1,
            "D2" => (int)System.Windows.Forms.Keys.D2,
            _ => (int)System.Windows.Forms.Keys.Oemtilde
        };
    }

    /// <summary>
    /// 获取老板键的显示名称
    /// </summary>
    private static string GetHotKeyDisplayName(string configKey)
    {
        var opt = Array.Find(BossKeyOptions, x => x.ConfigKey == configKey);
        return opt.LabelKey != null ? i18n[g_conf.Lang][opt.LabelKey] : configKey;
    }

    private void ShowOrHiden()
    {
        if (Visibility == Visibility.Visible)
        {
            Hide();
            g_configWindow?.Hide();
        }
        else
        {
            Show();
            if (ConfigWindowShow)
            {
                MakeConfigWindow();
            }
        }
    }
    #endregion
}
