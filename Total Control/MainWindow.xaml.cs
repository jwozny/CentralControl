using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;
using Total_Control.User;

namespace Total_Control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDragWindow();

            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            _NavigationFrame.Navigate(new Dashboard());
            Dashboard.Style = selectedMenuStyle;
        }

        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Minimized;
        }
        private void RestoreWindow(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => WindowStyle = WindowStyle.None));
        }
        private void MaximizeWindow(object sender, RoutedEventArgs e)
        {
            SwitchState();
        }
        private void CloseWindow(object sender, RoutedEventArgs e)
        {
            Close();
        }
        private void SwitchState()
        {
            if (ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip)
            {
                switch (WindowState)
                {
                    case WindowState.Normal:
                        {
                            WindowState = WindowState.Maximized;
                            break;
                        }
                    case WindowState.Maximized:
                        {
                            WindowState = WindowState.Normal;
                            break;
                        }
                }
            }
        }
        private void InitializeDragWindow()
        {
            var restoreIfMove = false;

            Titlebar.MouseLeftButtonDown += (s, e) =>
            {
                if (e.ClickCount == 2)
                {
                    SwitchState();
                }
                else
                {
                    if (WindowState == WindowState.Maximized)
                    {
                        restoreIfMove = true;
                    }

                    DragMove();
                }
            };
            Titlebar.MouseLeftButtonUp += (s, e) =>
            {
                restoreIfMove = false;
            };
            Titlebar.MouseMove += (s, e) =>
            {
                if (restoreIfMove)
                {
                    restoreIfMove = false;
                    var mouseX = e.GetPosition(this).X;
                    var width = RestoreBounds.Width;
                    var x = mouseX - width / 2;

                    WindowState = WindowState.Normal;
                    Left = x;
                    Top = 0;
                    DragMove();
                }
            };
        }

        public void ResetMenuColors()
        {
            Style defaultMenuStyle = FindResource("MenuStyle") as Style;

            Dashboard.Style = defaultMenuStyle;
            User.Style = defaultMenuStyle;
            Server.Style = defaultMenuStyle;
            Storage.Style = defaultMenuStyle;
            Network.Style = defaultMenuStyle;
            Settings.Style = defaultMenuStyle;

            ResetSubMenuColors();
        }
        public void ResetSubMenuColors()
        {
            Style defaultSubMenuStyle = FindResource("SubMenuStyle") as Style;

            UserCreate.Style = defaultSubMenuStyle;
            UserReset.Style = defaultSubMenuStyle;
            UserUnlock.Style = defaultSubMenuStyle;
            UserExtend.Style = defaultSubMenuStyle;
            UserEnable.Style = defaultSubMenuStyle;
            UserDisable.Style = defaultSubMenuStyle;
            UserTerminate.Style = defaultSubMenuStyle;

            ServerService.Style = defaultSubMenuStyle;
            ServerShutdown.Style = defaultSubMenuStyle;
            ServerRestart.Style = defaultSubMenuStyle;

            StorageReport.Style = defaultSubMenuStyle;

            NetworkConnect.Style = defaultSubMenuStyle;
            NetworkBackup.Style = defaultSubMenuStyle;
            NetworkReload.Style = defaultSubMenuStyle;
            NetworkShutdown.Style = defaultSubMenuStyle;

            SettingsGeneral.Style = defaultSubMenuStyle;
            SettingsCredentials.Style = defaultSubMenuStyle;
        }

        private void Dashboard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            _NavigationFrame.Navigate(new Dashboard());
            ResetMenuColors();
            Dashboard.Style = selectedMenuStyle;
        }
        private void User_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            _NavigationFrame.Navigate(new User.User());
            ResetMenuColors();
            User.Style = selectedMenuStyle;
        }
        private void UserCreate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new User.Create());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserCreate.Style = selectedSubMenuStyle;
        }
        private void UserReset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new User.Reset());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserReset.Style = selectedSubMenuStyle;
        }
        private void UserUnlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new User.Unlock());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserUnlock.Style = selectedSubMenuStyle;
        }
        private void UserExtend_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new User.Extend());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserExtend.Style = selectedSubMenuStyle;
        }
        private void UserEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            _NavigationFrame.Navigate(new User.Enable());
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserEnable.Style = selectedSubMenuStyle;
        }
        private void UserDisable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            _NavigationFrame.Navigate(new User.Disable());
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserDisable.Style = selectedSubMenuStyle;
        }
        private void UserTerminate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new User.Terminate());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            User.Style = expandedMenuStyle;
            UserTerminate.Style = selectedSubMenuStyle;
        }
        private void Server_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Server());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Server.Style = selectedMenuStyle;
        }
        private void ServerService_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Server.Service());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Server.Style = expandedMenuStyle;
            ServerService.Style = selectedSubMenuStyle;
        }
        private void ServerShutdown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Server.Shutdown());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Server.Style = expandedMenuStyle;
            ServerShutdown.Style = selectedSubMenuStyle;
        }
        private void ServerRestart_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Server.Restart());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Server.Style = expandedMenuStyle;
            ServerRestart.Style = selectedSubMenuStyle;
        }
        private void Storage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Storage());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Storage.Style = selectedMenuStyle;
        }
        private void StorageReport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Storage.Report());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Storage.Style = expandedMenuStyle;
            StorageReport.Style = selectedSubMenuStyle;
        }
        private void Network_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Network());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Network.Style = selectedMenuStyle;
        }
        private void NetworkConnect_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Network.Connect());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Network.Style = expandedMenuStyle;
            NetworkConnect.Style = selectedSubMenuStyle;
        }
        private void NetworkBackup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Network.Backup());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Network.Style = expandedMenuStyle;
            NetworkBackup.Style = selectedSubMenuStyle;
        }
        private void NetworkReload_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Network.Reload());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Network.Style = expandedMenuStyle;
            NetworkReload.Style = selectedSubMenuStyle;
        }
        private void NetworkShutdown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Network.Shutdown());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Network.Style = expandedMenuStyle;
            NetworkShutdown.Style = selectedSubMenuStyle;
        }
        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Settings());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Settings.Style = selectedMenuStyle;
        }
        private void SettingsGeneral_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Settings.General());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Settings.Style = expandedMenuStyle;
            SettingsGeneral.Style = selectedSubMenuStyle;
        }
        private void SettingsCredentials_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;
            //_NavigationFrame.Navigate(new Settings.Credentials());
            _NavigationFrame.Navigate(null);
            ResetMenuColors();
            Settings.Style = expandedMenuStyle;
            SettingsCredentials.Style = selectedSubMenuStyle;
        }
    }
}
