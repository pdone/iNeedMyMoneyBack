using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace iNeedMyMoneyBack;

/// <summary>
/// ConfigWindow.xaml 的交互逻辑
/// </summary>
public partial class ConfigWindow : Window
{
    private readonly Config _conf;
    private readonly Dictionary<string, Dictionary<string, string>> _i18n;
    public ConfigWindow()
    {
        InitializeComponent();
        MouseDown += (sender, e) => Utils.DragWindow(this);
        MinWidth = 270;
        MinHeight = 165;
        _conf = MainWindow._conf;
        _i18n = MainWindow._i18n;

        Opacity = _conf.Opacity;
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
        dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Center));
        dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, Brushes.Black));

        if (_conf.DarkMode)
        {
            dataGrid.CellStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(66, 66, 66))));
            dataGrid.ColumnHeaderStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Color.FromRgb(66, 66, 66))));
        }

        dataGrid.ItemsSource = _conf.Stocks;
    }

    private void btn_close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
