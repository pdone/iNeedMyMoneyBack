using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using static iNeedMyMoneyBack.Utils;

namespace iNeedMyMoneyBack;

public partial class ConfigWindow : Window
{
    public static readonly SolidColorBrush LightGray = new(Color.FromRgb(222, 222, 222));
    public static readonly SolidColorBrush DarkGray = new(Color.FromRgb(66, 66, 66));
    private static readonly SolidColorBrush LightCardBg = new(Color.FromRgb(245, 245, 245));
    private static readonly SolidColorBrush DarkCardBg = new(Color.FromRgb(50, 50, 50));
    private static readonly SolidColorBrush LightCardBorder = new(Color.FromRgb(200, 200, 200));
    private static readonly SolidColorBrush DarkCardBorder = new(Color.FromRgb(80, 80, 80));
    private static readonly SolidColorBrush LightInputBg = new(Color.FromRgb(255, 255, 255));
    private static readonly SolidColorBrush DarkInputBg = new(Color.FromRgb(40, 40, 40));
    private static readonly SolidColorBrush HoverBg = new(Color.FromArgb(40, 128, 128, 128));

    private readonly Config _conf;
    private readonly StockConfigArray _stocks;
    private readonly MainWindow _mainWindow;
    private readonly string[] SupportExchange = ["sh", "sz", "bj", "us", "hk"];
    private string _originalApi = "";

