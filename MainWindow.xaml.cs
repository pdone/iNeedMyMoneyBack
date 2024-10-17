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
    private RestClient _client;
    public static Config _conf = new();
    public static Dictionary<string, Dictionary<string, string>> _i18n;

    private static int codeIndex = 0;
    private static readonly Dictionary<string, string> codeDatas = [];
    private readonly BackgroundWorker worker = new()
    {
        WorkerSupportsCancellation = true,
    };

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
        _i18n = Utils.LoadLangData();
        _conf = Utils.LoadConfig();

        if (_conf != null && _conf.Stocks.Count == 0)
        {
            _conf.Stocks.Add(new StockConfig("sh000001", "上证指数", 3200));
            _conf.Stocks.Add(new StockConfig("sz399001"));
        }

        InitUI();

        if (_client == null)
        {
            _client = new RestClient("http://qt.gtimg.cn");
            _client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36 Edg/126.0.0.0");
        }

        MouseDown += (sender, e) => Utils.DragWindow(this);
        Closing += (sender, e) =>
        {
            _conf.Left = Left;
            _conf.Top = Top;
            _conf.Width = Width;
            _conf.Height = Height;
            Utils.SaveConfig(_conf);
        };
        DoWork(null, null);
        worker.DoWork += DoWork;
    }

    /// <summary>
    /// 初始化界面
    /// </summary>
    private void InitUI()
    {
        Width = _conf.Width;
        Height = _conf.Height;
        Left = _conf.Left;
        Top = _conf.Top;
        menu_dark.IsChecked = _conf.DarkMode;
        menu_topmost.IsChecked = _conf.Topmost;
        menu.Opacity = _conf.Opacity;
        Opacity = _conf.Opacity;
        Topmost = _conf.Topmost;
        ShowInTaskbar = _conf.ShowInTaskbar;
        menu_show_in_taskbar.IsChecked = _conf.ShowInTaskbar;
        menu_data_roll.IsChecked = _conf.DataRoll;
        InitColor();
        InitLang();
    }
    /// <summary>
    /// 初始化配色
    /// </summary>
    private void InitColor()
    {
        if (_conf.DarkMode)
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
        menu_ver.Header = $"{_i18n[_conf.Lang][menu_ver.Name]} {fvi.ProductVersion}";
        SetMenuItemHeader(menu_exit);
        SetMenuItemHeader(menu_dark);
        SetMenuItemHeader(menu_topmost);
        SetMenuItemHeader(menu_conf);
        SetMenuItemHeader(menu_exit);
        SetMenuItemHeader(menu_conf_file);
        SetMenuItemHeader(menu_show_in_taskbar);
        SetMenuItemHeader(menu_data_roll);
    }

    /// <summary>
    /// 设置菜单文本
    /// </summary>
    /// <param name="menuItem"></param>
    private void SetMenuItemHeader(MenuItem menuItem)
    {
        if (_i18n.ContainsKey(_conf.Lang) && _i18n[_conf.Lang].ContainsKey(menuItem.Name))
        {
            menuItem.Header = _i18n[_conf.Lang][menuItem.Name];
        }
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
        var info = $"{sc.DiaplayName} {res.CurrentPrice:f2} {res.PriceChangePercent}%";
        if (sc.BuyPrice > 0)
        {
            var makeMoney = res.CurrentPrice - sc.BuyPrice;
            if (sc.BuyCount > 0)
            {
                makeMoney *= sc.BuyCount;
            }
            info += $" {makeMoney:f2}";
        }
        return info;
    }

    private async void DoWork(object sender, EventArgs e)
    {
        while (!worker.IsBusy)
        {
            if (!Utils.IsTradingTime())// 非交易时间
            {
                UpdateUI(_i18n[_conf.Lang]["ui_nontrading"]);
                await Delay(30000);
                continue;
            }

            if (_conf.DataRoll)// 单行数据滚动展示
            {
                if (codeIndex > _conf.Stocks.Count - 1)
                {
                    codeIndex = 0;
                    await Delay();
                    continue;
                }

                var stock = _conf.Stocks.ElementAt(codeIndex);
                var res = await Request(stock);
                if (res == null)
                {
                    UpdateUI(_i18n[_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
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

                codeIndex++;
                if (_conf.Stocks.Count < codeIndex + 1)
                {
                    codeIndex = 0;
                }
            }
            else// 多行数据同步展示
            {
                var stocks = _conf.Stocks;
                var res = await Request(stocks);
                if (res == null || res.Count == 0)
                {
                    UpdateUI(_i18n[_conf.Lang]["ui_getdatafialed"], UIStatus.ProgramError);
                    await Delay(10000);
                    continue;
                }
                foreach (var info in res)
                {
                    var stock = stocks.FirstOrDefault(x => x.Code.Trim().Remove(0, 2) == info.StockCode);
                    if (stock == null)
                    {
                        continue;
                    }
                    var text = StockInfoHandle(ref stock, info);
                    if (codeDatas.ContainsKey(stock.Code))
                    {
                        codeDatas[stock.Code] = text;
                    }
                    else
                    {
                        codeDatas.Add(stock.Code, text);
                    }
                }
                var list = codeDatas.Select(x => x.Value);
                UpdateUI(string.Join(Environment.NewLine, list));
            }

            await Delay();// 请求间隔最低 2s
        }
    }

    private async Task Delay(int ms = 2000)
    {
        await Task.Delay(_conf.Interval < 2 ? ms : _conf.Interval * 1000);
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
            var response = await _client.GetAsync(request);
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
            var response = await _client.GetAsync(request);
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
                    _conf.DarkMode = !_conf.DarkMode;
                    menu_dark.IsChecked = _conf.DarkMode;
                    InitColor();
                    break;
                case "menu_topmost":
                    _conf.Topmost = !_conf.Topmost;
                    menu_topmost.IsChecked = _conf.Topmost;
                    Topmost = _conf.Topmost;
                    break;
                case "menu_conf":
                    var cw = new ConfigWindow
                    {
                        Owner = this,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    cw.ShowDialog();
                    if ((bool)cw.DialogResult)
                    {
                        Utils.SaveConfig(_conf);
                    }
                    break;
                case "menu_conf_file":
                    var fullPath = Path.Combine(Utils.UserDataPath, "config.json");
                    Process.Start(fullPath);
                    break;
                case "menu_show_in_taskbar":
                    _conf.ShowInTaskbar = !_conf.ShowInTaskbar;
                    menu_show_in_taskbar.IsChecked = _conf.ShowInTaskbar;
                    ShowInTaskbar = _conf.ShowInTaskbar;
                    break;
                case "menu_data_roll":
                    _conf.DataRoll = !_conf.DataRoll;
                    menu_data_roll.IsChecked = _conf.DataRoll;
                    break;
                case "menu_ver":
                default:
                    MessageBox.Show("nothing happened~");
                    break;
            }
        }
    }
}
