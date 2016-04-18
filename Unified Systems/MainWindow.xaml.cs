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
using Unified_Systems.User;
using WinInterop = System.Windows.Interop;

namespace Unified_Systems
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();

            SourceInitialized += new EventHandler(MainWindow_SourceInitialized);

            //ActiveDirectory.Connect();
            ShowHideOptions();
        }

        private Configuration configs = new Configuration();
        private void LoadConfig()
        {
            try
            {
                configs = ConfigActions.LoadConfig();
            }
            catch
            {
                File.Delete(System.IO.Path.GetTempPath() + @"\~onfig");
            }

            ActiveDirectory.DomainName = configs.DomainName;
            ActiveDirectory.AuthenticatingUsername = configs.ADUsername;
            ActiveDirectory.AuthenticatingPassword = configs.ADPassword;
        }

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
            WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(WindowProc));
        }
        private static void WmGetMinMaxInfo(System.IntPtr hwnd, System.IntPtr lParam)
        {

            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            int MONITOR_DEFAULTTONEAREST = 0x00000002;
            System.IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != System.IntPtr.Zero)
            {

                MONITORINFO monitorInfo = new MONITORINFO();
                GetMonitorInfo(monitor, monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.x = Math.Abs(rcWorkArea.left - rcMonitorArea.left);
                mmi.ptMaxPosition.y = Math.Abs(rcWorkArea.top - rcMonitorArea.top);
                mmi.ptMaxSize.x = Math.Abs(rcWorkArea.right - rcWorkArea.left);
                mmi.ptMaxSize.y = Math.Abs(rcWorkArea.bottom - rcWorkArea.top);
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }
        //public override void OnApplyTemplate()
        //{
        //    System.IntPtr handle = (new WinInterop.WindowInteropHelper(this)).Handle;
        //    WinInterop.HwndSource.FromHwnd(handle).AddHook(new WinInterop.HwndSourceHook(WindowProc));
        //}
        private static System.IntPtr WindowProc(System.IntPtr hwnd, int msg, System.IntPtr wParam, System.IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case 0x0024:
                    WmGetMinMaxInfo(hwnd, lParam);
                    handled = false;
                    break;
            }

            return (System.IntPtr)0;
        }
        /// <summary>
        /// POINT aka POINTAPI
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            /// <summary>
            /// x coordinate of point.
            /// </summary>
            public int x;
            /// <summary>
            /// y coordinate of point.
            /// </summary>
            public int y;

            /// <summary>
            /// Construct a point of coordinates (x,y).
            /// </summary>
            public POINT(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        };
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MONITORINFO
        {
            /// <summary>
            /// </summary>            
            public int cbSize = Marshal.SizeOf(typeof(MONITORINFO));

            /// <summary>
            /// </summary>            
            public RECT rcMonitor = new RECT();

            /// <summary>
            /// </summary>            
            public RECT rcWork = new RECT();

            /// <summary>
            /// </summary>            
            public int dwFlags = 0;
        }

        /* Win32 */
        /// <summary>
        /// Gets monitor info
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
            
            public static readonly RECT Empty = new RECT();
            
            public int Width
            {
                get { return Math.Abs(right - left); }  // Abs needed for BIDI OS
            }
            public int Height
            {
                get { return bottom - top; }
            }
            
            public RECT(int left, int top, int right, int bottom)
            {
                this.left = left;
                this.top = top;
                this.right = right;
                this.bottom = bottom;
            }
            
            public RECT(RECT rcSrc)
            {
                this.left = rcSrc.left;
                this.top = rcSrc.top;
                this.right = rcSrc.right;
                this.bottom = rcSrc.bottom;
            }
            
            public bool IsEmpty
            {
                get
                {
                    // BUGBUG : On Bidi OS (hebrew arabic) left > right
                    return left >= right || top >= bottom;
                }
            }
            /// <summary> Return a user friendly representation of this struct </summary>
            public override string ToString()
            {
                if (this == RECT.Empty) { return "RECT {Empty}"; }
                return "RECT { left : " + left + " / top : " + top + " / right : " + right + " / bottom : " + bottom + " }";
            }

            /// <summary> Determine if 2 RECT are equal (deep compare) </summary>
            public override bool Equals(object obj)
            {
                if (!(obj is Rect)) { return false; }
                return (this == (RECT)obj);
            }

            /// <summary>Return the HashCode for this struct (not garanteed to be unique)</summary>
            public override int GetHashCode()
            {
                return left.GetHashCode() + top.GetHashCode() + right.GetHashCode() + bottom.GetHashCode();
            }
            
            /// <summary> Determine if 2 RECT are equal (deep compare)</summary>
            public static bool operator ==(RECT rect1, RECT rect2)
            {
                return (rect1.left == rect2.left && rect1.top == rect2.top && rect1.right == rect2.right && rect1.bottom == rect2.bottom);
            }

            /// <summary> Determine if 2 RECT are different(deep compare)</summary>
            public static bool operator !=(RECT rect1, RECT rect2)
            {
                return !(rect1 == rect2);
            }


        }
        [DllImport("user32")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, MONITORINFO lpmi);
        [DllImport("User32")]
        internal static extern IntPtr MonitorFromWindow(IntPtr handle, int flags);
        /// <summary>
        /// Confirms whether the cursor is on the current monitor
        /// </summary>
        /// This is to allow the mouse to drag the window down from maximized to normal
        /// and keep the window on the same monitor if multiple monitors are involved.
        /// <param name="lpPoint"></param>
        /// <returns></returns>
        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetCursorPos(out POINT lpPoint);

        /* Window Control Actions */
        private bool mRestoreIfMove = false;
        private void MinimizeWindow(object sender, RoutedEventArgs e)
        {
            //WindowStyle = WindowStyle.SingleBorderWindow;
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
        private void Titlebar_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mRestoreIfMove = false;
        }
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

                POINT lMousePosition;
                GetCursorPos(out lMousePosition);

                //Left = lMousePosition.X - targetHorizontal;
                //Top = lMousePosition.Y - targetVertical;
                Left = lMousePosition.x - targetHorizontal;
                Top = lMousePosition.y - targetVertical;

                try { DragMove(); } catch { }
            }
        }

        /* Menu Button Actions */
        private string LoadedSubmenu;
        private void ResetMenuColors()
        {
            RemoveAllHandlers();
            Style defaultMenuStyle = FindResource("MenuStyle") as Style;

            Dashboard.Style = defaultMenuStyle;
            User.Style = defaultMenuStyle;
            Server.Style = defaultMenuStyle;
            Storage.Style = defaultMenuStyle;
            Network.Style = defaultMenuStyle;
            Settings.Style = defaultMenuStyle;

            //ResetSubMenuColors();
        }
        private void ResetSubMenuColors()
        {
            Style defaultSubMenuStyle = FindResource("SubMenuStyle") as Style;

            UserLookup.Style = defaultSubMenuStyle;
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
        private void HighlightSubmenus()
        {
            Style SelectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style HighlightedSubMenuStyle = FindResource("HighlightedSubMenuStyle") as Style;

            ResetSubMenuColors();
            
            if (!ReferenceEquals(ActiveDirectory.SelectedUser, null))
            {
                UserLookup.Style = HighlightedSubMenuStyle;
                if (!ActiveDirectory.GetUserProperties.IsBusy)
                {
                    if (ActiveDirectory.SelectedUser_IsAccountLockedOut) UserUnlock.Style = HighlightedSubMenuStyle;
                }
                if (ActiveDirectory.SelectedUser.IsAccountExpired()) UserExtend.Style = HighlightedSubMenuStyle;
                if (!ActiveDirectory.SelectedUser.Enabled.Value) UserEnable.Style = HighlightedSubMenuStyle;
                if (ActiveDirectory.SelectedUser.Enabled.Value) UserDisable.Style = HighlightedSubMenuStyle;
            }

            if (LoadedSubmenu == "User.Lookup") UserLookup.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Create") UserCreate.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Reset") UserReset.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Unlock") UserUnlock.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Extend") UserExtend.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Enable") UserEnable.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Disable") UserDisable.Style = SelectedSubMenuStyle;
            if (LoadedSubmenu == "User.Terminate") UserTerminate.Style = SelectedSubMenuStyle;
        }
        private void ShowHideOptions()
        {
            Dashboard.IsEnabled = false;
            DashboardPanel.Height = 28;
            DashboardAnimation.To = 24 * 0 + 28 - 0;

            User.IsEnabled = true;
            UserPanel.Height = 28;
            if (ActiveDirectory.IsConnected)
            {
                UserLookup.IsEnabled = true;
                UserCreate.IsEnabled = false;
                UserCreate.Visibility = Visibility.Collapsed;
                UserReset.IsEnabled = false;
                UserReset.Visibility = Visibility.Collapsed;
                UserUnlock.IsEnabled = true;
                UserExtend.IsEnabled = true;
                UserEnable.IsEnabled = true;
                UserDisable.IsEnabled = true;
                UserTerminate.IsEnabled = false;
                UserTerminate.Visibility = Visibility.Collapsed;

                UserAnimation.To = 24 * 5 + 28 - 2;

                UserStoryboard.Begin();
            }
            else
            {
                UserLookup.IsEnabled = false;
                UserCreate.IsEnabled = false;
                UserReset.IsEnabled = false;
                UserUnlock.IsEnabled = false;
                UserExtend.IsEnabled = false;
                UserEnable.IsEnabled = false;
                UserDisable.IsEnabled = false;
                UserTerminate.IsEnabled = false;

                UserAnimation.To = 24 * 0 + 28 - 0;
            }

            Server.IsEnabled = false;
            ServerPanel.Height = 28;
            ServerService.IsEnabled = false;
            ServerService.Visibility = Visibility.Collapsed;
            ServerShutdown.IsEnabled = false;
            ServerShutdown.Visibility = Visibility.Collapsed;
            ServerRestart.IsEnabled = false;
            ServerRestart.Visibility = Visibility.Collapsed;
            ServerAnimation.To = 24 * 0 + 28 - 0;

            Storage.IsEnabled = false;
            StoragePanel.Height = 28;
            StorageReport.IsEnabled = false;
            StorageReport.Visibility = Visibility.Collapsed;
            StorageAnimation.To = 24 * 0 + 28 - 0;

            Network.IsEnabled = false;
            NetworkPanel.Height = 28;
            NetworkConnect.IsEnabled = false;
            NetworkConnect.Visibility = Visibility.Collapsed;
            NetworkBackup.IsEnabled = false;
            NetworkBackup.Visibility = Visibility.Collapsed;
            NetworkReload.IsEnabled = false;
            NetworkReload.Visibility = Visibility.Collapsed;
            NetworkShutdown.IsEnabled = false;
            NetworkShutdown.Visibility = Visibility.Collapsed;
            NetworkAnimation.To = 24 * 0 + 28 - 0;

            Settings.IsEnabled = true;
            SettingsPanel.Height = 28;
            SettingsGeneral.IsEnabled = false;
            SettingsGeneral.Visibility = Visibility.Collapsed;
            SettingsCredentials.IsEnabled = true;
            SettingsAnimation.To = 24 * 1 + 28 - 2;
        }

        private void Dashboard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Dashboard.Style = expandedMenuStyle;
            if (false/*Add check here*/)
            {
                //_NavigationFrame.Navigate(new Dashboard.Dashboard());
                ResetSubMenuColors();
                Dashboard.Style = selectedMenuStyle;
            }
        }

        private void User_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;
            if (!ActiveDirectory.IsConnected)
            {
                _NavigationFrame.Navigate(new User.User());
                ResetSubMenuColors();
                User.Style = selectedMenuStyle;

                RemoveAllHandlers();
                _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(User_Connected);
            }
        }
        private void UserLookup_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Lookup());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Lookup";
            HighlightSubmenus();
            UserLookup.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_Disconnected);
        }
        private void UserCreate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            //_NavigationFrame.Navigate(new User.Create());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Create";
            HighlightSubmenus();
            UserCreate.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserCreate_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserCreate_Disconnected);
        }
        private void UserReset_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            //_NavigationFrame.Navigate(new User.Reset());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Reset";
            HighlightSubmenus();
            UserReset.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserReset_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserReset_Disconnected);
        }
        private void UserUnlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Unlock());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Unlock";
            HighlightSubmenus();
            UserUnlock.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserUnlock_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserUnlock_Disconnected);
        }
        private void UserExtend_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Extend());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Extend";
            HighlightSubmenus();
            UserExtend.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserExtend_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserExtend_Disconnected);
        }
        private void UserEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Enable());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Enable";
            HighlightSubmenus();
            UserEnable.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserEnable_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserEnable_Disconnected);
        }
        private void UserDisable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Disable());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Disable";
            HighlightSubmenus();
            UserDisable.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserDisable_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserDisable_Disconnected);
        }
        private void UserTerminate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            //_NavigationFrame.Navigate(new User.Terminate());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Terminate";
            HighlightSubmenus();
            UserTerminate.Style = selectedSubMenuStyle;

            RemoveAllHandlers();
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserTerminate_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserTerminate_Disconnected);
        }
        // Add:
        // User - Grant Domain Admin
        // User - Revoke Domain Admin
        // User - Grant Local Admin
        // User - Revoke Local Admin
        // User - Grant VPN Access
        // User - Revoke VPN Access

        //private Server.Server Server_ = new Server.Server();
        private void Server_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Server.Style = expandedMenuStyle;
            if (false/*Add check here*/)
            {
                //_NavigationFrame.Navigate(Server_);
                ResetSubMenuColors();
                Server.Style = selectedMenuStyle;
            }
        }
        //private Server.Service Server_Service = new Server.Service();
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
        //private Server.Shutdown Server_Shutdown = new Server.Shutdown();
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
        //private Server.Restart Server_Restart = new Server.Restart();
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

        //private Storage.Storage Storage_ = new Storage.Storage();
        private void Storage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Storage.Style = expandedMenuStyle;
            if (false/*Add check here*/)
            {
                //_NavigationFrame.Navigate(Storage_Page);
                ResetSubMenuColors();
                Storage.Style = selectedMenuStyle;
            }
        }
        //private Storage.Report Storage_Report = new Storage.Report();
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

        //private Network.Network Network_ = new Network.Network();
        private void Network_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Network.Style = expandedMenuStyle;
            if (false/*Add check here*/)
            {
                //_NavigationFrame.Navigate(Network_);
                ResetSubMenuColors();
                Network.Style = selectedMenuStyle;
            }
        }
        //private Network.Connect Network_Connect = new Network.Connect();
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
        //private Network.Backup Network_Backup = new Network.Backup();
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
        //private Network.Reload Network_Reload = new Network.Reload();
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
        //private Network.Shutdown Network_Shutdown = new Network.Shutdown();
        private void NetworkShutdown_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Network.Style = expandedMenuStyle;

            //_NavigationFrame.Navigate(NetworkShutdown_Page);
            ResetSubMenuColors();
            NetworkShutdown.Style = selectedSubMenuStyle;
        }

        //private Settings.Settings Settings_ = new Settings.Settings();
        private void Settings_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Settings.Style = expandedMenuStyle;
            if (false/*Add check here*/)
            {
                //_NavigationFrame.Navigate(Settings_Page);
                ResetSubMenuColors();
                Settings.Style = selectedMenuStyle;
            }
        }
        //private Settings.General Settings_General = new Settings.General();
        private void SettingsGeneral_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Settings.Style = expandedMenuStyle;

            //_NavigationFrame.Navigate(Settings_General);
            ResetSubMenuColors();
            SettingsGeneral.Style = selectedSubMenuStyle;
        }
        private Settings.Credentials Settings_Credentials = new Settings.Credentials();
        private void SettingsCredentials_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            Settings.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(Settings_Credentials);
            ResetSubMenuColors();
            SettingsCredentials.Style = selectedSubMenuStyle;
        }

        /* Menu Handlers */
        private void RemoveAllHandlers()
        {
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(User_Connected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserCreate_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserReset_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserUnlock_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserExtend_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserEnable_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserDisable_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserTerminate_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserCreate_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserReset_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserUnlock_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserExtend_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserEnable_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserDisable_Disconnected);
            _NavigationFrame.NavigationService.LoadCompleted -= new System.Windows.Navigation.LoadCompletedEventHandler(UserTerminate_Disconnected);
        }

        private void User_Connected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.User)e.Content).ConnectionVerified += new EventHandler(User_Connected);
        }
        private void User_Connected(object sender, EventArgs e)
        {
            RemoveAllHandlers();
            ShowHideOptions();

            Style selectedSubMenuStyle = FindResource("SelectedSubMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;

            _NavigationFrame.Navigate(new User.Lookup());
            ResetSubMenuColors();
            LoadedSubmenu = "User.Lookup";
            UserLookup.Style = selectedSubMenuStyle;

            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_HighlightSubmenus);
            _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(UserLookup_Disconnected);
        }

        private void UserLookup_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Lookup)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserCreate_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Create)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserReset_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Reset)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserUnlock_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Unlock)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserExtend_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Extend)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserEnable_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Enable)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserDisable_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Disable)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void UserTerminate_HighlightSubmenus(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Terminate)e.Content).HighlightSubmenus += new EventHandler(User_HighlightSubmenus);
        }
        private void User_HighlightSubmenus(object sender, EventArgs e)
        {
            HighlightSubmenus();
        }

        private void UserLookup_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Lookup)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserCreate_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Create)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserReset_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Reset)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserUnlock_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Unlock)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserExtend_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Extend)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserEnable_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Enable)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserDisable_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            ((User.Disable)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserTerminate_Disconnected(object sender, System.Windows.Navigation.NavigationEventArgs e)
        {
            //((User.Terminate)e.Content).Disconnected += new EventHandler(UserSubmenu_ExitToLogin);
        }
        private void UserSubmenu_ExitToLogin(object sender, EventArgs e)
        {
            ShowHideOptions();

            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            Style expandedMenuStyle = FindResource("ExpandedMenuStyle") as Style;

            ResetMenuColors();
            User.Style = expandedMenuStyle;
            if (!ActiveDirectory.IsConnected)
            {
                _NavigationFrame.Navigate(new User.User());
                ResetSubMenuColors();
                User.Style = selectedMenuStyle;

                _NavigationFrame.NavigationService.LoadCompleted += new System.Windows.Navigation.LoadCompletedEventHandler(User_Connected);
            }
        }
    }
}