    public ConfigWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _conf = MainWindow.g_conf;
        _stocks = MainWindow.g_conf_stocks;
        InitUI();
    }

    private void InitUI()
    {
        Width = _conf.ConfigWindowWidth;
        Height = _conf.ConfigWindowHeight;
        Opacity = _conf.Opacity;
        SizeChanged += (_, __) =>
        {
            _conf.ConfigWindowWidth = Width;
            _conf.ConfigWindowHeight = Height;
        };
        MouseDown += (_, __) => DragWindow(this);

        // 先初始化颜色，因为卡片创建时需要使用颜色资源
        InitColor();
        InitBorderThickess(_conf.HideBorder);
        Resources["MainOpacity"] = _conf.Opacity;

        // 初始化股票卡片列表
        RefreshStockCards();

        // 初始化字段控件
        foreach (var kvp in _conf.FieldControls)
        {
            var cbx = CreateCheckBox(kvp.Key, kvp.Value);
            FieldControls.Children.Add(cbx);
        }

        // 初始化指数和扩展控件（按市场分组）
        foreach (var item in _conf.ExtendControls)
        {
            var market = GetIndexMarket(item.Key);
            if (market != "")
            {
                var target = market == "hk" ? StockIndexControlsHK : market == "us" ? StockIndexControlsUS : StockIndexControlsA;
                var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
                var cbx = CreateCheckBox(item.Key, item.Visable);
                target.Children.Add(cbxNewline);
                target.Children.Add(cbx);
            }
            else
            {
                var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
                var cbx = CreateCheckBox(item.Key, item.Visable);
                ExtendControls.Children.Add(cbxNewline);
                ExtendControls.Children.Add(cbx);
            }
        }

        // 初始化更多设置Tab页
        // 初始化字体大小ComboBox
        var fontSizes = new[] { 12, 14, 16, 18, 20, 22, 24 };
        foreach (var size in fontSizes)
        {
            cmbFontSize.Items.Add(new ComboBoxItem { Content = size.ToString(), Tag = (double)size });
        }
        cmbFontSize.SelectedIndex = Array.IndexOf(fontSizes, (int)_conf.FontSizeMain);
        if (cmbFontSize.SelectedIndex < 0) cmbFontSize.SelectedIndex = 2; // default 16

        // 初始化列间距ComboBox
        var columnSpacings = new[] { 2, 4, 6, 8, 10, 12 };
        foreach (var spacing in columnSpacings)
        {
            cmbColumnSpacing.Items.Add(new ComboBoxItem { Content = spacing.ToString(), Tag = (double)spacing });
        }
        cmbColumnSpacing.SelectedIndex = Array.IndexOf(columnSpacings, (int)_conf.GridColumnSpacing);
        if (cmbColumnSpacing.SelectedIndex < 0) cmbColumnSpacing.SelectedIndex = 1; // default 4

        // 初始化查询间隔ComboBox
        var intervals = new[] { 2, 5, 10, 30, 60, 300, 600, 1800 };
        foreach (var interval in intervals)
        {
            cmbInterval.Items.Add(new ComboBoxItem { Content = $"{interval}秒", Tag = interval });
        }
        cmbInterval.SelectedIndex = Array.IndexOf(intervals, _conf.Interval);
        if (cmbInterval.SelectedIndex < 0) cmbInterval.SelectedIndex = 1; // default 5

        _originalApi = _conf.Api;
        txtApi.Text = _conf.Api;

        // 初始化双击跳转ComboBox
        cmbDoubleClickAction.Items.Add(new ComboBoxItem { Content = "雪球", Tag = "xueqiu" });
        cmbDoubleClickAction.Items.Add(new ComboBoxItem { Content = "同花顺", Tag = "tonghuashun" });
        cmbDoubleClickAction.SelectedIndex = _conf.DoubleClickAction == "tonghuashun" ? 1 : 0;

        // 初始化启用美股/港股CheckBox（先取消事件订阅，避免初始化时触发）
        chk_enable_us.Checked -= Chk_EnableMarket_Checked;
        chk_enable_hk.Checked -= Chk_EnableMarket_Checked;
        chk_enable_us.IsChecked = _conf.EnableUS;
        chk_enable_hk.IsChecked = _conf.EnableHK;
        chk_enable_us.Checked += Chk_EnableMarket_Checked;
        chk_enable_hk.Checked += Chk_EnableMarket_Checked;
        UpdateMarketCheckBoxText();
        UpdateIndexControlsVisibility();

        InitLang();
    }

    private void RefreshStockCards()
    {
        stockListPanel.Children.Clear();
        for (var i = 0; i < _stocks.Count; i++)
        {
            var card = CreateStockCard(_stocks[i], i);
            stockListPanel.Children.Add(card);
        }
    }

    private Border CreateStockCard(StockConfig stock, int index)
    {
        var card = new Border
        {
            Margin = new Thickness(0, 0, 3, 5),
            Padding = new Thickness(8),
            Background = Resources["CardBackground"] as SolidColorBrush,
            BorderBrush = Resources["CardBorder"] as SolidColorBrush,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(5),
            Tag = stock
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // 左侧信息区域
        var infoPanel = new StackPanel();

        // 第一行：代码和名称
        var headerPanel = new StackPanel { Orientation = Orientation.Horizontal };
        var codeText = new TextBlock
        {
            Text = stock.Code,
            FontWeight = FontWeights.Bold,
            Foreground = Resources["TextColor"] as SolidColorBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        var nameText = new TextBlock
        {
            Text = $"{stock.Name} {(stock.NickName.IsNullOrWhiteSpace() ? "" : stock.NickName)}",
            Foreground = Resources["TextColor"] as SolidColorBrush,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7
        };
        headerPanel.Children.Add(codeText);
        headerPanel.Children.Add(nameText);
        infoPanel.Children.Add(headerPanel);

        // 第二行：买入信息
        if (stock.BuyPrice > 0 || stock.BuyCount > 0)
        {
            var buyInfo = new TextBlock
            {
                Foreground = Resources["TextColor"] as SolidColorBrush,
                FontSize = 11,
                Opacity = 0.6,
                Margin = new Thickness(0, 3, 0, 0)
            };
            var parts = new List<string>();
            if (stock.BuyPrice > 0)
            {
                parts.Add($"{i18n[_conf.Lang]["col_buyprice"]}: {stock.BuyPrice:F2}");
            }

            if (stock.BuyCount > 0)
            {
                parts.Add($"{i18n[_conf.Lang]["col_buycount"]}: {stock.BuyCount}");
            }

            buyInfo.Text = string.Join("  ", parts);
            infoPanel.Children.Add(buyInfo);
        }

        // 第三行：提醒设置
        if (stock.ReminderPriceUp > 0 || stock.ReminderPriceDown > 0)
        {
            var reminderInfo = new TextBlock
            {
                Foreground = Resources["TextColor"] as SolidColorBrush,
                FontSize = 11,
                Opacity = 0.6,
                Margin = new Thickness(0, 2, 0, 0)
            };
            var parts = new List<string>();
            if (stock.ReminderPriceUp > 0)
            {
                parts.Add($"↑{stock.ReminderPriceUp:F2}");
            }

            if (stock.ReminderPriceDown > 0)
            {
                parts.Add($"↓{stock.ReminderPriceDown:F2}");
            }

            parts.Add($"{i18n[_conf.Lang]["col_ReminderTimes"]}: {stock.ReminderTimes}");
            reminderInfo.Text = string.Join("  ", parts);
            infoPanel.Children.Add(reminderInfo);
        }

        Grid.SetColumn(infoPanel, 0);
        grid.Children.Add(infoPanel);

        // 右侧按钮区域
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };

        var moveUpBtn = new Button
        {
            Content = Symbols.ArrowUp,
            FontSize = 11,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(2, 0, 2, 0),
            Tag = stock,
            Visibility = index == 0 ? Visibility.Collapsed : Visibility.Visible
        };
        moveUpBtn.Click += Btn_Move_Stock_Up_Click;

        var moveDownBtn = new Button
        {
            Content = Symbols.ArrowDown,
            FontSize = 11,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(2, 0, 2, 0),
            Tag = stock,
            Visibility = index == _stocks.Count - 1 ? Visibility.Collapsed : Visibility.Visible
        };
        moveDownBtn.Click += Btn_Move_Stock_Down_Click;

        var editBtn = new Button
        {
            Content = i18n[_conf.Lang]["btn_edit"],
            FontSize = 11,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(2, 0, 2, 0),
            Tag = stock
        };
        editBtn.Click += Btn_Edit_Stock_Click;

        var deleteBtn = new Button
        {
            Content = i18n[_conf.Lang]["btn_delete"],
            FontSize = 11,
            Padding = new Thickness(6, 2, 6, 2),
            Margin = new Thickness(2, 0, 0, 0),
            Tag = stock
        };
        deleteBtn.Click += Btn_Delete_Stock_Click;

        buttonPanel.Children.Add(moveUpBtn);
        buttonPanel.Children.Add(moveDownBtn);
        buttonPanel.Children.Add(editBtn);
        buttonPanel.Children.Add(deleteBtn);

        Grid.SetColumn(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        card.Child = grid;
        card.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2)
            {
                ShowEditDialog(stock);
            }
        };
        return card;
    }

    private void Btn_Add_Stock_Click(object sender, RoutedEventArgs e)
    {
        AddStock();
    }

    private void TxtStockCode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddStock();
        }
    }

    private async void AddStock()
    {
        var code = txtStockCode.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        code = code.ToLower();

        if (StockConfigArray.ImportantIndexs.Any(x => x.Code == code))
        {
            ShowMessage(i18n[_conf.Lang]["msg_index_not_support_add"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var startWith = code.Length >= 2 ? code.Substring(0, 2) : "";
        if (!SupportExchange.Contains(startWith))
        {
            ShowMessage(string.Format(i18n[_conf.Lang]["msg_exchange_not_support"], string.Join("、", SupportExchange)), i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        // 检查市场是否启用
        if (startWith == "us" && !_conf.EnableUS)
        {
            ShowMessage(string.Format(i18n[_conf.Lang]["msg_market_not_enabled"], i18n[_conf.Lang]["chk_enable_us"]), i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }
        if (startWith == "hk" && !_conf.EnableHK)
        {
            ShowMessage(string.Format(i18n[_conf.Lang]["msg_market_not_enabled"], i18n[_conf.Lang]["chk_enable_hk"]), i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        if (_stocks.Any(x => x.Code == code))
        {
            ShowMessage(i18n[_conf.Lang]["msg_stock_exists"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var stockInfo = await _mainWindow.VerifyStockCode(code);
        if (stockInfo == null)
        {
            ShowMessage(i18n[_conf.Lang]["msg_stock_not_found"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var newStock = new StockConfig(code)
        {
            Name = stockInfo.StockName
        };
        _stocks.Add(newStock);
        RefreshStockCards();
        txtStockCode.Text = "";
        _mainWindow.MarkGridStructureDirty();
        DataUpdate();
    }

    private void Btn_Edit_Stock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StockConfig stock)
        {
            ShowEditDialog(stock);
        }
    }

    private void ShowEditDialog(StockConfig stock)
    {
        var bgColor = Resources["CardBackground"] as SolidColorBrush;
        var fgColor = Resources["TextColor"] as SolidColorBrush;
        var inputBg = Resources["InputBackground"] as SolidColorBrush;
        var borderColor = Resources["CardBorder"] as SolidColorBrush;

        var dialog = new Window
        {
            Title = $"{i18n[_conf.Lang]["btn_edit"]} - {stock.Code}",
            Width = 360,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = bgColor,
            Foreground = fgColor,
            FontFamily = FontFamily,
            ResizeMode = ResizeMode.NoResize,
            Opacity = Opacity
        };

        // 继承配置界面的资源以适配深浅模式
        dialog.Resources["TextColor"] = fgColor;
        dialog.Resources["CardBorder"] = borderColor;
        dialog.Resources["HoverBackground"] = Resources["HoverBackground"];

        var mainPanel = new StackPanel { Margin = new Thickness(15) };

        // 输入框样式（圆角）
        var txtTemplate = new ControlTemplate(typeof(TextBox));
        var txtBorder = new FrameworkElementFactory(typeof(Border), "border");
        txtBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(TextBox.BackgroundProperty));
        txtBorder.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(TextBox.BorderBrushProperty));
        txtBorder.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(TextBox.BorderThicknessProperty));
        txtBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        txtBorder.SetValue(Border.PaddingProperty, new TemplateBindingExtension(TextBox.PaddingProperty));
        txtBorder.SetValue(Border.SnapsToDevicePixelsProperty, true);
        var scrollViewer = new FrameworkElementFactory(typeof(ScrollViewer), "PART_ContentHost");
        scrollViewer.SetValue(ScrollViewer.MarginProperty, new Thickness(0));
        txtBorder.AppendChild(scrollViewer);
        txtTemplate.VisualTree = txtBorder;

        // 创建输入行（标签 + 输入框在同一行）
        TextBox CreateInputRow(string label, string value, string tag)
        {
            var row = new Grid { Margin = new Thickness(0, 6, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = fgColor,
                Padding = new Thickness(0, 0, 8, 0)
            });
            var txt = new TextBox
            {
                Text = value,
                Tag = tag,
                Background = inputBg,
                Foreground = fgColor,
                BorderBrush = borderColor,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(4, 2, 4, 2),
                Template = txtTemplate
            };
            Grid.SetColumn(txt, 1);
            row.Children.Add(txt);
            mainPanel.Children.Add(row);
            return txt;
        }

        var txtNick = CreateInputRow(i18n[_conf.Lang]["col_nickname"], stock.NickName ?? "", "NickName");
        var txtBuyPrice = CreateInputRow(i18n[_conf.Lang]["col_buyprice"], stock.BuyPrice.ToString("F2"), "BuyPrice");
        var txtBuyCount = CreateInputRow(i18n[_conf.Lang]["col_buycount"], stock.BuyCount.ToString(), "BuyCount");
        var txtReminderUp = CreateInputRow(i18n[_conf.Lang]["col_ReminderPriceUp"], stock.ReminderPriceUp.ToString("F2"), "ReminderPriceUp");
        var txtReminderDown = CreateInputRow(i18n[_conf.Lang]["col_ReminderPriceDown"], stock.ReminderPriceDown.ToString("F2"), "ReminderPriceDown");

        // 按钮样式（与配置界面一致）
        var btnTemplate = new ControlTemplate(typeof(Button));
        var templateBorder = new FrameworkElementFactory(typeof(Border), "border");
        templateBorder.SetValue(Border.PaddingProperty, new Thickness(6, 3, 6, 3));
        templateBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        templateBorder.SetResourceReference(Border.BorderBrushProperty, "CardBorder");
        templateBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        templateBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        templateBorder.AppendChild(contentPresenter);
        btnTemplate.VisualTree = templateBorder;
        var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("HoverBackground"), "border"));
        btnTemplate.Triggers.Add(trigger);

        // 按钮
        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 15, 0, 0)
        };
        var okBtn = new Button { Content = i18n[_conf.Lang]["btn_ok"], Width = 60, Margin = new Thickness(0, 0, 10, 0), Background = Brushes.Transparent, Foreground = fgColor, Template = btnTemplate };
        var cancelBtn = new Button { Content = i18n[_conf.Lang]["btn_cancel"], Width = 60, Background = Brushes.Transparent, Foreground = fgColor, Template = btnTemplate };
        okBtn.Click += (_, __) =>
        {
            stock.NickName = txtNick.Text;
            double.TryParse(txtBuyPrice.Text, out var bp);
            stock.BuyPrice = bp;
            int.TryParse(txtBuyCount.Text, out var bc);
            stock.BuyCount = bc;
            double.TryParse(txtReminderUp.Text, out var ru);
            stock.ReminderPriceUp = ru;
            double.TryParse(txtReminderDown.Text, out var rd);
            stock.ReminderPriceDown = rd;
            if (ru > 0 || rd > 0)
            {
                stock.ReminderTimes = 1;
            }
            dialog.DialogResult = true;
            dialog.Close();
        };
        cancelBtn.Click += (_, __) =>
        {
            dialog.DialogResult = false;
            dialog.Close();
        };
        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                okBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
            else if (e.Key == Key.Escape)
            {
                cancelBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            }
        };
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(btnPanel);

        dialog.Content = mainPanel;

        // 设置标题栏深色模式
        dialog.SourceInitialized += (_, __) =>
        {
            SetWindowDarkMode(new WindowInteropHelper(dialog).Handle, _conf.DarkMode);
        };

        if (dialog.ShowDialog() == true)
        {
            RefreshStockCards();
            DataUpdate();
        }
    }

    private void Btn_Delete_Stock_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StockConfig stock)
        {
            var result = ShowConfirm(
                string.Format(i18n[_conf.Lang]["msg_confirm_delete"], stock.DisplayName),
                i18n[_conf.Lang]["ui_title_confirm"]);

            if (result)
            {
                _stocks.Remove(stock);
                RefreshStockCards();
                _mainWindow.MarkGridStructureDirty();
                DataUpdate();
            }
        }
    }

    private void Btn_Move_Stock_Up_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StockConfig stock)
        {
            var index = _stocks.IndexOf(stock);
            if (index > 0)
            {
                _stocks.RemoveAt(index);
                _stocks.Insert(index - 1, stock);
                RefreshStockCards();
                _mainWindow.MarkGridStructureDirty();
                DataUpdate();
            }
        }
    }

    private void Btn_Move_Stock_Down_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is StockConfig stock)
        {
            var index = _stocks.IndexOf(stock);
            if (index < _stocks.Count - 1)
            {
                _stocks.RemoveAt(index);
                _stocks.Insert(index + 1, stock);
                RefreshStockCards();
                _mainWindow.MarkGridStructureDirty();
                DataUpdate();
            }
        }
    }

    private void RefreshFieldControls()
    {
        FieldControls.Children.Clear();
        foreach (var kvp in _conf.FieldControls)
        {
            var cbx = CreateCheckBox(kvp.Key, kvp.Value);
            FieldControls.Children.Add(cbx);
        }
        InitLang();
    }

    private void RefreshExtendControls()
    {
        StockIndexControlsA.Children.Clear();
        StockIndexControlsHK.Children.Clear();
        StockIndexControlsUS.Children.Clear();
        ExtendControls.Children.Clear();
        foreach (var item in _conf.ExtendControls)
        {
            var market = GetIndexMarket(item.Key);
            if (market != "")
            {
                var target = market == "hk" ? StockIndexControlsHK : market == "us" ? StockIndexControlsUS : StockIndexControlsA;
                var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
                var cbx = CreateCheckBox(item.Key, item.Visable);
                target.Children.Add(cbxNewline);
                target.Children.Add(cbx);
            }
            else
            {
                var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
                var cbx = CreateCheckBox(item.Key, item.Visable);
                ExtendControls.Children.Add(cbxNewline);
                ExtendControls.Children.Add(cbx);
            }
        }
        InitLang();
    }

    private void DataUpdate()
    {
        // 根据配置过滤指数和个股
        var filteredIndexs = StockConfigArray.GetFilteredImportantIndexs(_conf.EnableUS, _conf.EnableHK);
        var filteredStocks = new StockConfigArray();
        foreach (var stock in _stocks)
        {
            if (stock.Code.StartsWith("us") && !_conf.EnableUS) continue;
            if (stock.Code.StartsWith("hk") && !_conf.EnableHK) continue;
            filteredStocks.Add(stock);
        }
        MainWindow.g_conf_stocks_with_index = [.. filteredStocks.Union(filteredIndexs)];
        _mainWindow.DataUpdate(false);
    }

    public void InitColor()
    {
        var isDark = _conf.DarkMode;
        Resources["CardBackground"] = isDark ? DarkCardBg : LightCardBg;
        Resources["CardBorder"] = isDark ? DarkCardBorder : LightCardBorder;
        Resources["InputBackground"] = isDark ? DarkInputBg : LightInputBg;
        Resources["HoverBackground"] = HoverBg;
        Resources["CheckedColor"] = isDark ? DarkGray : new SolidColorBrush(Color.FromRgb(187, 187, 187));
        Resources["TextColor"] = isDark ? LightGray : SystemColors.ControlTextBrush;
        Resources["ScrollThumb"] = isDark ? new SolidColorBrush(Color.FromRgb(100, 100, 100)) : new SolidColorBrush(Color.FromRgb(180, 180, 180));
        Resources["ScrollTrack"] = isDark ? new SolidColorBrush(Color.FromRgb(40, 40, 40)) : new SolidColorBrush(Color.FromRgb(240, 240, 240));

        border.Background = MainWindow.color_bg;

        // 刷新股票卡片以应用新颜色
        RefreshStockCards();
    }

    public void InitBorderThickess(bool hide)
    {
        Resources["BorderThickness"] = new Thickness(hide ? 0 : 1);
    }

    public void InitLang()
    {
        Title = i18n[_conf.Lang]["ui_title_config"];
        btn_close.Content = i18n[_conf.Lang]["btn_close"];
        btn_add_stock.Content = i18n[_conf.Lang]["btn_add_stock"];
        txtStockCode.ToolTip = i18n[_conf.Lang]["txt_stock_code_tip"];

        tab_stocks.Header = i18n[_conf.Lang]["tab_stocks"];
        tab_fields.Header = i18n[_conf.Lang]["tab_fields"];
        tab_more_settings.Header = i18n[_conf.Lang]["tab_more_settings"];
        lbl_basic_fields.Text = i18n[_conf.Lang]["lbl_basic_fields"];
        lbl_index_fields.Text = i18n[_conf.Lang]["lbl_index_fields"];
        lbl_index_a.Text = i18n[_conf.Lang]["lbl_index_a"];
        lbl_index_hk.Text = i18n[_conf.Lang]["lbl_index_hk"];
        lbl_index_us.Text = i18n[_conf.Lang]["lbl_index_us"];
        lbl_extend_fields.Text = i18n[_conf.Lang]["lbl_extend_fields"];

        // 更多设置Tab页
        lbl_font_size.Text = i18n[_conf.Lang]["lbl_font_size"];
        lbl_interval.Text = i18n[_conf.Lang]["lbl_interval"];
        lbl_api.Text = i18n[_conf.Lang]["lbl_api"];
        lbl_column_spacing.Text = i18n[_conf.Lang]["lbl_column_spacing"];
        txtResetLabel.Text = i18n[_conf.Lang]["btn_reset_default"];
        lbl_double_click_action.Text = i18n[_conf.Lang]["ui_double_click_action"];
        UpdateMarketCheckBoxText();
        if (cmbDoubleClickAction.Items.Count >= 2)
        {
            ((ComboBoxItem)cmbDoubleClickAction.Items[0]).Content = i18n[_conf.Lang]["ui_double_click_xueqiu"];
            ((ComboBoxItem)cmbDoubleClickAction.Items[1]).Content = i18n[_conf.Lang]["ui_double_click_tonghuashun"];
        }

        // 更新查询间隔ComboBox的显示文本
        if (cmbInterval.Items.Count >= 8)
        {
            var intervals = new[] { 2, 5, 10, 30, 60, 300, 600, 1800 };
            for (int i = 0; i < intervals.Length; i++)
            {
                ((ComboBoxItem)cmbInterval.Items[i]).Content = $"{intervals[i]}{i18n[_conf.Lang]["interval_unit"]}";
            }
        }

        foreach (var child in FieldControls.Children)
        {
            if (child is CheckBox cbx)
            {
                cbx.Content = cbx.Name.EndsWith(NewlineSuffix)
                    ? i18n[_conf.Lang][NewlineSuffix]
                    : (i18n[_conf.Lang].ContainsKey(cbx.Name) ? i18n[_conf.Lang][cbx.Name] : cbx.Name);
            }
        }

        foreach (var child in StockIndexControlsA.Children)
        {
            if (child is CheckBox cbx)
            {
                cbx.Content = cbx.Name.EndsWith(NewlineSuffix)
                    ? i18n[_conf.Lang][NewlineSuffix]
                    : (i18n[_conf.Lang].ContainsKey(cbx.Name) ? i18n[_conf.Lang][cbx.Name] : cbx.Name);
            }
        }

        foreach (var child in StockIndexControlsHK.Children)
        {
            if (child is CheckBox cbx)
            {
                cbx.Content = cbx.Name.EndsWith(NewlineSuffix)
                    ? i18n[_conf.Lang][NewlineSuffix]
                    : (i18n[_conf.Lang].ContainsKey(cbx.Name) ? i18n[_conf.Lang][cbx.Name] : cbx.Name);
            }
        }

        foreach (var child in StockIndexControlsUS.Children)
        {
            if (child is CheckBox cbx)
            {
                cbx.Content = cbx.Name.EndsWith(NewlineSuffix)
                    ? i18n[_conf.Lang][NewlineSuffix]
                    : (i18n[_conf.Lang].ContainsKey(cbx.Name) ? i18n[_conf.Lang][cbx.Name] : cbx.Name);
            }
        }

        foreach (var child in ExtendControls.Children)
        {
            if (child is CheckBox cbx)
            {
                cbx.Content = cbx.Name.EndsWith(NewlineSuffix)
                    ? i18n[_conf.Lang][NewlineSuffix]
                    : (i18n[_conf.Lang].ContainsKey(cbx.Name) ? i18n[_conf.Lang][cbx.Name] : cbx.Name);
            }
        }

        RefreshStockCards();
    }

    private CheckBox CreateCheckBox(string name, bool isChecked)
    {
        var checkBox = new CheckBox
        {
            Name = name,
            IsChecked = isChecked
        };
        checkBox.Checked += CheckBox_Checked;
        checkBox.Unchecked += CheckBox_Checked;
        return checkBox;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            if (_conf.FieldControls.ContainsKey(checkBox.Name))
            {
                _conf.FieldControls[checkBox.Name] = (bool)checkBox.IsChecked;
                _mainWindow.MarkGridStructureDirty();
            }
            else if (_conf.ExtendControls.Find(x => x.Key == checkBox.Name) is ExtendControlObj ec)
            {
                ec.Visable = (bool)checkBox.IsChecked;
                _mainWindow.MarkGridStructureDirty();
            }
            else if (_conf.ExtendControls.Find(x => x.GetNewLineKey() == checkBox.Name) is ExtendControlObj ec2)
            {
                ec2.NewLine = (bool)checkBox.IsChecked;
                _mainWindow.MarkGridStructureDirty();
            }
            _mainWindow.DataUpdate();
        }
    }

    private static string GetIndexMarket(string key)
    {
        if (!key.StartsWith(StockIndexPrefix))
        {
            return "";
        }

        var code = key.Substring(StockIndexPrefix.Length);
        if (code.StartsWith("hk"))
        {
            return "hk";
        }

        if (code.StartsWith("us"))
        {
            return "us";
        }

        return "a";
    }

    private void Btn_Close_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Hidden;
    }

    private void Btn_Reset_Api_Click(object sender, RoutedEventArgs e)
    {
        cmbFontSize.SelectedIndex = 2; // 16
        cmbInterval.SelectedIndex = 1; // 5秒
        cmbColumnSpacing.SelectedIndex = 1; // 4
        cmbDoubleClickAction.SelectedIndex = 0;// 雪球
        txtApi.Text = "https://qt.gtimg.cn";
    }

    private async void Btn_Apply_Click(object sender, RoutedEventArgs e)
    {
        await ApplyApiAsync();
    }

    private async Task ApplyApiAsync()
    {
        // 验证API
        var api = txtApi.Text?.Trim();
        if (string.IsNullOrWhiteSpace(api))
        {
            ShowMessage(i18n[_conf.Lang]["msg_api_invalid"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var isValid = await _mainWindow.VerifyApi(api);
        if (!isValid)
        {
            ShowMessage(i18n[_conf.Lang]["msg_api_invalid"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        // 应用API设置
        _conf.Api = api;
        _originalApi = api;
        _conf.Save();
        _mainWindow.UpdateRestClient(api);
        _mainWindow.MarkGridStructureDirty();
        _mainWindow.DataUpdate();

        // 显示"已应用"状态
        txtApiApply.Text = i18n[_conf.Lang]["btn_applied"];
        txtApiApply.Foreground = Brushes.Green;
        txtApiApply.Visibility = Visibility.Visible;

        // 2秒后移除
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        timer.Tick += (_, __) =>
        {
            txtApiApply.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }

    private void CmbDoubleClickAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbDoubleClickAction.SelectedItem is ComboBoxItem item)
        {
            _conf.DoubleClickAction = item.Tag.ToString();
            _conf.Save();
        }
    }

    private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbFontSize.SelectedItem is ComboBoxItem item && item.Tag is double fontSize)
        {
            _conf.FontSizeMain = fontSize;
            _conf.Save();
            _mainWindow.UpdateFontSize(fontSize);
            _mainWindow.MarkGridStructureDirty();
            _mainWindow.DataUpdate();
        }
    }

    private void CmbColumnSpacing_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbColumnSpacing.SelectedItem is ComboBoxItem item && item.Tag is double spacing)
        {
            _conf.GridColumnSpacing = spacing;
            _conf.Save();
            _mainWindow.MarkGridStructureDirty();
            _mainWindow.DataUpdate();
        }
    }

    private void CmbInterval_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cmbInterval.SelectedItem is ComboBoxItem item && item.Tag is int interval)
        {
            _conf.Interval = interval;
            _conf.Save();
        }
    }

    private void TxtApi_TextChanged(object sender, TextChangedEventArgs e)
    {
        var currentApi = txtApi.Text?.Trim();
        if (currentApi != _originalApi)
        {
            txtApiApply.Text = i18n[_conf.Lang]["btn_apply"];
            txtApiApply.Foreground = Resources["TextColor"] as SolidColorBrush;
            txtApiApply.Visibility = Visibility.Visible;
        }
        else
        {
            txtApiApply.Visibility = Visibility.Collapsed;
        }
    }

    private async void TxtApiApply_Click(object sender, MouseButtonEventArgs e)
    {
        await ApplyApiAsync();
    }

    private void ShowMessage(string message, string title)
    {
        var bgColor = Resources["CardBackground"] as SolidColorBrush;
        var fgColor = Resources["TextColor"] as SolidColorBrush;
        var borderColor = Resources["CardBorder"] as SolidColorBrush;

        var dialog = new Window
        {
            Title = title,
            Width = 320,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = bgColor,
            Foreground = fgColor,
            FontFamily = FontFamily,
            ResizeMode = ResizeMode.NoResize,
            Opacity = Opacity
        };

        dialog.Resources["TextColor"] = fgColor;
        dialog.Resources["CardBorder"] = borderColor;
        dialog.Resources["HoverBackground"] = Resources["HoverBackground"];

        var mainPanel = new StackPanel { Margin = new Thickness(20) };

        var txtBlock = new TextBlock
        {
            Text = message,
            Foreground = fgColor,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15)
        };
        mainPanel.Children.Add(txtBlock);

        var btnTemplate = new ControlTemplate(typeof(Button));
        var templateBorder = new FrameworkElementFactory(typeof(Border), "border");
        templateBorder.SetValue(Border.PaddingProperty, new Thickness(6, 3, 6, 3));
        templateBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        templateBorder.SetResourceReference(Border.BorderBrushProperty, "CardBorder");
        templateBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        templateBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        templateBorder.AppendChild(contentPresenter);
        btnTemplate.VisualTree = templateBorder;
        var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("HoverBackground"), "border"));
        btnTemplate.Triggers.Add(trigger);

        var okBtn = new Button
        {
            Content = i18n[_conf.Lang]["btn_ok"],
            Width = 80,
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = Brushes.Transparent,
            Foreground = fgColor,
            Template = btnTemplate
        };
        okBtn.Click += (_, __) => dialog.Close();
        mainPanel.Children.Add(okBtn);

        dialog.Content = mainPanel;
        dialog.KeyDown += (_, e) => { if (e.Key == Key.Enter || e.Key == Key.Escape) { dialog.Close(); } };

        dialog.SourceInitialized += (_, __) =>
        {
            SetWindowDarkMode(new WindowInteropHelper(dialog).Handle, _conf.DarkMode);
        };

        dialog.ShowDialog();
    }

    private bool ShowConfirm(string message, string title)
    {
        var bgColor = Resources["CardBackground"] as SolidColorBrush;
        var fgColor = Resources["TextColor"] as SolidColorBrush;
        var borderColor = Resources["CardBorder"] as SolidColorBrush;
        var result = false;

        var dialog = new Window
        {
            Title = title,
            Width = 340,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Background = bgColor,
            Foreground = fgColor,
            FontFamily = FontFamily,
            ResizeMode = ResizeMode.NoResize,
            Opacity = Opacity
        };

        dialog.Resources["TextColor"] = fgColor;
        dialog.Resources["CardBorder"] = borderColor;
        dialog.Resources["HoverBackground"] = Resources["HoverBackground"];

        var mainPanel = new StackPanel { Margin = new Thickness(20) };

        var txtBlock = new TextBlock
        {
            Text = message,
            Foreground = fgColor,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 15)
        };
        mainPanel.Children.Add(txtBlock);

        var btnTemplate = new ControlTemplate(typeof(Button));
        var templateBorder = new FrameworkElementFactory(typeof(Border), "border");
        templateBorder.SetValue(Border.PaddingProperty, new Thickness(6, 3, 6, 3));
        templateBorder.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
        templateBorder.SetResourceReference(Border.BorderBrushProperty, "CardBorder");
        templateBorder.SetValue(Border.BorderThicknessProperty, new Thickness(1));
        templateBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
        var contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
        contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        templateBorder.AppendChild(contentPresenter);
        btnTemplate.VisualTree = templateBorder;
        var trigger = new Trigger { Property = Button.IsMouseOverProperty, Value = true };
        trigger.Setters.Add(new Setter(Border.BackgroundProperty, new DynamicResourceExtension("HoverBackground"), "border"));
        btnTemplate.Triggers.Add(trigger);

        var btnPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        var okBtn = new Button
        {
            Content = i18n[_conf.Lang]["btn_ok"],
            Width = 80,
            Margin = new Thickness(0, 0, 10, 0),
            Background = Brushes.Transparent,
            Foreground = fgColor,
            Template = btnTemplate
        };
        var cancelBtn = new Button
        {
            Content = i18n[_conf.Lang]["btn_cancel"],
            Width = 80,
            Background = Brushes.Transparent,
            Foreground = fgColor,
            Template = btnTemplate
        };
        okBtn.Click += (_, __) => { result = true; dialog.Close(); };
        cancelBtn.Click += (_, __) => dialog.Close();
        btnPanel.Children.Add(okBtn);
        btnPanel.Children.Add(cancelBtn);
        mainPanel.Children.Add(btnPanel);

        dialog.Content = mainPanel;
        dialog.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) { result = true; dialog.Close(); }
            else if (e.Key == Key.Escape)
            {
                dialog.Close();
            }
        };

        dialog.SourceInitialized += (_, __) =>
        {
            SetWindowDarkMode(new WindowInteropHelper(dialog).Handle, _conf.DarkMode);
        };

        dialog.ShowDialog();
        return result;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Btn_Close_Click(null, null);
        }
    }

    private void Chk_EnableMarket_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var isUS = checkBox.Name == "chk_enable_us";
            var result = ShowConfirm(
                i18n[_conf.Lang]["msg_enable_us_hk_warn"],
                i18n[_conf.Lang]["ui_title_warn"]);

            if (result)
            {
                if (isUS)
                    _conf.EnableUS = true;
                else
                    _conf.EnableHK = true;
                _conf.Save();
                UpdateMarketCheckBoxText();
                UpdateIndexControlsVisibility();
                _mainWindow.MarkGridStructureDirty();
                DataUpdate();
            }
            else
            {
                // 用户取消，恢复未选中状态（避免循环触发）
                checkBox.Checked -= Chk_EnableMarket_Checked;
                checkBox.IsChecked = false;
                checkBox.Checked += Chk_EnableMarket_Checked;
            }
        }
    }

    private void Chk_EnableMarket_Unchecked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            var isUS = checkBox.Name == "chk_enable_us";
            if (isUS)
                _conf.EnableUS = false;
            else
                _conf.EnableHK = false;
            _conf.Save();
            UpdateMarketCheckBoxText();
            UpdateIndexControlsVisibility();
            _mainWindow.MarkGridStructureDirty();
            DataUpdate();
        }
    }

    private void UpdateMarketCheckBoxText()
    {
        // 设置固定文本（不再切换启用/禁用）
        txt_label_us.Text = i18n[_conf.Lang]["chk_enable_us"];
        txt_label_hk.Text = i18n[_conf.Lang]["chk_enable_hk"];
        
        // 控制绿色对号的显示/隐藏
        txt_checkmark_us.Visibility = _conf.EnableUS ? Visibility.Visible : Visibility.Collapsed;
        txt_checkmark_hk.Visibility = _conf.EnableHK ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateIndexControlsVisibility()
    {
        var hkVisible = _conf.EnableHK ? Visibility.Visible : Visibility.Collapsed;
        var usVisible = _conf.EnableUS ? Visibility.Visible : Visibility.Collapsed;
        StockIndexControlsHK.Visibility = hkVisible;
        lbl_index_hk.Visibility = hkVisible;
        StockIndexControlsUS.Visibility = usVisible;
        lbl_index_us.Visibility = usVisible;
    }

    public void UpdateDataGrid()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshStockCards();
        });
    }
}
