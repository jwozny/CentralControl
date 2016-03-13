using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
        private static int numberOfWorkers = 10;
        public static int[] closePrevention = new int[numberOfWorkers];

        public MainWindow()
        {
            InitializeComponent();
            SourceInitialized += new EventHandler(MainWindow_SourceInitialized);

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
        private void UnifiedSystems_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (RSATneeded) installRSAT.Visibility = Visibility.Visible;
        }
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
        private void UnifiedSystems_Closing(object sender, CancelEventArgs e)
        {
            bool taskRunning = false;
            for (int i = 0; i < numberOfWorkers; i++)
            {
                if (closePrevention[i] != 0)
                {
                    taskRunning = true;
                }
            }
            if (taskRunning)
            {
                if (System.Windows.Forms.MessageBox.Show(
                                "There is a background operation in progress.\nYou may do some serious damage if you close now.\n\nAre you sure you want to close?",
                                "Operation in progress",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Asterisk) == System.Windows.Forms.DialogResult.Yes)
                {
                    e.Cancel = false;
                    if(installWorker.IsBusy) installWorker.CancelAsync();
                }
                else
                {
                    e.Cancel = true;
                }
            }
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
        private void installRSAT_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RSATneeded = false;
            installRSAT.Visibility = Visibility.Hidden;
            installRSATStatus.Visibility = Visibility.Visible;
            installRSATStatusText.Visibility = Visibility.Visible;

            installWorker_Initialize();

            bool waiting = true;
            while (waiting)
            {
                if (installWorker.IsBusy != true)
                {
                    installWorker.RunWorkerAsync();
                    waiting = false;
                }
                else
                {
                    Thread.Sleep(200);
                    waiting = true;
                }
            }
        }

        /* Background Command Worker */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        private static BackgroundWorker installWorker = new BackgroundWorker();
        private static Exception installResults;
        public static bool RSATneeded = false;
        private static bool RSATinstalled = false;

        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        private void installWorker_Initialize()
        {
            installWorker.WorkerReportsProgress = true;
            installWorker.WorkerSupportsCancellation = true;
            installWorker.DoWork += installWorker_DoWork;
            installWorker.ProgressChanged += installWorker_ProgressChanged;
            installWorker.RunWorkerCompleted += installWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void installWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainWindow.closePrevention[0] = 1;

            installResults = null;
            Runspace runspace = null;
            Pipeline pipeline = null;

            installWorker.ReportProgress(0); //Detecting OS
            string url = String.Empty;
            string installCommand_winrs = " ";
            if (Environment.OSVersion.ToString().Contains("10.0") || Environment.OSVersion.ToString().Contains("6.3") || Environment.OSVersion.ToString().Contains("6.2"))
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    url = "https://download.microsoft.com/download/1/D/8/1D8B5022-5477-4B9A-8104-6A71FF9D98AB/WindowsTH-KB2693643-x64.msu";
                }
                else
                {
                    url = "https://download.microsoft.com/download/1/D/8/1D8B5022-5477-4B9A-8104-6A71FF9D98AB/WindowsTH-KB2693643-x86.msu";
                }
                installCommand_winrs = "winrs.exe -r:localhost dism.exe /online /add-package /PackagePath:C:\\Temp\\KB2693643.cab";
            }
            else if (Environment.OSVersion.ToString().Contains("6.1"))
            {
                if (Environment.Is64BitOperatingSystem)
                {
                    url = "https://download.microsoft.com/download/4/F/7/4F71806A-1C56-4EF2-9B4F-9870C4CFD2EE/Windows6.1-KB958830-x64-RefreshPkg.msu";
                }
                else
                {
                    url = "https://download.microsoft.com/download/4/F/7/4F71806A-1C56-4EF2-9B4F-9870C4CFD2EE/Windows6.1-KB958830-x86-RefreshPkg.msu";
                }
                installCommand_winrs = "winrs.exe -r:localhost dism.exe /online /add-package /PackagePath:C:\\Temp\\KB958830.cab";
            }
            else if (Environment.OSVersion.ToString().Contains("6.0"))
            {
                url = "https://download.microsoft.com/download/3/0/1/301EC38B-D8BD-40CD-A3B8-3A514A553BE8/Windows6.0-KB941314-x86_en-US.msu";
                installCommand_winrs = "winrs.exe -r:localhost dism.exe /online /add-package /PackagePath:C:\\Temp\\KB941314.cab";
            }
            Thread.Sleep(1000);

            string importModuleCommand = "Import-Module BitsTransfer";
            string downloadCommand = "Start-BitsTransfer -Source \"" + url + "\" -Destination \"C:\\Temp\\rsat.msu\"";
            string extractCommand_winrs = "winrs.exe -r:localhost wusa.exe \"C:\\Temp\\rsat.msu\" /extract:C:\\Temp";

            string installCommand_wusa = "wusa.exe C:\\Temp\\rsat.msu /quiet /norestart"; //This might actually be working, just needs a reboot to take effect...
            string installCommand = "C:\\Temp\\rsat.msu";

            installWorker.ReportProgress(5); //Initiating Directory
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript("mkdir C:\\Temp");
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(10); //Importing Download Module
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(importModuleCommand);
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(20); //Downloading RSAT Installer
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(downloadCommand);
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(60); //Extracting RSAT
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(extractCommand_winrs);
                //pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(65); //Installing RSAT
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(installCommand_wusa);
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(10000);

            installWorker.ReportProgress(85); //Removing RSAT Installer
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript("del C:\\Temp\\rsat.msu");
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(95); //Testing RSAT
            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript("Get-ADUser -Filter *");
                pipeline.Invoke();
            }
            catch (Exception exception)
            {
                installResults = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            if (installResults != null) return;
            Thread.Sleep(1000);

            installWorker.ReportProgress(100); //Complete
            RSATinstalled = true;
            Thread.Sleep(1000);
        }
        private void installWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            installRSATStatus.Value = e.ProgressPercentage;
            switch (e.ProgressPercentage)
            {
                case 0:
                    installRSATStatusText.Text = "Detecting OS";
                    break;
                case 5:
                    installRSATStatusText.Text = "Initiating Directory";
                    break;
                case 10:
                    installRSATStatusText.Text = "Importing Download Module";
                    break;
                case 20:
                    installRSATStatusText.Text = "Downloading RSAT Installer";
                    break;
                case 60:
                    installRSATStatusText.Text = "Extracting RSAT";
                    break;
                case 65:
                    installRSATStatusText.Text = "Installing RSAT";
                    break;
                case 85:
                    installRSATStatusText.Text = "Removing RSAT Installer";
                    break;
                case 95:
                    installRSATStatusText.Text = "Testing RSAT";
                    break;
                case 100:
                    installRSATStatusText.Text = "Complete";
                    break;
                default:
                    installRSATStatusText.Text = String.Empty;
                    break;
            }
        }
        private void installWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            installRSATStatus.Visibility = Visibility.Hidden;
            installRSATStatusText.Visibility = Visibility.Hidden;
            if (installResults != null)
            {
                if (installResults.Message.Contains("The term 'Get-ADUser' is not recognized as the name of a cmdlet"))
                {
                    installRSAT.Visibility = Visibility.Hidden;
                    System.Windows.Forms.MessageBox.Show(
                                "The system needs to reboot to complete RSAT installation",
                                "Reboot Required",
                                MessageBoxButtons.OK);
                    RSATneeded = true;
                }
            }
            else if (!RSATinstalled)
            {
                installRSAT.Content = "Retry Installing RSAT";
                installRSAT.Visibility = Visibility.Visible;
                System.Windows.Forms.MessageBox.Show(
                            "There was an error installing RSAT automatically...\n\n" + installResults.Message.ToString(),
                            "RSAT Installation Failed",
                            MessageBoxButtons.OK);
                RSATneeded = true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(
                            "Remote Server Administrive Tools installed successfully!",
                            "RSAT Installation Succeeded",
                            MessageBoxButtons.OK);
                            RSATneeded = false;
            }
            MainWindow.closePrevention[0] = 0;
        }
    }

    /// <summary>
    /// Static class to store active directory user info collection and action scripts
    /// </summary>
    public static class ActiveDirectory
    {
        private static Collection<PSObject> users;
        public static Collection<PSObject> Users
        {
            get
            {
                return users;
            }
            set
            {
                users = value;
            }
        }

        public static Exception ExecutePowershell(string Command)
        {
            Runspace runspace = null;
            Pipeline pipeline = null;
            Exception results = null;

            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(Command);
                users = pipeline.Invoke();
            }
            catch (Exception exception)
            {
                results = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            return results;
        }

        public static Exception GetAllUsers()
        {
            return ExecutePowershell("Get-ADUser -Filter * -Properties * | Sort-Object SamAccountName");
        }
        public static Exception EnableUser(string User)
        {
            return ExecutePowershell("Enable-ADAccount -Identity " + User);
        }
        public static Exception DisableUser(string User)
        {
            return ExecutePowershell("Disable-ADAccount -Identity " + User);
        }
    }
}
