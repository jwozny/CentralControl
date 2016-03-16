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
            SourceInitialized += new EventHandler(MainWindow_SourceInitialized);
            ActiveDirectory.InitializeDomain();
            ActiveDirectory.RefreshUsers();

            Style selectedMenuStyle = FindResource("SelectedMenuStyle") as Style;
            _NavigationFrame.Navigate(new Dashboard());
            Dashboard.Style = selectedMenuStyle;
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

    /// <summary>
    /// Static class to store active directory user info collection and action scripts
    /// </summary>
    public static class ActiveDirectory
    {
        public static PrincipalContext Domain;
        public static PrincipalSearcher Searcher;
        public static void InitializeDomain()
        {
            Domain = new PrincipalContext(ContextType.Domain, "asse.org");
            Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
            users = new List<UserPrincipal>();
        }
        private static List<UserPrincipal> users;
        public static List<UserPrincipal> Users
        {
            get
            {
                return users;
            }
        }
        public static UserPrincipal SelectedUser;

        public static void RefreshUsers()
        {
            Searcher.Dispose();
            Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
            if (!ReferenceEquals(users, null))
            {
                users.Clear();
            }
            foreach (UserPrincipal User in Searcher.FindAll())
            {
                users.Add(User);
            }
            //users.Sort();
        }

        /* Extensions */
        private static string GetProperty(this Principal principal, string property)
        {
            DirectoryEntry directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
            if (directoryEntry.Properties.Contains(property))
                return directoryEntry.Properties[property].Value.ToString();
            else
                return string.Empty;
        }
        public static string GetTitle(this Principal principal)
        {
            return principal.GetProperty("title");
        }
        public static string GetDepartment(this Principal principal)
        {
            return principal.GetProperty("department");
        }
        public static string GetCompany(this Principal principal)
        {
            return principal.GetProperty("company");
        }

        public static bool SaveUser(this UserPrincipal User)
        {
            //try
            //{
            //    DirectoryEntry usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
            //    int val = (int)usr.Properties["userAccountControl"].Value;
            //    usr.Properties["userAccountControl"].Value = val | 0x2;
            //    //ADS_UF_ACCOUNTDISABLE

            //    usr.CommitChanges();
            //    usr.Close();
            //}
            //catch (System.DirectoryServices.DirectoryServicesCOMException E)
            //{
            //    System.Windows.Forms.MessageBox.Show(
            //            "There was an error:\n\n" + E.Message.ToString(),
            //            "Error",
            //            MessageBoxButtons.OK,
            //            MessageBoxIcon.Asterisk);
            //    return false;
            //}
            //return true;
            return false;
        }
        public static bool EnableUser(this UserPrincipal User)
        {
            try
            {
                DirectoryEntry usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                int val = (int)usr.Properties["userAccountControl"].Value;
                usr.Properties["userAccountControl"].Value = val & ~0x2;
                //ADS_UF_ACCOUNTDISABLE

                usr.CommitChanges();
                usr.Close();
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                System.Windows.Forms.MessageBox.Show(
                        "There was an error:\n\n" + E.Message.ToString(),
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk);
                return false;
            }
            return true;
        }
        public static bool DisableUser(this UserPrincipal User)
        {
            try
            {
                DirectoryEntry usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                int val = (int)usr.Properties["userAccountControl"].Value;
                usr.Properties["userAccountControl"].Value = val | 0x2;
                //ADS_UF_ACCOUNTDISABLE

                usr.CommitChanges();
                usr.Close();
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                System.Windows.Forms.MessageBox.Show(
                        "There was an error:\n\n" + E.Message.ToString(),
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk);
                return false;
            }
            return true;
        }
    }
}
