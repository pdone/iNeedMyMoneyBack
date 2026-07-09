using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using iNeedMyMoneyBack;

[assembly: InternalsVisibleTo("iNeedMyMoneyBack.Tests")]
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
    public const string ProductVersion = "2.4";
    public const string Copyright = "Copyright © pdone 2026";

    #region 检查实例是否已存在 存在则将其显示到前台
    private static Mutex _mutex;
    private static bool _createNew;

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private bool InstanceExists()
    {
        _mutex = new Mutex(true, $"{ProductName}-{Copyright}", out _createNew);
        if (!_createNew)
        {
            Logger.Info("Instance already exists");
            BringExistingInstanceToFront();
            return true;
        }
        return false;
    }

    private void BringExistingInstanceToFront()
    {
        // 获取当前应用程序的进程名
        var processName = Process.GetCurrentProcess().ProcessName;

        // 查找所有同名进程
        var processes = Process.GetProcessesByName(processName);

        foreach (var process in processes)
        {
            // 跳过当前进程
            if (process.Id == Process.GetCurrentProcess().Id)
            {
                continue;
            }

            // 获取已存在实例的窗口句柄
            var hWnd = process.MainWindowHandle;

            if (hWnd != IntPtr.Zero)
            {
                // 如果窗口是最小化状态，先恢复窗口
                if (IsIconic(hWnd))
                {
                    ShowWindow(hWnd, 9); // SW_RESTORE
                }

                // 将窗口显示到前台
                SetForegroundWindow(hWnd);
                break;
            }
        }
    }
    #endregion

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        if (InstanceExists())
        {
            Current.Shutdown();
            return;
        }
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
