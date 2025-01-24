using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public static string TongHuaShun(string arg) => HttpUtility.UrlEncode($"https://www.iwencai.com/unifiedwap/result?w={arg}");
    }
    /// <summary>
    /// 增加菜单不透明度 避免与主界面重叠时显示不清除
    /// </summary>
    private const double MenuOpacityAdded = 0.3;
    /// <summary>
    /// 告警值 涨跌幅的绝对值大于此值时 界面高亮提示 单位 %
    /// </summary>
    private const int AlertValue = 9;
    /// <summary>
    /// 界面状态
    /// </summary>
    private enum UIStatus
    {
        Normal,
        UpLimit,
        DownLimit,
        ProgramError,
    }
    /// <summary>
    /// 常用符号
    /// </summary>
    private struct Symbols
    {
        public const string ArrowRight = "→";
        public const string ArrowLeft = "←";
        public const string ArrowLeftRight = "↔";
        public const string ArrowUp = "↑";
        public const string ArrowUpDown = "↕";
        public const string Wave = "↗";
        public const string RightUp = Wave;
        public const string RightDown = "↘";
    }
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

        g_conf_stocks_with_index = g_conf_stocks.Union(StockConfigArray.ImportantIndexs).ToList();

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
        MainLabel.PreviewMouseDoubleClick += OnPreviewMouseDoubleClick_MainLabel;
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
        UnregisterHotKey(hwnd, HotKeys.MainShow);
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
    /// 左键双击主文本事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnPreviewMouseDoubleClick_MainLabel(object sender, MouseButtonEventArgs e)
    {
        if (g_conf.Transparent)// 透明背景时不触发
        {
            return;
        }
        if (e.ChangedButton != MouseButton.Left)// 仅左键触发
        {
            return;
        }
        if (sender is not Label tempLabel)
        {
            return;
        }
        if (tempLabel.Content is not TextBlock tempTextBlock)
        {
            return;
        }
        var y = e.GetPosition(tempLabel).Y;
        // 使用反射获取TextBlock的行数
        var lineCountProperty = typeof(TextBlock).GetProperty("LineCount", BindingFlags.NonPublic | BindingFlags.Instance);
        // 文本行数
        var lineCount = (int)lineCountProperty.GetValue(tempTextBlock, null);
        // 计算行高
        var lineHeight = tempTextBlock.ActualHeight / lineCount;
        if (y > g_conf_stocks.Count() * lineHeight)
        {
            return;
        }
        var idx = (int)(y / lineHeight);
        if (idx < 0 || idx > g_conf_stocks.Count() - 1)
        {
            return;
        }
        var stock = g_conf_stocks.ElementAt(idx);
        if (stock.Code.IsNullOrWhiteSpace())
        {
            return;
        }
        Process.Start(StockDetailPage.XueQiu(stock.Code));
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
            if (!kvp.Value)// 启用字段
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
            DataUpdate(false);
            Delay();// 请求间隔最低 2s
        }
    }
    #endregion

    #region 更新界面
    /// <summary>
    /// 更新界面显示内容
    /// </summary>
    /// <param name="useCache">是否使用上一次请求响应数据</param>
    public async void DataUpdate(bool useCache = true)
    {
        var isTradingTime = IsTradingTime();
        if (!isTradingTime && !g_conf.Debug)// 非交易时间
        {
            UpdateUI(i18n[g_conf.Lang]["ui_nontrading"]);
            if (!useCache)// 不使用缓存时 计时器定时请求 在非交易时间时需要延迟30秒
            {
                Delay(30000);
            }
            return;
        }

        if (g_conf.DataRoll)// 单行数据滚动展示
        {
            if (g_codeIndex > g_conf_stocks.Count - 1)
            {
                g_codeIndex = 0;
                Delay();
                return;
            }

            var stock = g_conf_stocks.ElementAt(g_codeIndex);
            var res = await Request(stock, useCache);
            if (res == null)
            {
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
            else if (res.CurrentPrice <= -AlertValue)
            {
                status = UIStatus.DownLimit;
            }
            UpdateUI(text, status);

            g_codeIndex++;
            if (g_conf_stocks.Count < g_codeIndex + 1)
            {
                g_codeIndex = 0;
            }
        }
        else// 多行数据同步展示
        {
            var res = await Request(g_conf_stocks_with_index, useCache);
            if (res == null || res.Count == 0)
            {
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
            var content = string.Empty;// 整理后的界面文本
            var reminder = "";// 提醒信息
            foreach (var info in res)
            {
                var stock = g_conf_stocks_with_index.FirstOrDefault(x =>
                {
                    if (x.Code.IsNullOrWhiteSpace() || x.Code.Length < 8) { return false; }
                    return x.Code.Trim().Remove(0, 2) == info.StockCode;
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
                content += StockInfoHandle(ref stock, info) + Environment.NewLine;
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

            var allyield = 0.0;
            var allyield_day = 0.0;
            if (allcost != 0.0)
            {
                allyield = allmake / allcost * 100;// 总收益率
                allyield_day = daymake / allcost * 100;// 总持日收益率
            }

            foreach (var item in g_conf.ExtendControls)
            {
                if (!item.Visable)
                {
                    if (item.NewLine)
                    {
                        content += Environment.NewLine;
                    }
                    continue;
                }
                if (!i18n[g_conf.Lang].TryGetValue(item.Key, out var field))
                {
                    continue;
                }
                var newline = item.NewLine ? Environment.NewLine : "";
                var temp = item.Key switch
                {
                    "ui_fieldname" => $"{GetFieldName(newline)} ",
                    "ui_all_stock_day_make" => $"{newline}{field} {daymake:f2} ",
                    "ui_all_stock_all_make" => $"{newline}{field} {allmake:f2} ",
                    "ui_all_cost" => $"{newline}{field} {allcost:f2} ",
                    "ui_all_market_value" => $"{newline}{field} {allmarketvalue:f2} ",
                    "ui_all_yield_day" => $"{newline}{field} {allyield_day:f2}% ",
                    "ui_all_yield" => $"{newline}{field} {allyield:f2}% ",
                    _ => ""
                };
                if (item.Key.StartsWith(StockIndexPrefix))
                {
                    temp = $"{newline}{StockConfigArray.ImportantIndexs[item.Key]?.IndexInfo} ";
                }
                content += temp;
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
            UpdateUI(content, status);
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
        Task.Delay(ms).Wait();
    }

    private void Delay()
    {
        Delay(g_conf.Interval < 2 ? 2000 : g_conf.Interval * 1000);
    }

    private void UpdateUI(string msg, UIStatus status = UIStatus.Normal)
    {
        if (this == null)
        {
            return;
        }

        Application.Current.Dispatcher.Invoke(() =>
        {
            MainTextBlock.Text = msg;
            if (status != UIStatus.Normal)
            {
                UpdateColor(status);
            }
        });
    }

    private void UpdateColor(UIStatus status = UIStatus.Normal)
    {
        switch (status)
        {
            case UIStatus.UpLimit:
                MainTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);
                border.Background = new SolidColorBrush(Colors.Red);
                break;
            case UIStatus.DownLimit:
                MainTextBlock.Foreground = new SolidColorBrush(Colors.YellowGreen);
                border.Background = new SolidColorBrush(Colors.Green);
                break;
            case UIStatus.ProgramError:
                MainTextBlock.Foreground = new SolidColorBrush(Colors.Yellow);
                border.Background = new SolidColorBrush(Colors.Purple);
                break;
            case UIStatus.Normal:
            default:
                break;
        }
    }
    #endregion

    #region 请求数据
    private async Task<StockInfo> Request(StockConfig sc, bool useCache)
    {
        try
        {
            if (useCache)
            {
                g_last_res ??= await Request(sc, false);// 使用缓存时 如果缓存为空则请求一次
                return g_last_res;
            }
            var request = new RestRequest();
            request.AddQueryParameter("q", sc.Code);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content;
                g_last_res = StockInfo.Get(content);
                return g_last_res;
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
                if (g_last_res_set == null || g_last_res_set.Count == 0)
                {
                    g_last_res_set = await Request(scs, false);// 使用缓存时 如果缓存为空则请求一次
                }
                return g_last_res_set;
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
                    return g_last_res_set;
                }
                g_last_res_set.Clear();
                foreach (var content in contents)
                {
                    if (content.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    var stock = StockInfo.Get(content.Trim());
                    if (stock != null)
                    {
                        g_last_res_set.Add(stock);
                    }
                }
                return g_last_res_set;
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
            // OnMenuItemClick(item.Name);
            OnMenuItemClick(item);
        }
    }

    public void OnMenuItemClick(string menuItemName)
    {
        switch (menuItemName)
        {
            case "menu_exit":
                Close();
                break;
            case "menu_hide_border":
                g_conf.HideBorder = !g_conf.HideBorder;
                menu_hide_border.IsChecked = g_conf.HideBorder;
                var tkns = new Thickness(g_conf.HideBorder ? 0 : 1);
                Resources["BorderThickness"] = tkns;
                border.BorderThickness = tkns;
                g_configWindow?.InitBorderThickess(g_conf.HideBorder);
                break;
            case "menu_dark":
                g_conf.DarkMode = !g_conf.DarkMode;
                menu_dark.IsChecked = g_conf.DarkMode;
                InitColor();
                g_configWindow?.InitColor();
                break;
            case "menu_topmost":
                g_conf.Topmost = !g_conf.Topmost;
                menu_topmost.IsChecked = g_conf.Topmost;
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
                menu_show_in_taskbar.IsChecked = g_conf.ShowInTaskbar;
                ShowInTaskbar = g_conf.ShowInTaskbar;
                BorderTwinkle(g_conf.ShowInTaskbar);
                break;
            case "menu_data_roll":
                g_conf.DataRoll = !g_conf.DataRoll;
                menu_data_roll.IsChecked = g_conf.DataRoll;
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
                menu_debug_mode.IsChecked = g_conf.Debug;
                DataUpdate();
                BorderTwinkle(g_conf.Debug);
                break;
            case "menu_check_update":
                MakeUpdater();
                break;
            case "menu_transparent":
                g_conf.Transparent = !g_conf.Transparent;
                menu_transparent.IsChecked = g_conf.Transparent;
                border.Background = g_conf.Transparent ? color_transparent : color_bg;
                DragBlock.Visibility = g_conf.Transparent ? Visibility.Visible : Visibility.Collapsed;
                break;
            default:
                MessageBox.Show(this, "Nothing happened...", i18n[g_conf.Lang]["ui_title_tip"]);
                break;
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
    public struct HotKeys
    {
        public static int MainShow => (int)System.Windows.Forms.Keys.Oemtilde;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var hwnd = new WindowInteropHelper(this).Handle;
        RegisterHotKey(hwnd, HotKeys.MainShow, MOD_CTRL, HotKeys.MainShow);

        var source = HwndSource.FromHwnd(hwnd);
        source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
    {
        if (msg == 0x0312) // 0x0312 是 WM_HOTKEY 消息
        {
            var hotKeyId = wparam.ToInt32();
            if (hotKeyId == HotKeys.MainShow)
            {
                ShowOrHiden();
            }
        }
        return IntPtr.Zero;
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
