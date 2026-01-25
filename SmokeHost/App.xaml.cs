using System;
using System.Windows;
using System.Windows.Threading;

namespace ThinkGeo.SmokeHost
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Surface any unexpected UI-thread exceptions early (helpful for async/lifecycle bugs).
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            base.OnStartup(e);
        }

        private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // Don't swallow exceptions silently; show a simple dialog and let the debugger catch it.
            MessageBox.Show(
                e.Exception.ToString(),
                "Unhandled UI exception",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            // Mark as handled so the app stays up for inspection; comment this out if you prefer crash-on-error.
            e.Handled = true;
        }
    }
}
