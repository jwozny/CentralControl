using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using WinInterop = System.Windows.Interop;

namespace Central_Control
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Interaction logic for MainWindow.xaml
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            SourceInitialized += new EventHandler(MainWindow_SourceInitialized);

            GlobalConfig.LoadFromDisk();            
            InitializeMenu();

            VersionFootnote.Content = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name + " " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
        /// <summary>
        /// When the mainWindow is initialized, do this (window control hooks for custom titlebar)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
            WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(SnapAssist.WindowProc));
        }
        
        /* Window controls */
        /// <summary>
        /// Minimize window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MinimizeWindowButton_Click(object sender, RoutedEventArgs e)
        {
            //WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Minimized;
        }
        /// <summary>
        /// Switch the state of the window (maximize, normal)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MaximizeWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchState();
        }
        /// <summary>
        /// Close the window and quit the application
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        /// <summary>
        /// Was the window just resized?
        /// </summary>
        private bool Resized = false;
        /// <summary>
        /// When window size changes, resize specific controls to accomodate small sizes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WindowSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (this.ActualWidth <= 900)
            {
                ToggleMainMenu("Instahide");
                Resized = true;
            }
        }
        /// <summary>
        /// Run the main menu hide animation after resizing is complete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (Resized)
            {
                HideMenuStoryboard.Begin();
                Resized = false;
            }
        }
        /// <summary>
        /// Switch window state between Maximized and Normal
        /// </summary>
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
        /// <summary>
        /// Is the mouse left button down?
        /// </summary>
        private bool mRestoreIfMove = false;
        /// <summary>
        /// Restore the window from minimize state without artifacts
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RestoreWindow(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => WindowStyle = WindowStyle.None));
        }
        /// <summary>
        /// Mouse click action for the titlebar, double click switches state while single activates the drag function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Titlebar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if ((ResizeMode == ResizeMode.CanResize) || (ResizeMode == ResizeMode.CanResizeWithGrip))
                {
                    SwitchState();
                }

                return;
            }

            else if (WindowState == WindowState.Maximized)
            {
                mRestoreIfMove = true;
                return;
            }

            DragMove();
        }
        /// <summary>
        /// Action when the titlebar is let go (stop moving or confirm snap)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Titlebar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mRestoreIfMove = false;
        }
        /// <summary>
        /// While mouse is held down, move the window and use snap assist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Titlebar_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (mRestoreIfMove)
            {
                mRestoreIfMove = false;

                double percentHorizontal = e.GetPosition(this).X / ActualWidth;
                double targetHorizontal = RestoreBounds.Width * percentHorizontal;

                double percentVertical = e.GetPosition(this).Y / ActualHeight;
                double targetVertical = RestoreBounds.Height * percentVertical;

                WindowState = WindowState.Normal;

                SnapAssist.POINT lMousePosition;
                SnapAssist.GetCursorPos(out lMousePosition);

                //Left = lMousePosition.X - targetHorizontal;
                //Top = lMousePosition.Y - targetVertical;
                Left = lMousePosition.x - targetHorizontal;
                Top = lMousePosition.y - targetVertical;

                try { DragMove(); } catch { }
            }
        }

        /* Main Menu style functions */
        /// <summary>
        /// Initialize main menu and Submenu options
        /// </summary>
        private void InitializeMenu()
        {
            int SubmenuCount;

            // Set AD Submenu area height
            SubmenuCount = 0;
            AD.IsEnabled = true;
            ADPanel.Height = 26;

            if (ActiveDirectory.IsConnected && !ActiveDirectory.Connector.IsBusy)
            {
                if (ActiveDirectory.Users.Count > 0)
                {
                    AD_Users.IsEnabled = true;
                    SubmenuCount++;
                }
                if (ActiveDirectory.Groups.Count > 0)
                {
                    AD_Groups.IsEnabled = true;
                    SubmenuCount++;
                }
            }
            else
            {
                AD_Users.IsEnabled = false;
                AD_Groups.IsEnabled = false;
            }
            ADAnimation.To = 24 * SubmenuCount + 26;

            // Set Settings Submenu area height
            SubmenuCount = 0;
            Settings.IsEnabled = true;
            SettingsPanel.Height = 26;

            Settings_Credentials.IsEnabled = true;
            SubmenuCount++;

            SettingsAnimation.To = 24 * SubmenuCount + 26;
        }
        /// <summary>
        /// Reset all menu styles to their defaults
        /// </summary>
        private void ResetMenu()
        {
            RemoveAll_ADHandlers();

            // Reset AD menu style
            AD.Style = FindResource("Menu") as Style;

            // Reset Settings menu style
            Settings.Style = FindResource("Menu") as Style;
        }
        /// <summary>
        /// Reset all Submenu styles to their defaults
        /// </summary>
        private void ResetSubmenu()
        {
            // Reset AD Submenu styles
            AD_Users.Style = FindResource("Submenu") as Style;
            AD_Groups.Style = FindResource("Submenu") as Style;
            AD_Users.Visibility = Visibility.Collapsed;
            AD_Groups.Visibility = Visibility.Collapsed;

            // Reset Settings Submenu styles
            Settings_Credentials.Style = FindResource("Submenu") as Style;
            Settings_Credentials.Visibility = Visibility.Collapsed;
        }
        private void ShowMenu_AD()
        {
            if (ActiveDirectory.Users.Count > 0)
                AD_Users.Visibility = Visibility.Visible;
            if (ActiveDirectory.Groups.Count > 0)
                AD_Groups.Visibility = Visibility.Visible;
        }
        private void ShowMenu_Settings()
        {
            Settings_Credentials.Visibility = Visibility.Visible;
        }

        /* Main Menu controls */
        /// <summary>
        /// Returns if the main menu hidden
        /// </summary>
        private bool MenuHidden = false;
        /// <summary>
        /// Toggle between showing the main menu and hiding it
        /// </summary>
        /// <param name="state">null,Show,Hide,InstaHide</param>
        private void ToggleMainMenu(string state)
        {
            switch (state)
            {
                case "Show":
                    ShowMenuStoryboard.Begin();
                    MenuShowButton.Visibility = Visibility.Collapsed;
                    MenuHideButton.Visibility = Visibility.Visible;
                    MainMenu.SetValue(Grid.ColumnSpanProperty, 1);

                    MenuHidden = false;
                    break;
                case "Hide":
                    HideMenuStoryboard.Begin();
                    MenuShowButton.Visibility = Visibility.Visible;
                    MenuHideButton.Visibility = Visibility.Collapsed;
                    MainMenu.SetValue(Grid.ColumnSpanProperty, 2);

                    MenuHidden = true;
                    break;
                case "Instahide":
                    MainMenu.SetValue(WidthProperty, 20.0);
                    MenuShowButton.Visibility = Visibility.Visible;
                    MenuHideButton.Visibility = Visibility.Collapsed;
                    MainMenu.SetValue(Grid.ColumnSpanProperty, 2);

                    MenuHidden = true;
                    break;
                default:
                    if (MenuHidden)
                    {
                        ShowMenuStoryboard.Begin();
                        MenuShowButton.Visibility = Visibility.Collapsed;
                        MenuHideButton.Visibility = Visibility.Visible;
                        MainMenu.SetValue(Grid.ColumnSpanProperty, 1);

                        MenuHidden = false;
                    }
                    else
                    {
                        HideMenuStoryboard.Begin();
                        MenuShowButton.Visibility = Visibility.Visible;
                        MenuHideButton.Visibility = Visibility.Collapsed;
                        MainMenu.SetValue(Grid.ColumnSpanProperty, 2);

                        MenuHidden = true;
                    }
                    break;
            }
        }
        /// <summary>
        /// Show the main menu when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuShowButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMainMenu(null);
            MainMenu.SetValue(Grid.ColumnSpanProperty, 1);
        }
        /// <summary>
        /// Hide the main menu when clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuHideButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMainMenu(null);
            MainMenu.SetValue(Grid.ColumnSpanProperty, 2);
        }
        /// <summary>
        /// Hide the menu after temporarily showing it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainMenuPanel_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (MenuHidden)
            {
                ShowMenuStoryboard.Begin();
            }
        }
        /// <summary>
        /// Temporarily show the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainMenuPanel_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (MenuHidden)
            {
                HideMenuStoryboard.Begin();
            }
        }
        /// <summary>
        /// Page initialization for AD_Connect
        /// </summary>
        private AD.Connect AD_Connect_Page = new AD.Connect();
        /// <summary>
        /// Show the AD_Connect page if not connected to AD, otherwise expand the menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetMenu();
            AD.Style = FindResource("Menu_Expanded") as Style;

            if (!ActiveDirectory.IsConnected)
            {
                ResetSubmenu();
                AD.Style = FindResource("Menu_Selected") as Style;

                _NavigationFrame.Navigate(AD_Connect_Page);
                _NavigationFrame.NavigationService.LoadCompleted += AD_Connected;
            }
            else if (ActiveDirectory.Connector.IsBusy)
            {
                ResetSubmenu();
                AD.Style = FindResource("Menu_Selected") as Style;

                _NavigationFrame.Navigate(AD_Connect_Page);
                _NavigationFrame.NavigationService.LoadCompleted += AD_Connected;
            }
            else
            {
                InitializeMenu();
                ShowMenu_AD();
            }
        }
        /// <summary>
        /// Page initialization for AD_Users
        /// </summary>
        private AD.Users AD_Users_Page = new AD.Users();
        /// <summary>
        /// Go to the AD_Users page and watch for AD disconnect event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Users_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetMenu();
            AD.Style = FindResource("Menu_Expanded") as Style;

            ResetSubmenu();
            ShowMenu_AD();
            AD_Users.Style = FindResource("Submenu_Selected") as Style;
            
            _NavigationFrame.Navigate(AD_Users_Page);
            _NavigationFrame.NavigationService.LoadCompleted += AD_Disconnected_Users;
        }
        /// <summary>
        /// Page initialization for AD_Groups
        /// </summary>
        private AD.Groups AD_Groups_Page = new AD.Groups();
        /// <summary>
        /// Go to the AD_Groups page and watch for AD disconnect event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Groups_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetMenu();
            AD.Style = FindResource("Menu_Expanded") as Style;

            ResetSubmenu();
            ShowMenu_AD();
            AD_Groups.Style = FindResource("Submenu_Selected") as Style;
            
            _NavigationFrame.Navigate(AD_Groups_Page);
            _NavigationFrame.NavigationService.LoadCompleted += AD_Disconnected_Groups;
        }
        /// <summary>
        /// Expand the Settings menu
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetMenu();
            Settings.Style = FindResource("Menu_Expanded") as Style;

            ShowMenu_Settings();
        }
        /// <summary>
        /// Page initialization for the Credentials page
        /// </summary>
        private Settings.Credentials Settings_Credentials_Page = new Settings.Credentials();
        /// <summary>
        /// Go to the Credentials page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Settings_Credentials_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ResetMenu();
            Settings.Style = FindResource("Menu_Expanded") as Style;

            ResetSubmenu();
            ShowMenu_Settings();
            Settings_Credentials.Style = FindResource("Submenu_Selected") as Style;

            _NavigationFrame.Navigate(Settings_Credentials_Page);
        }

        /* AD Event Handlers */
        /// <summary>
        /// Remove all AD related event handlers when assigning a new handler
        /// </summary>
        private void RemoveAll_ADHandlers()
        {
            _NavigationFrame.NavigationService.LoadCompleted -= AD_Connected;
            _NavigationFrame.NavigationService.LoadCompleted -= AD_Disconnected_Users;
            _NavigationFrame.NavigationService.LoadCompleted -= AD_Disconnected_Groups;
        }

        /// <summary>
        /// Watch for the ConnectionVerified event in the AD_Connect page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Connected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((AD.Connect)e.Content).ConnectionVerified += new EventHandler(AD_Connected);
        }
        /// <summary>
        /// Handler - Do this when the AD pages detect that AD is accessible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Connected(object sender, EventArgs e)
        {
            InitializeMenu();

            ResetMenu();
            AD.Style = FindResource("Menu_Expanded") as Style;

            ResetSubmenu();
            ShowMenu_AD();
            AD_Users.Style = FindResource("Submenu_Selected") as Style;

            ADStoryboard.Begin();

            _NavigationFrame.Navigate(AD_Users_Page);
            _NavigationFrame.NavigationService.LoadCompleted += AD_Disconnected;
        }

        /// <summary>
        /// Watch for the Disconnected event in the AD Users page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Disconnected_Users(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((AD.Users)e.Content).Disconnected += new EventHandler(AD_Disconnected);
        }
        /// <summary>
        /// Watch for the Disconnected event in the AD Groups page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Disconnected_Groups(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((AD.Groups)e.Content).Disconnected += new EventHandler(AD_Disconnected);
        }
        /// <summary>
        /// Handler - Do this when the AD pages detect that AD is no longer accessible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_Disconnected(object sender, EventArgs e)
        {
            InitializeMenu();

            if (!ActiveDirectory.IsConnected)
            {
                AD.Style = FindResource("Menu_Selected") as Style;

                ResetSubmenu();

                _NavigationFrame.Navigate(AD_Connect_Page);
                _NavigationFrame.NavigationService.LoadCompleted += AD_Connected;
            }
        }
    }
}
