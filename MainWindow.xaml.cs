using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RestSharp;

#pragma warning disable IDE0044

namespace iNeedMyMoneyBack
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private RestClient _client;
        public static Config _conf = new Config();
        public static Dictionary<string, Dictionary<string, string>> _i18n;

        private static int codeIndex = 0;
        private static Dictionary<int, string> codeDatas = new Dictionary<int, string>();

        readonly BackgroundWorker worker = new BackgroundWorker()
        {
            WorkerSupportsCancellation = true,
        };

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
                _conf.Stocks.Add(new StockConfig("sh000001", "大A", 3200));
                _conf.Stocks.Add(new StockConfig("sz399001"));
            }

            InitUI();
            InitLang();

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
            worker.DoWork += (sender, e) => DoWork(sender, e);
        }

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
            UpdateColor();
        }

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

        private void SetMenuItemHeader(MenuItem menuItem)
        {
            if (_i18n.ContainsKey(_conf.Lang) && _i18n[_conf.Lang].ContainsKey(menuItem.Name))
            {
                menuItem.Header = _i18n[_conf.Lang][menuItem.Name];
            }
        }

        private async void DoWork(object sender, EventArgs e)
        {
            while (!worker.IsBusy)
            {
                if (!Utils.IsTradingTime())
                {
                    UpdateUI(() =>
                    {
                        lb.Content = $"Non-trading";
                    });
                    await Task.Delay(30000);
                    continue;
                }

                if (codeIndex > _conf.Stocks.Count - 1)
                {
                    codeIndex = 0;
                    await Delay();
                    continue;
                }
                var stock = _conf.Stocks.ElementAt(codeIndex);
                StockInfo res = await Request(stock);
                if (res == null)
                {
                    await Delay();
                    continue;
                }
                if (stock.Name.IsNullOrWhiteSpace())
                {
                    stock.Name = res.StockName;
                }
                UpdateUI(() =>
                {
                    var rr = $"{(stock.NickName.IsNullOrWhiteSpace() ? res.StockName : stock.NickName)}" +
                    $" {res.CurrentPrice:f3} {res.PriceChangePercent}%";
                    if (stock.BuyPrice > 0)
                    {
                        rr += (stock.BuyPrice - res.CurrentPrice).ToString(" -0.000");
                    }

                    if (_conf.DataRoll)
                    {
                        var sb = new StringBuilder();
                        sb.Insert(0, rr);
                        var lineCount = sb.ToString().Count(x => x.Equals('\n'));
                        if (lineCount >= _conf.Stocks.Count)
                        {
                            RemoveLastLine(sb);
                        }
                        lb.Content = sb.ToString();
                    }
                    else
                    {
                        if (codeDatas.ContainsKey(codeIndex))
                        {
                            codeDatas[codeIndex] = rr;
                        }
                        else
                        {
                            codeDatas.Add(codeIndex, rr);
                        }
                        var list = codeDatas.OrderBy(x => x.Key).Select(x => x.Value);
                        lb.Content = string.Join(Environment.NewLine, list);
                    }

                    //(stock.BuyPrice > 0 ? stock.BuyPrice.ToString("f2") : string.Empty);
                    if (res.CurrentPrice >= res.PriceLimitUp)
                    {
                        UpdateColor(true);
                    }
                    else if (res.CurrentPrice <= res.PriceLimitDown)
                    {
                        UpdateColor(true, true);
                    }
                });

                codeIndex++;
                if (_conf.Stocks.Count < codeIndex + 1)
                {
                    codeIndex = 0;
                }
                // 请求间隔最低 2s
                await Delay();
            }
        }

        static void RemoveLastLine(StringBuilder sb)
        {
            int lastIndex = sb.ToString().LastIndexOf('\n');
            if (lastIndex != -1)
            {
                sb.Remove(lastIndex, sb.Length - lastIndex);
            }
            else if (sb.Length > 0) // 如果没有换行符，整个字符串就是最后一行
            {
                sb.Clear();
            }
        }

        private async Task Delay(int ms = 2000)
        {
            await Task.Delay(_conf.Interval < 2 ? ms : _conf.Interval * 1000);
        }

        public static Brush color_bg;
        public static Brush color_fg;

        private void UpdateColor(bool isHighlight = false, bool isLimitStop = false)
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

            if (isHighlight)
            {
                if (isLimitStop)
                {
                    lb.Foreground = color_fg;
                    border.Background = new SolidColorBrush(Colors.DarkGreen);
                }
                else
                {
                    lb.Foreground = new SolidColorBrush(Colors.Yellow);
                    border.Background = new SolidColorBrush(Color.FromRgb(234, 20, 62));
                }
            }
        }

        private void UpdateUI(Action action)
        {
            Application.Current.Dispatcher.Invoke(action);
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
                        UpdateColor();
                        break;
                    case "menu_topmost":
                        _conf.Topmost = !_conf.Topmost;
                        menu_topmost.IsChecked = _conf.Topmost;
                        Topmost = _conf.Topmost;
                        break;
                    case "menu_conf":
                        ConfigWindow cw = new ConfigWindow
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
}
