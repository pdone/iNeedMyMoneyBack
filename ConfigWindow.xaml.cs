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

    private readonly Config _conf;
    private readonly Dictionary<string, Dictionary<string, string>> _i18n;
    private readonly MainWindow _mainWindow;
    public ConfigWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _conf = MainWindow.g_conf;
        _i18n = MainWindow.g_i18n;
        InitUI();
    }

    private void InitUI()
    {
        // 窗体属性
        Width = _conf.ConfigWindowWidth;
        Height = _conf.ConfigWindowHeight;
        Opacity = _conf.Opacity;
        SizeChanged += (_, __) =>
        {
            _conf.ConfigWindowWidth = Width;
            _conf.ConfigWindowHeight = Height;
        };
        MouseDown += (_, __) => Utils.DragWindow(this);
        // 界面数据
        dataGrid.ItemsSource = _conf.Stocks;
        foreach (var kvp in _conf.FieldControls)
        {
            var cbx = CreateCheckBox(kvp.Key, kvp.Value);
            FieldControls.Children.Add(cbx);
        }
        foreach (var item in _conf.ExtendControls)
        {
            var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
            ExtendControls.Children.Add(cbxNewline);
            var cbx = CreateCheckBox(item.Key, item.Visable);
            ExtendControls.Children.Add(cbx);
        }
        // 初始化语言
        InitLang();
        // 初始化颜色
        InitColor();
    }

    public void InitColor()
    {
        dataGrid.Background = MainWindow.color_bg;
        border.Background = MainWindow.color_bg;
        Resources["CheckedColor"] = new SolidColorBrush(Color.FromRgb(187, 187, 187));
        Resources["TextColor"] = SystemColors.ControlTextBrush;
        if (_conf.DarkMode)
        {
            Resources["CheckedColor"] = DarkGray;
            Resources["TextColor"] = LightGray;
        }
    }

    public void InitLang()
    {
        btn_close.Content = _i18n[_conf.Lang][btn_close.Name];
        dataGrid.Columns[0].Header = _i18n[_conf.Lang]["col_code"];
        dataGrid.Columns[1].Header = _i18n[_conf.Lang]["col_name"];
        dataGrid.Columns[2].Header = _i18n[_conf.Lang]["col_nickname"];
        dataGrid.Columns[3].Header = _i18n[_conf.Lang]["col_buyprice"];
        dataGrid.Columns[4].Header = _i18n[_conf.Lang]["col_buycount"];
        foreach (var it in FieldControls.Children)
        {
            if (it is CheckBox cbx)
            {
                cbx.Content = _i18n[_conf.Lang][cbx.Name];
            }
        }
        foreach (var it in ExtendControls.Children)
        {
            if (it is CheckBox cbx)
            {
                if (cbx.Name.EndsWith(ExtendControlObj.NewlineSuffix))
                {
                    cbx.Content = _i18n[_conf.Lang][ExtendControlObj.NewlineSuffix];
                }
                else
                {
                    cbx.Content = _i18n[_conf.Lang][cbx.Name];
                }
            }
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
            }
            else if (_conf.ExtendControls.Find(x => x.Key == checkBox.Name) is ExtendControlObj ec)
            {
                ec.Visable = (bool)checkBox.IsChecked;
            }
            else if (_conf.ExtendControls.Find(x => x.GetNewLineKey() == checkBox.Name) is ExtendControlObj ec2)
            {
                ec2.NewLine = (bool)checkBox.IsChecked;
            }
            _mainWindow.DoWork(null, null);
        }
    }

    private void Btn_Close_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Hidden;
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
