using System;
using System.Windows;

namespace iNeedMyMoneyBack
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
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
                var dr = MessageBox.Show("未知错误！请将错误日志发送给开发者", "Tip", MessageBoxButton.OKCancel);
                if (dr == MessageBoxResult.OK)
                {
                    System.Diagnostics.Process.Start("https://awaw.cc");
                }
            }
        }
    }
}
