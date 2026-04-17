using System.Windows;
using System.Windows.Input;

namespace VoidStrike
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (UsernameTxt.Text == "102" && PasswordTxt.Password == "102")
            {
                MainWindow main = new MainWindow();
                main.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Invalid Credentials!", "VoidStrike Login", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://discord.gg/VEMm5hyEJ") { UseShellExecute = true });
        }

        private bool _rpcEnabled = false;

        private void DiscordRPC_Toggle(object sender, RoutedEventArgs e)
        {
            _rpcEnabled = !_rpcEnabled;
            if (_rpcEnabled)
            {
                DiscordRPCBtn.Content = "DISCORD RPC: ACTIVE";
                DiscordRPCBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5865F2"));
                DiscordRPCBtn.Foreground = System.Windows.Media.Brushes.White;

                Services.DiscordService.Instance.Initialize();
                Services.DiscordService.Instance.SetStatus("In Login Screen", "Authenticating with Master Core");
            }
            else
            {
                DiscordRPCBtn.Content = "DISCORD RPC: OFF";
                DiscordRPCBtn.Background = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#111111"));
                DiscordRPCBtn.Foreground = new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#5865F2"));

                Services.DiscordService.Instance.Deinitialize();
            }
        }
    }
}
