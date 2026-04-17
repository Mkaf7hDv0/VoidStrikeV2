using System;
using System.IO;
using System.Windows;

namespace VoidStrike
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, args) =>
                LogException(args.ExceptionObject as Exception);

            DispatcherUnhandledException += (s, args) => {
                LogException(args.Exception);
                args.Handled = true;
            };

            try
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Services.DiscordService.Instance.Dispose();
            base.OnExit(e);
        }

        private void LogException(Exception ex)
        {
            if (ex == null) return;
            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error_log.txt");
            string message = $"[{DateTime.Now}] ERROR: {ex.Message}\nStackTrace: {ex.StackTrace}\nInnerException: {ex.InnerException?.Message}\n\n";
            File.AppendAllText(logPath, message);
            MessageBox.Show($"FATAL ERROR: {ex.Message}", "VoidStrike Core Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
