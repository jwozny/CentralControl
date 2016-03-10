using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Text;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
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

            _NavigationFrame.Navigate(new Dashboard());
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

        private void Dashboard_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _NavigationFrame.Navigate(new Dashboard());
        }
        private void User_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _NavigationFrame.Navigate(new User.User());
        }
        private void UserCreate_MouseDown(object sender, MouseButtonEventArgs e)
        {
            //_NavigationFrame.Navigate(new User.Create());
        }
        private void UserEnable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _NavigationFrame.Navigate(new User.Enable());
        }
        private void UserDisable_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _NavigationFrame.Navigate(new User.Disable());
        }
    }
}
