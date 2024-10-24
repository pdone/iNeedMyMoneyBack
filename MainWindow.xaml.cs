using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RestSharp;

namespace iNeedMyMoneyBack;

/// <summary>
/// MainWindow.xaml 的交互逻辑
/// </summary>
public partial class MainWindow : Window
{
    private RestClient g_client;
    public static Config g_conf = new();
    public static Dictionary<string, Dictionary<string, string>> g_i18n;
    private static int g_codeIndex = 0;
    private static readonly Dictionary<string, string> g_codeDatas = [];
    private readonly BackgroundWorker g_worker = new()
    {
        WorkerSupportsCancellation = true,
    };

    public struct Symbols
    {
        public const string ArrowRight = "→";
        public const string ArrowLeft = "←";
        public const string ArrowLeftRight = "↔";
        public const string ArrowUp = "↑";
        public const string ArrowUpDown = "↕";
        public const string Wave = "~";

    }

    public enum UIStatus
    {
        Normal,
        UpLimit,
        DownLimit,
        ProgramError,
    }

    public MainWindow()
    {
        InitializeComponent();
        Init();
    }

    private void Init()
    {
        g_i18n = Utils.LoadLangData();
        g_conf = Utils.LoadConfig();

        if (g_conf != null && g_conf.Stocks.Count == 0)
        {
            g_conf.Stocks.Add(new StockConfig("sh000001", "上证指数", 3200));
            g_conf.Stocks.Add(new StockConfig("sz399001"));
        }

        InitUI();

        if (g_client == null)
        {
            g_client = new RestClient(g_conf.Api);
            g_client.AddDefaultHeader("User-Agent", g_conf.UserAgent);
        }

        MouseDown += (sender, e) => Utils.DragWindow(this);
        Closing += (sender, e) =>
        {
            g_conf.Left = Left;
            g_conf.Top = Top;
            g_conf.Width = Width;
            g_conf.Height = Height;
            Utils.SaveConfig(g_conf);
        };
        DoWork(null, null);
        g_worker.DoWork += DoWork;
    }

    /// <summary>
    /// 初始化界面
    /// </summary>
    private void InitUI()
    {
        Width = g_conf.Width;
        Height = g_conf.Height;
        Left = g_conf.Left;
        Top = g_conf.Top;
        menu_dark.IsChecked = g_conf.DarkMode;
        menu_topmost.IsChecked = g_conf.Topmost;
        menu.Opacity = g_conf.Opacity;
        Opacity = g_conf.Opacity;
        Topmost = g_conf.Topmost;
        ShowInTaskbar = g_conf.ShowInTaskbar;
        menu_show_in_taskbar.IsChecked = g_conf.ShowInTaskbar;
        menu_data_roll.IsChecked = g_conf.DataRoll;
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
    }
    /// <summary>
    /// 初始化语言配置
    /// </summary>
    private void InitLang()
    {
        var asm = Assembly.GetExecutingAssembly();
        var fvi = FileVersionInfo.GetVersionInfo(asm.Location);
        menu_ver.Header = $"{g_i18n[g_conf.Lang][menu_ver.Name]} {fvi.ProductVersion}";
        SetMenuItemHeader(menu_exit);
        SetMenuItemHeader(menu_dark);
        SetMenuItemHeader(menu_topmost);
        SetMenuItemHeader(menu_conf);
        SetMenuItemHeader(menu_exit);
        SetMenuItemHeader(menu_conf_file);
        SetMenuItemHeader(menu_show_in_taskbar);
        SetMenuItemHeader(menu_data_roll);
        SetMenuItemHeader(menu_lang);
    }

    /// <summary>
    /// 设置菜单文本
    /// </summary>
    /// <param name="menuItem"></param>
    private void SetMenuItemHeader(MenuItem menuItem)
    {
        if (g_i18n.ContainsKey(g_conf.Lang) && g_i18n[g_conf.Lang].ContainsKey(menuItem.Name))
        {
            menuItem.Header = g_i18n[g_conf.Lang][menuItem.Name];
        }
    }

