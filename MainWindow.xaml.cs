using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

        static int codeIndex = 0;

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
            UpdateColor();
        }

        private void InitLang()
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            menu_ver.Header = $"{_i18n[_conf.Lang][menu_ver.Name]} {fvi.ProductVersion}";
            menu_exit.Header = $"{_i18n[_conf.Lang][menu_exit.Name]}";
            menu_dark.Header = $"{_i18n[_conf.Lang][menu_dark.Name]}";
            menu_topmost.Header = $"{_i18n[_conf.Lang][menu_topmost.Name]}";
            menu_conf.Header = $"{_i18n[_conf.Lang][menu_conf.Name]}";
        }
        private async void DoWork(object sender, EventArgs e)
        {
            while (!worker.IsBusy)
            {
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

                bool isHighlight = false;
                if (stock.BuyPrice > 0 && stock.BuyPrice <= res.CurrentPrice)
                {
                    isHighlight = true;
                    Logger.Info($"{stock.NickName} have a goodnews!");
                }

                UpdateUI(() =>
                {
                    lb.Content = $"{(stock.NickName.IsNullOrWhiteSpace() ? res.StockName : stock.NickName)}" +
                    $" {res.CurrentPrice:f2} " +
                    (stock.BuyPrice > 0 ? stock.BuyPrice.ToString("f2") : string.Empty);
                    UpdateColor(isHighlight);
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

        private async Task Delay(int ms = 2000)
        {
            await Task.Delay(_conf.Interval < 2 ? ms : _conf.Interval * 1000);
        }

        public static Brush color_bg;
        public static Brush color_fg;

        private void UpdateColor(bool isHighlight = false)
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
                lb.Foreground = new SolidColorBrush(Colors.Yellow);
                border.Background = new SolidColorBrush(Color.FromRgb(234, 85, 20));
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
                    case "menu_ver":
                    default:
                        MessageBox.Show("nothing happened~");
                        break;
                }
            }
        }
    }
}
