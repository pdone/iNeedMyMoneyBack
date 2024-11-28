using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using RestSharp;
using static iNeedMyMoneyBack.Utils;

namespace iNeedMyMoneyBack;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    public static Config g_conf = new();
    public static StockConfigArray g_conf_stocks = [];
    private RestClient g_client;
    private static int g_codeIndex = 0;
    private static readonly Dictionary<string, string> g_codeDatas = [];
    private readonly BackgroundWorker g_worker = new()
    {
        WorkerSupportsCancellation = true,
    };
    private ConfigWindow g_configWindow;
    /// <summary>
    /// 重要指数
    /// </summary>
    public readonly StockConfigArray ImportantIndexs =
    [
        new StockConfig("sh000001"),
        new StockConfig("sz399001"),
        new StockConfig("sz399006"),
        new StockConfig("sz399300"),
        new StockConfig("bj899050"),
    ];
    /// <summary>
    /// 增加菜单不透明度 避免与主界面重叠时显示不清除
    /// </summary>
    private const double MenuOpacityAdded = 0.2;
    /// <summary>
    /// 数值补齐长度
    /// </summary>
    private const int PricePad = 5;
    /// <summary>
    /// 界面状态
    /// </summary>
    public enum UIStatus
    {
        Normal,
        UpLimit,
        DownLimit,
        ProgramError,
    }
    /// <summary>
    /// 常用符号
    /// </summary>
    public struct Symbols
    {
        public const string ArrowRight = "→";
        public const string ArrowLeft = "←";
        public const string ArrowLeftRight = "↔";
        public const string ArrowUp = "↑";
        public const string ArrowUpDown = "↕";
        public const string Wave = "↗";
    }

    public MainWindow()
    {
        InitializeComponent();
        InitGlobalData();
        InitUI();
        LodaUpdater();
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
            g_conf_stocks.Add(new StockConfig("sh000001"));
            g_conf_stocks.Add(new StockConfig("sz399001"));
        }

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
        menu.Opacity = Math.Min(Opacity + MenuOpacityAdded, 1);
        menu_dark.IsChecked = g_conf.DarkMode;
        menu_topmost.IsChecked = g_conf.Topmost;
        menu_show_in_taskbar.IsChecked = g_conf.ShowInTaskbar;
        menu_data_roll.IsChecked = g_conf.DataRoll;
        menu_debug_mode.IsChecked = g_conf.Debug;
        menu_hide_border.IsChecked = g_conf.HideBorder;
        Resources["BorderThickness"] = new Thickness(g_conf.HideBorder ? 0 : 1);

        // 界面事件绑定
        PreviewMouseDown += (_, __) => DragWindow(this);
        PreviewMouseWheel += (_, e) => OnPreviewMouseWheel(e);
        PreviewKeyDown += OnKeyDown;
        Closing += MainWindow_Closing;
        menu.PreviewMouseWheel += (_, e) => OnPreviewMouseWheel(e);
        menu.KeyDown += OnKeyDown;
        menu_opacity.PreviewMouseWheel += (_, e) => OnPreviewMouseWheel(e, false);
        g_worker.DoWork += DoWork;
        g_worker.RunWorkerAsync();

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
    private void InitColor()
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

        border.Background = color_bg;
        lb.Foreground = color_fg;
        menu.Background = color_bg;
        menu.Foreground = color_fg;
        Resources["TextColor"] = color_fg;
        Resources["SubMenuBackground"] = color_bg;

        var tempSubMenuMask = new SolidColorBrush
        {
            Color = color_bg.Color,
            Opacity = menu.Opacity
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
        menu_ver.Header = $"{i18n[g_conf.Lang][menu_ver.Name]} {App.ProductVersion}(_V)";
        SetMenuItemHeader(menu_exit, "X");
        SetMenuItemHeader(menu_hide_border, "H");
        SetMenuItemHeader(menu_dark, "N");
        SetMenuItemHeader(menu_topmost, "T");
        SetMenuItemHeader(menu_conf, "C");
        SetMenuItemHeader(menu_conf_file, "F");
        SetMenuItemHeader(menu_data_dir, "D");
        SetMenuItemHeader(menu_show_in_taskbar, "B");
        SetMenuItemHeader(menu_data_roll, "R");
        SetMenuItemHeader(menu_lang, "L");
        SetMenuItemHeader(menu_ui, "U");
        SetMenuItemHeader(menu_check_update, "U");
        SetMenuItemHeader(menu_debug_mode);
        menu_opacity.Header = string.Format(i18n[g_conf.Lang]["menu_opacity"], (Opacity * 100).ToString("f0"));
        menu_opacity.InputGestureText = i18n[g_conf.Lang]["menu_opacity_igt"];
    }

    /// <summary>
    /// 主窗口关闭事件
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MainWindow_Closing(object sender, CancelEventArgs e)
    {
        g_worker.CancelAsync();
        g_conf.Left = Left;
        g_conf.Top = Top;
        g_conf.Width = Width;
        g_conf.Height = Height;
        g_conf.Opacity = Opacity;
        g_conf.Save();
        g_conf_stocks.Save();
    }

    /// <summary>
    /// 鼠标滚轮事件
    /// </summary>
    /// <param name="e"></param>
    /// <param name="needPressCtrl"></param>
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

        menu.Opacity = Math.Min(Opacity + MenuOpacityAdded, 1);
        var tempSubMenuMask = new SolidColorBrush
        {
            Color = color_bg.Color,
            Opacity = menu.Opacity
        };
        Resources["SubMenuMask"] = tempSubMenuMask;
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
            var menuItemName = e.Key switch
            {
                Key.B => menu_show_in_taskbar.Name,
                Key.C => menu_conf.Name,
                Key.D => menu_data_dir.Name,
                Key.F => menu_conf_file.Name,
                Key.H => menu_hide_border.Name,
                Key.U => menu_check_update.Name,
                Key.L => menu_lang.Name,
                Key.N => menu_dark.Name,
                Key.R => menu_data_roll.Name,
                Key.T => menu_topmost.Name,
                Key.X => menu_exit.Name,
                _ => ""
            };
            if (menuItemName.IsNullOrWhiteSpace())
            {
                return;
            }
            OnMenuItemClick(menuItemName);
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
    private void SetMenuItemHeader(MenuItem menuItem, string shortcuts = null)
    {
        if (i18n.ContainsKey(g_conf.Lang) && i18n[g_conf.Lang].ContainsKey(menuItem.Name))
        {
            if (shortcuts.IsNullOrWhiteSpace())
            {
                menuItem.Header = i18n[g_conf.Lang][menuItem.Name];
            }
            else
            {
                menuItem.Header = i18n[g_conf.Lang][menuItem.Name] + $"(_{shortcuts})";
            }
        }
    }

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
    /// 整理数据
    /// </summary>
    /// <param name="sc">配置</param>
    /// <param name="res">最新数据</param>
    /// <returns></returns>
    private string StockInfoHandle(ref StockConfig sc, StockInfo res)
    {
        sc.Name = res.StockName;
        var info = $"{sc.DiaplayName}";
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
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value)// 启用字段
            {
                continue;
            }
            var dot = res.StockName.EndsWith("ETF") ? "f3" : "f2";
            var fieldData = kvp.Key switch
            {
                "ui_price" => $" {res.CurrentPrice.ToString(dot),PricePad}",
                "ui_change" => $" {res.PriceChangePercent,PricePad:f2}%",
                "ui_buy_price" => hold ? $" {sc.BuyPrice,PricePad:f2}" : "",
                "ui_num" => hold ? $" {sc.BuyCount,PricePad}" : "",
                "ui_cost" => hold ? $" {sc.Cost,PricePad:f0}" : "",
                "ui_market_value" => hold ? $" {sc.MarketValue,PricePad:f0}" : "",
                "ui_yield" => hold ? $" {sc.Yield,PricePad:f2}%" : "",
                "ui_day_make" => hold ? $" {sc.DayMake,PricePad:f0}" : "",
                "ui_all_make" => hold ? $" {sc.AllMake,PricePad:f0}" : "",
                "ui_yesterday_todayopen" => $" {res.YesterdayClose.ToString(dot),PricePad}{Symbols.ArrowRight}{res.TodayOpen.ToString(dot),-PricePad}",
                "ui_lowest_highest" => $" {res.LowestPrice.ToString(dot),PricePad}{Symbols.ArrowUpDown}{res.HighestPrice.ToString(dot),-PricePad}",
                "ui_limitup_limitdown" => res.PriceLimitDown != res.PriceLimitUp ? $" {res.PriceLimitDown.ToString(dot),PricePad}{Symbols.Wave}{res.PriceLimitUp.ToString(dot),-PricePad}" : "",
                _ => ""
            };
            info += fieldData;
        }
        return info;
    }

    public void DoWork(object sender, EventArgs e)
    {
        while (true)
        {
            DataUpdate(true);
            Delay();// 请求间隔最低 2s
        }
    }

    public async void DataUpdate(bool delay = false)
    {
        if (!IsTradingTime() && !g_conf.Debug)// 非交易时间
        {
            UpdateUI(i18n[g_conf.Lang]["ui_nontrading"]);
            if (delay)
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
            var res = await Request(stock);
            if (res == null)
            {
                UpdateUI(i18n[g_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                Delay(10000);
                return;
            }
            var text = StockInfoHandle(ref stock, res);
            if (res.CurrentPrice >= res.PriceLimitUp)
            {
                UpdateUI(text, UIStatus.UpLimit);
            }
            else if (res.CurrentPrice <= res.PriceLimitDown)
            {
                UpdateUI(text, UIStatus.DownLimit);
            }
            else
            {
                UpdateUI(text);
            }

            g_codeIndex++;
            if (g_conf_stocks.Count < g_codeIndex + 1)
            {
                g_codeIndex = 0;
            }
        }
        else// 多行数据同步展示
        {
            var stocks = g_conf_stocks.Union(ImportantIndexs).ToList();
            var res = await Request(stocks);
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
            foreach (var info in res)
            {
                var stock = stocks.FirstOrDefault(x =>
                {
                    if (x.Code.IsNullOrWhiteSpace() || x.Code.Length <= 2) { return false; }
                    return x.Code.Trim().Remove(0, 2) == info.StockCode;
                });
                if (stock == null || stock.Code.IsNullOrWhiteSpace())
                {
                    continue;
                }
                if (ImportantIndexs.Any(x => x.Code == stock.Code))
                {
                    ImportantIndexs[stock.Code].IndexInfo = $"{i18n[g_conf.Lang][StockIndexPrefix + stock.Code]} {info.CurrentPrice:f2} {info.PriceChangePercent:f2}%";
                    continue;
                }
                g_codeDatas[stock.Code] = StockInfoHandle(ref stock, info);

                daymake += stock.DayMake;
                allmake += stock.AllMake;
                allcost += stock.Cost;
                allmarketvalue += stock.Cost + stock.AllMake;
                if (info.PriceChangePercent < -9)
                {
                    hasDownLimit = true;
                }
                else if (info.PriceChangePercent > 9)
                {
                    hasUpLimit = true;
                }
            }
            var allyield = 0.0;
            if (allcost != 0.0)
            {
                allyield = allmake / allcost * 100;// 总收益率
            }
            var list = g_codeDatas.Select(x => x.Value);
            var content = string.Join(Environment.NewLine, list);
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
                    "ui_all_yield" => $"{newline}{field} {allyield:f2}% ",
                    _ => ""
                };
                if (item.Key.StartsWith(StockIndexPrefix))
                {
                    temp = $"{newline}{ImportantIndexs[item.Key]?.IndexInfo} ";
                }
                content += temp;
            }
            var status = UIStatus.Normal;
            if (hasUpLimit)
            {
                status = UIStatus.UpLimit;
            }
            else if (hasDownLimit)
            {
                status = UIStatus.DownLimit;
            }
            UpdateUI(content, status);
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

    public static SolidColorBrush color_bg;
    public static SolidColorBrush color_fg;

    private void UpdateUI(string msg, UIStatus status = UIStatus.Normal)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            lb.Content = msg;
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
                lb.Foreground = new SolidColorBrush(Colors.Yellow);
                border.Background = new SolidColorBrush(Colors.Red);
                break;
            case UIStatus.DownLimit:
                lb.Foreground = new SolidColorBrush(Colors.YellowGreen);
                border.Background = new SolidColorBrush(Colors.Green);
                break;
            case UIStatus.ProgramError:
                lb.Foreground = new SolidColorBrush(Colors.Yellow);
                border.Background = new SolidColorBrush(Colors.Purple);
                break;
            case UIStatus.Normal:
            default:
                break;
        }
    }

    private async Task<StockInfo> Request(StockConfig sc)
    {
        try
        {
            var request = new RestRequest();
            request.AddQueryParameter("q", sc.Code);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var content = response.Content;
                var info = StockInfo.Get(content);
                return info;
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

    private async Task<List<StockInfo>> Request(List<StockConfig> scs)
    {
        try
        {
            var codes = string.Join(",", scs.Where(x => x.Code.IsNullOrWhiteSpace() == false).Select(x => x.Code));
            var request = new RestRequest();
            request.AddQueryParameter("q", codes);
            var response = await g_client.GetAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var contents = response.Content.Split(';');
                var list = new List<StockInfo>();
                if (contents.Length == 0)
                {
                    return list;
                }
                foreach (var content in contents)
                {
                    if (content.IsNullOrWhiteSpace())
                    {
                        continue;
                    }
                    var stock = StockInfo.Get(content.Trim());
                    if (stock != null)
                    {
                        list.Add(stock);
                    }
                }
                return list;
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
        g_configWindow.PreviewMouseWheel += (_, e) => OnPreviewMouseWheel(e);
        g_configWindow.KeyDown += OnKeyDown;
        if (g_configWindow.Visibility == Visibility.Visible)
        {
            g_configWindow.Hide();
        }
        else
        {
            g_configWindow.Show();
        }
        Focus();
    }

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem item)
        {
            OnMenuItemClick(item.Name);
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
                Resources["BorderThickness"] = new Thickness(g_conf.HideBorder ? 0 : 1);
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
                var startInfo = new ProcessStartInfo()
                {
                    FileName = UpdaterPath,
                    Arguments = $"{App.UpdateMask}" +
                    $" {App.ProductVersion}" +
                    $" {g_conf.CheckUpdateUrl}" +
                    $" {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, App.ProductFileName)}" +
                    $" {UpdaterIcoPath}",
                };
                Process.Start(startInfo);
                break;
            default:
                MessageBox.Show(this, "Nothing happened...", i18n[g_conf.Lang]["ui_title_tip"]);
                break;
        }
    }

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
                border.BorderThickness = new Thickness(1);
                border.BorderBrush = new SolidColorBrush(Colors.Black);
            });
        });
    }
}