    private string GetFieldName()
    {
        var fieldName = $"{Environment.NewLine}{g_i18n[g_conf.Lang]["ui_name"]}";
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value)// 启用字段
            {
                continue;
            }
            var fieldData = kvp.Key switch
            {
                "ui_yesterday_todayopen" => $" {g_i18n[g_conf.Lang]["ui_yesterday"]}{Symbols.ArrowRight}{g_i18n[g_conf.Lang]["ui_todayopen"]}",
                "ui_lowest_highest" => $" {g_i18n[g_conf.Lang]["ui_lowest"]}{Symbols.ArrowUpDown}{g_i18n[g_conf.Lang]["ui_highest"]}",
                "ui_limitup_limitdown" => $" {g_i18n[g_conf.Lang]["ui_limitdown"]}{Symbols.Wave}{g_i18n[g_conf.Lang]["ui_limitup"]}",
                _ => $" {g_i18n[g_conf.Lang][kvp.Key]}"
            };
            fieldName += fieldData;
        }
        return fieldName;
    }

    private const int PricePad = 5;
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
        foreach (var kvp in g_conf.FieldControls)
        {
            if (!kvp.Value)// 启用字段
            {
                continue;
            }
            var fieldData = kvp.Key switch
            {
                "ui_price" => $" {res.CurrentPrice,PricePad}",
                "ui_change" => $" {res.PriceChangePercent,6}%",
                "ui_cost" => $" {sc.BuyPrice,PricePad:f2}",
                "ui_num" => $" {sc.BuyCount,PricePad}",
                "ui_day_make" => $" {sc.DayMake,PricePad:f0}",
                "ui_all_make" => $" {sc.AllMake,PricePad:f0}",
                "ui_yesterday_todayopen" => $" {res.YesterdayClose,PricePad}{Symbols.ArrowRight}{res.TodayOpen,-PricePad}",
                "ui_lowest_highest" => $" {res.LowestPrice,PricePad}{Symbols.ArrowUpDown}{res.HighestPrice,-PricePad}",
                "ui_limitup_limitdown" => res.PriceLimitDown != res.PriceLimitUp ? $" {res.PriceLimitDown,PricePad}{Symbols.Wave}{res.PriceLimitUp,-PricePad}" : "",
                _ => ""
            };
            info += fieldData;
        }
        return info;
    }

    public async void DoWork(object sender, EventArgs e)
    {
        while (!g_worker.IsBusy)
        {
            if (!Utils.IsTradingTime() && !g_conf.Debug)// 非交易时间
            {
                UpdateUI(g_i18n[g_conf.Lang]["ui_nontrading"]);
                await Delay(30000);
                continue;
            }

            if (g_conf.DataRoll)// 单行数据滚动展示
            {
                if (g_codeIndex > g_conf.Stocks.Count - 1)
                {
                    g_codeIndex = 0;
                    await Delay();
                    continue;
                }

                var stock = g_conf.Stocks.ElementAt(g_codeIndex);
                var res = await Request(stock);
                if (res == null)
                {
                    UpdateUI(g_i18n[g_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                    await Delay(10000);
                    continue;
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
                if (g_conf.Stocks.Count < g_codeIndex + 1)
                {
                    g_codeIndex = 0;
                }
            }
            else// 多行数据同步展示
            {
                var stocks = g_conf.Stocks;
                var res = await Request(stocks);
                if (res == null || res.Count == 0)
                {
                    UpdateUI(g_i18n[g_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                    await Delay(10000);
                    continue;
                }
                var daymake = 0.0;
                var allmake = 0.0;
                foreach (var info in res)
                {
                    var stock = stocks.FirstOrDefault(x => x.Code.Trim().Remove(0, 2) == info.StockCode);
                    if (stock == null)
                    {
                        continue;
                    }
                    var text = StockInfoHandle(ref stock, info);
                    if (g_codeDatas.ContainsKey(stock.Code))
                    {
                        g_codeDatas[stock.Code] = text;
                    }
                    else
                    {
                        g_codeDatas.Add(stock.Code, text);
                    }
                    daymake += stock.DayMake;
                    allmake += stock.AllMake;
                }
                var list = g_codeDatas.Select(x => x.Value);
                var content = string.Join(Environment.NewLine, list);
                foreach (var kvp in g_conf.ExtendControls)
                {
                    if (!kvp.Value)
                    {
                        continue;
                    }
                    if (!g_i18n[g_conf.Lang].TryGetValue(kvp.Key, out var field))
                    {
                        continue;
                    }
                    var temp = kvp.Key switch
                    {
                        "ui_fieldname" => GetFieldName(),
                        "ui_all_stock_day_make" => $"{Environment.NewLine}{field} {daymake,-8:f2}",
                        "ui_all_stock_all_make" => $"{Environment.NewLine}{field} {allmake,-8:f2}",
                        _ => field
                    };
                    content += temp;
                }

                UpdateUI(content);
            }

            await Delay();// 请求间隔最低 2s
        }
    }

    private async Task Delay(int ms = 2000)
    {
        await Task.Delay(g_conf.Interval < 2 ? ms : g_conf.Interval * 1000);
    }

    public static Brush color_bg;
    public static Brush color_fg;

    private void UpdateUI(string msg, UIStatus status = UIStatus.Normal)
    {
        if (status != UIStatus.Normal)
        {
            UpdateColor(status);
        }
        Application.Current.Dispatcher.Invoke(() => { lb.Content = msg; });
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
                var info = new StockInfo(content);
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
            var codes = string.Join(",", scs.Select(x => x.Code));
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
                    var stock = new StockInfo(content.Trim());
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

    private void MenuItem_Click(object sender, RoutedEventArgs e)
    {
        var item = (MenuItem)sender;
        if (item != null)
        {
            switch (item.Name)
            {
                case "menu_exit":
                    Close();
                    break;
                case "menu_dark":
                    g_conf.DarkMode = !g_conf.DarkMode;
                    menu_dark.IsChecked = g_conf.DarkMode;
                    InitColor();
                    break;
                case "menu_topmost":
                    g_conf.Topmost = !g_conf.Topmost;
                    menu_topmost.IsChecked = g_conf.Topmost;
                    Topmost = g_conf.Topmost;
                    break;
                case "menu_conf":
                    var cw = new ConfigWindow(this)
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        Left = Left + Width,
                        Top = Top
                    };
                    cw.ShowDialog();
                    if ((bool)cw.DialogResult)
                    {
                        Utils.SaveConfig(g_conf);
                    }
                    break;
                case "menu_conf_file":
                    var fullPath = Path.Combine(Utils.UserDataPath, "config.json");
                    Process.Start(fullPath);
                    break;
                case "menu_show_in_taskbar":
                    g_conf.ShowInTaskbar = !g_conf.ShowInTaskbar;
                    menu_show_in_taskbar.IsChecked = g_conf.ShowInTaskbar;
                    ShowInTaskbar = g_conf.ShowInTaskbar;
                    break;
                case "menu_data_roll":
                    g_conf.DataRoll = !g_conf.DataRoll;
                    menu_data_roll.IsChecked = g_conf.DataRoll;
                    break;
                case "menu_lang":
                    g_conf.Lang = g_conf.Lang == "cn" ? "en" : "cn";
                    InitLang();
                    DoWork(null, null);
                    break;
                case "menu_ver":
                default:
                    MessageBox.Show("nothing happened~");
                    break;
            }
        }
    }
}
