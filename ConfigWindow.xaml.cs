using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using static iNeedMyMoneyBack.Utils;

namespace iNeedMyMoneyBack;

public partial class ConfigWindow : Window
{
    public static readonly SolidColorBrush LightGray = new(Color.FromRgb(222, 222, 222));// 浅灰
    public static readonly SolidColorBrush DarkGray = new(Color.FromRgb(66, 66, 66));// 深灰

    private readonly Config _conf;
    private readonly StockConfigArray _stocks;
    private readonly MainWindow _mainWindow;
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
        // 窗体属性
        Width = _conf.ConfigWindowWidth;
        Height = _conf.ConfigWindowHeight;
        Opacity = _conf.Opacity;
        SizeChanged += (_, __) =>
        {
            _conf.ConfigWindowWidth = Width;
            _conf.ConfigWindowHeight = Height;
        };
        MouseDown += (_, __) => DragWindow(this);
        // 界面数据
        dataGrid.ItemsSource = _stocks;
        dataGrid.RowEditEnding += DataGrid_RowEditEnding;

        foreach (var kvp in _conf.FieldControls)
        {
            var cbx = CreateCheckBox(kvp.Key, kvp.Value);
            FieldControls.Children.Add(cbx);
        }
        foreach (var item in _conf.ExtendControls)
        {
            var cbxNewline = CreateCheckBox(item.GetNewLineKey(), item.NewLine);
            var cbx = CreateCheckBox(item.Key, item.Visable);
            if (item.Key.StartsWith(StockIndexPrefix))// 指数
            {
                StockIndexControls.Children.Add(cbxNewline);
                StockIndexControls.Children.Add(cbx);
            }
            else
            {
                ExtendControls.Children.Add(cbxNewline);
                ExtendControls.Children.Add(cbx);
            }
        }
        // 初始化语言
        InitLang();
        // 初始化颜色
        InitColor();
        InitBorderThickess(_conf.HideBorder);
    }

    private void DataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit)
        {
            return;
        }
        if (e.Row.Item is not StockConfig curItem)
        {
            return;
        }
        if (curItem.Code.IsNullOrWhiteSpace())
        {
            DataGrid.DeleteCommand.Execute(null, dataGrid);
            goto DataUpdate;
        }
        if (StockConfigArray.ImportantIndexs.Any(x => x.Code == curItem.Code.ToLower()))
        {
            MessageBox.Show("股指暂不支持添加监控，可在配置界面启用对应股指选项来查看");
            DataGrid.DeleteCommand.Execute(null, dataGrid);
            return;
        }
        var startWith = curItem.Code.Substring(0, 2).ToLower();
        if (startWith != "sh" && startWith != "sz" && startWith != "bj")
        {
            MessageBox.Show("代码需要以sh、sz、bj开头，分别代表上海、深圳、北京");
            DataGrid.DeleteCommand.Execute(null, dataGrid);
            return;
        }
    DataUpdate:
        MainWindow.g_conf_stocks_with_index = _stocks.Union(StockConfigArray.ImportantIndexs).ToList();
        _mainWindow.DataUpdate(false);
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

    public void InitBorderThickess(bool hide)
    {
        Resources["BorderThickness"] = new Thickness(hide ? 0 : 1);
    }

    public void InitLang()
    {
        Title = i18n[_conf.Lang]["ui_title_config"];
        btn_close.Content = i18n[_conf.Lang][btn_close.Name];
        dataGrid.Columns[0].Header = i18n[_conf.Lang]["col_code"];
        dataGrid.Columns[1].Header = i18n[_conf.Lang]["col_name"];
        dataGrid.Columns[2].Header = i18n[_conf.Lang]["col_nickname"];
        dataGrid.Columns[3].Header = i18n[_conf.Lang]["col_buyprice"];
        dataGrid.Columns[4].Header = i18n[_conf.Lang]["col_buycount"];
        foreach (var it in FieldControls.Children)
        {
            if (it is CheckBox cbx)
            {
                cbx.Content = i18n[_conf.Lang][cbx.Name];
            }
        }
        foreach (var it in ExtendControls.Children)
        {
            if (it is CheckBox cbx)
            {
                if (cbx.Name.EndsWith(NewlineSuffix))
                {
                    cbx.Content = i18n[_conf.Lang][NewlineSuffix];
                }
                else
                {
                    cbx.Content = i18n[_conf.Lang][cbx.Name];
                }
            }
        }
        foreach (var it in StockIndexControls.Children)
        {
            if (it is CheckBox cbx)
            {
                if (cbx.Name.EndsWith(NewlineSuffix))
                {
                    cbx.Content = i18n[_conf.Lang][NewlineSuffix];
                }
                else
                {
                    cbx.Content = i18n[_conf.Lang][cbx.Name];
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
            _mainWindow.DataUpdate();
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
