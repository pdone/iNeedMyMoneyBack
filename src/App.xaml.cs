using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using iNeedMyMoneyBack;

[assembly: AssemblyTitle(App.ProductName)]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany(App.ProductCompany)]
[assembly: AssemblyProduct(App.ProductName)]
[assembly: AssemblyCopyright(App.Copyright)]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]

[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]

[assembly: AssemblyVersion(App.ProductVersion)]
[assembly: AssemblyFileVersion(App.ProductVersion)]

namespace iNeedMyMoneyBack;

/// <summary>
/// App.xaml 的交互逻辑
/// </summary>
public partial class App : Application
{
    public const string ProductName = "iNeedMyMoneyBack";
    public const string ProductFileName = ProductName + ".exe";
    public const string ProductCompany = "Pdone Technology Ltd.";
    public const string ProductVersion = "1.4";
    public const string Copyright = "Copyright © pdone 2025";

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
    }

    private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            Logger.Error(exception);
            var dr = MessageBox.Show($"Send the issues to developers?{Environment.NewLine}{Environment.NewLine}" +
                $"Unhandled Exception:{Environment.NewLine}" +
                $"{exception.Message}",
                "Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
            if (dr == MessageBoxResult.Yes)
            {
                Process.Start($"https://github.com/pdone/{ProductName}");
            }
        }
    }
}
