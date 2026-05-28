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

        InitLang();
    }

    private void RefreshStockCards()
    {
        stockListPanel.Children.Clear();
        foreach (var stock in _stocks)
        {
            var card = CreateStockCard(stock);
            stockListPanel.Children.Add(card);
        }
    }

    private Border CreateStockCard(StockConfig stock)
    {
        var card = new Border
        {
            Margin = new Thickness(0, 0, 0, 5),
            Padding = new Thickness(8),
            Background = Resources["CardBackground"] as SolidColorBrush,
            BorderBrush = Resources["CardBorder"] as SolidColorBrush,
            BorderThickness = new Thickness(1),
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
            if (stock.BuyPrice > 0) parts.Add($"{i18n[_conf.Lang]["col_buyprice"]}: {stock.BuyPrice:F2}");
            if (stock.BuyCount > 0) parts.Add($"{i18n[_conf.Lang]["col_buycount"]}: {stock.BuyCount}");
            buyInfo.Text = string.Join(" | ", parts);
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
            if (stock.ReminderPriceUp > 0) parts.Add($"↑{stock.ReminderPriceUp:F2}");
            if (stock.ReminderPriceDown > 0) parts.Add($"↓{stock.ReminderPriceDown:F2}");
            parts.Add($"{i18n[_conf.Lang]["col_ReminderTimes"]}: {stock.ReminderTimes}");
            reminderInfo.Text = string.Join(" ", parts);
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

        buttonPanel.Children.Add(editBtn);
        buttonPanel.Children.Add(deleteBtn);

        Grid.SetColumn(buttonPanel, 1);
        grid.Children.Add(buttonPanel);

        card.Child = grid;
        card.MouseLeftButtonDown += (_, e) =>
        {
            if (e.ClickCount == 2) ShowEditDialog(stock);
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
            MessageBox.Show(i18n[_conf.Lang]["msg_index_not_support_add"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var startWith = code.Length >= 2 ? code.Substring(0, 2) : "";
        if (!SupportExchange.Contains(startWith))
        {
            MessageBox.Show(string.Format(i18n[_conf.Lang]["msg_exchange_not_support"], string.Join("、", SupportExchange)), i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        if (_stocks.Any(x => x.Code == code))
        {
            MessageBox.Show(i18n[_conf.Lang]["msg_stock_exists"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var isValid = await _mainWindow.VerifyStockCode(code);
        if (!isValid)
        {
            MessageBox.Show(i18n[_conf.Lang]["msg_stock_not_found"], i18n[_conf.Lang]["ui_title_warn"]);
            return;
        }

        var newStock = new StockConfig(code);
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
            ResizeMode = ResizeMode.NoResize
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
        txtBorder.SetValue(Border.CornerRadiusProperty, new CornerRadius(4));
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
            if (e.Key == Key.Enter) okBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            else if (e.Key == Key.Escape) cancelBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
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
            var result = MessageBox.Show(
                string.Format(i18n[_conf.Lang]["msg_confirm_delete"], stock.DisplayName),
                i18n[_conf.Lang]["ui_title_confirm"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _stocks.Remove(stock);
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
        MainWindow.g_conf_stocks_with_index = _stocks.Union(StockConfigArray.ImportantIndexs).ToList();
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
        grid.Background = MainWindow.color_bg;

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
        lbl_basic_fields.Text = i18n[_conf.Lang]["lbl_basic_fields"];
        lbl_index_fields.Text = i18n[_conf.Lang]["lbl_index_fields"];
        lbl_index_a.Text = i18n[_conf.Lang]["lbl_index_a"];
        lbl_index_hk.Text = i18n[_conf.Lang]["lbl_index_hk"];
        lbl_index_us.Text = i18n[_conf.Lang]["lbl_index_us"];
        lbl_extend_fields.Text = i18n[_conf.Lang]["lbl_extend_fields"];

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
        if (!key.StartsWith(StockIndexPrefix)) return "";
        var code = key.Substring(StockIndexPrefix.Length);
        if (code.StartsWith("hk")) return "hk";
        if (code.StartsWith("us")) return "us";
        return "a";
    }

    private void Btn_Close_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Hidden;
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Btn_Close_Click(null, null);
        }
    }

    public void UpdateDataGrid()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            RefreshStockCards();
        });
    }
}
