using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace iNeedMyMoneyBack;

/// <summary>
/// ConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class ConfigWindow : Window
{
    public static readonly SolidColorBrush LightGray = new(Color.FromRgb(222, 222, 222));// 浅灰
    public static readonly SolidColorBrush DarkGray = new(Color.FromRgb(66, 66, 66));// 深灰
    public static SolidColorBrush ColorChecked;

    private readonly Config _conf;
    private readonly Dictionary<string, Dictionary<string, string>> _i18n;
    public ConfigWindow()
    {
        InitializeComponent();
        _conf = MainWindow.g_conf;
        _i18n = MainWindow.g_i18n;
        InitUI();
    }

    private void InitUI()
    {
        Width = _conf.ConfigWindowWidth;
        Height = _conf.ConfigWindowHeight;
        Opacity = _conf.Opacity;
        Closing += (sender, e) =>
        {
            _conf.ConfigWindowWidth = Width;
            _conf.ConfigWindowHeight = Height;
        };
        MouseDown += (sender, e) => Utils.DragWindow(this);

        btn_close.Content = _i18n[_conf.Lang][btn_close.Name];
        dataGrid.Columns[0].Header = _i18n[_conf.Lang]["col_code"];
        dataGrid.Columns[1].Header = _i18n[_conf.Lang]["col_name"];
        dataGrid.Columns[2].Header = _i18n[_conf.Lang]["col_nickname"];
        dataGrid.Columns[3].Header = _i18n[_conf.Lang]["col_buyprice"];
        dataGrid.Columns[4].Header = _i18n[_conf.Lang]["col_buycount"];

        dataGrid.Foreground = MainWindow.color_fg;
        dataGrid.Background = MainWindow.color_bg;
        border.Background = MainWindow.color_bg;
        dataGrid.RowStyle.Setters.Add(new Setter(BackgroundProperty, MainWindow.color_bg));
        dataGrid.RowStyle.Setters.Add(new Setter(ForegroundProperty, MainWindow.color_fg));

        dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(BackgroundProperty, MainWindow.color_bg));
        dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, Brushes.Black));

        dataGrid.ItemsSource = _conf.Stocks;

        ColorChecked = new(Color.FromRgb(187, 187, 187));
        if (_conf.DarkMode)
        {
            dataGrid.CellStyle.Setters.Add(new Setter(BorderBrushProperty, DarkGray));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, DarkGray));
            btn_close.Foreground = LightGray;
            ColorChecked = DarkGray;
        }

        foreach (var kvp in _conf.FieldControl)
        {
            var cbx = CreateCheckBox(kvp.Key, kvp.Value);
            FieldControl.Children.Add(cbx);
        }
    }

    /// <summary>
    /// 添加复选框
    /// </summary>
    private CheckBox CreateCheckBox(string name, bool isChecked)
    {
        CheckBox checkBox = new()
        {
            Name = name,
            Content = _i18n[_conf.Lang][name],
            IsChecked = isChecked
        };
        if (_conf.DarkMode)
        {
            checkBox.Foreground = LightGray;
        }
        checkBox.Checked += CheckBox_Checked;
        checkBox.Unchecked += CheckBox_Checked;
        return checkBox;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox checkBox)
        {
            if (_conf.FieldControl.ContainsKey(checkBox.Name))
            {
                _conf.FieldControl[checkBox.Name] = (bool)checkBox.IsChecked;
            }
        }
    }

    private void Btn_Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        // 检查按键是否为 Esc
        if (e.Key == Key.Escape)
        {
            Btn_Close_Click(null, null);
        }
    }
}
