using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Threading;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.DirectoryServices.ActiveDirectory;

namespace Unified_Systems.User
{
    /// <summary>
    /// Interaction logic for User.xaml
    /// </summary>
    public partial class User : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public User()
        {
            InitializeComponent();
        }
        private void User_Loaded(object sender, RoutedEventArgs e)
        {
            if (ActiveDirectory.IsConnected)
            {
                Continue();
            }
            else
            {
                ActiveDirectory.Connect();
                ActiveDirectory.ConnectAD.RunWorkerCompleted += ConnectAD_Completed;

                credInputMessage.Foreground = Brushes.LightSkyBlue;
                credInputMessage.Text = "Connecting...";
                credInputMessage.Visibility = Visibility.Visible;
                showCredInput();
                disableCredInput();
            }
        }

        private void PopulateCredentials()
        {
            if (!string.IsNullOrEmpty(ActiveDirectory.DomainName))
            {
                ADdomainTextBox.Text = ActiveDirectory.DomainName;
                ADdomainCheckbox.IsChecked = false;
            }
            else
            {
                ADdomainCheckbox.IsChecked = true;
            }

            if (!string.IsNullOrEmpty(ActiveDirectory.AuthenticatingUsername) && !string.IsNullOrEmpty(ActiveDirectory.AuthenticatingPassword))
            {
                ADuserTextBox.Text = ActiveDirectory.AuthenticatingUsername;
                ADpassPasswordBox.Password = ActiveDirectory.AuthenticatingPassword;
                ADcredentialsCheckbox.IsChecked = false;
            }
            else
            {
                ADcredentialsCheckbox.IsChecked = true;
            }
        }
        private void showCredInput()
        {
            curtain.Visibility = Visibility.Visible;
            credInputs.Visibility = Visibility.Visible;

            PopulateCredentials();
        }
        private void hideCredInput()
        {
            curtain.Visibility = Visibility.Hidden;
            credInputs.Visibility = Visibility.Hidden;
            credInputMessage.Visibility = Visibility.Collapsed;
        }
        private void disableCredInput()
        {
            credInputcurtain.Visibility = Visibility.Visible;
        }
        private void enableCredInput()
        {
            credInputcurtain.Visibility = Visibility.Hidden;
        }

        private void ADdomainTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ADdomainTextBox.SelectAll();
        }
        private void ADuserTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ADuserTextBox.SelectAll();
        }
        private void ADpassPasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ADpassPasswordBox.SelectAll();
        }

        private void ADdomainTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
                saveCredLabelButton.Style = defaultMouseDownLabelButtonStyle;

                saveCred();
            }
        }
        private void ADpassPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
                saveCredLabelButton.Style = defaultMouseDownLabelButtonStyle;

                saveCred();
            }
        }

        private void resetLabelButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultMouseDownLabelButtonStyle;

            PopulateCredentials();
        }
        private void resetLabelButton_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultLabelButtonStyle;
        }
        private void resetLabelButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultLabelButtonStyle;
        }

        private void saveCredLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveCredLabelButton.Style = defaultMouseDownLabelButtonStyle;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            saveCred();
        }
        private void saveCredLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveCredLabelButton.Style = defaultLabelButtonStyle;
        }
        private void saveCredLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveCredLabelButton.Style = defaultLabelButtonStyle;
        }

        private void saveCred()
        {
            Configuration configs = new Configuration();
            try
            {
                if (ADdomainCheckbox.IsChecked == false)
                {
                    if (string.IsNullOrEmpty(ADdomainTextBox.Text))
                    {
                        ActiveDirectory.DomainName = null;
                        configs.DomainName = null;
                    }
                    else
                    {
                        ActiveDirectory.DomainName = ADdomainTextBox.Text;
                        configs.DomainName = ADdomainTextBox.Text;
                    }
                }
                else
                {
                    ActiveDirectory.DomainName = null;
                    configs.DomainName = null;
                    ADdomainTextBox.Text = Domain.GetComputerDomain().ToString();
                }

                if (ADcredentialsCheckbox.IsChecked == false)
                {
                    if (string.IsNullOrEmpty(ADuserTextBox.Text) ^ string.IsNullOrEmpty(ADpassPasswordBox.Password))
                    {
                        if (string.IsNullOrEmpty(ADuserTextBox.Text))
                        {
                            Style errorTextBoxStyle = FindResource("errorTextBoxStyle") as Style;
                            ADuserTextBox.Style = errorTextBoxStyle;
                        }
                        if (string.IsNullOrEmpty(ADpassPasswordBox.Password))
                        {
                            Style errorPasswordBoxStyle = FindResource("errorPasswordBoxStyle") as Style;
                            ADpassPasswordBox.Style = errorPasswordBoxStyle;
                        }
                        return;
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(ADuserTextBox.Text))
                        {
                            ActiveDirectory.AuthenticatingUsername = null;
                            configs.ADUsername = null;
                        }
                        else
                        {
                            ActiveDirectory.AuthenticatingUsername = ADuserTextBox.Text;
                            configs.ADUsername = ADuserTextBox.Text;
                        }
                        Style defaultTextBoxStyle = FindResource("defaultTextBoxStyle") as Style;
                        ADuserTextBox.Style = defaultTextBoxStyle;

                        if (string.IsNullOrEmpty(ADpassPasswordBox.Password))
                        {
                            ActiveDirectory.AuthenticatingPassword = null;
                            configs.ADPassword = null;
                        }
                        else
                        {
                            ActiveDirectory.AuthenticatingPassword = ADpassPasswordBox.Password;
                            configs.ADPassword = ADpassPasswordBox.Password;
                        }
                        Style defaultPasswordBoxStyle = FindResource("defaultPasswordBoxStyle") as Style;
                        ADpassPasswordBox.Style = defaultPasswordBoxStyle;
                    }
                }
                else
                {
                    ActiveDirectory.AuthenticatingUsername = null;
                    configs.ADUsername = null;
                    ADuserTextBox.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                    ActiveDirectory.AuthenticatingPassword = null;
                    configs.ADPassword = null;
                    ADpassPasswordBox.Password = string.Empty;
                }

                PopulateCredentials();
            }
            catch { }
            ConfigActions.SaveConfig(configs);

            credInputMessage.Foreground = Brushes.LightSkyBlue;
            credInputMessage.Text = "Connecting...";
            ActiveDirectory.Connect();
        }

        private void ADdomainCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ADdomainTextBox.IsEnabled = false;

            try
            {
                ADdomainTextBox.Text = Domain.GetComputerDomain().ToString();
            }
            catch
            {
                ADdomainTextBox.Text = null;
            }
        }
        private void ADdomainCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ADdomainTextBox.IsEnabled = true;

            if (!string.IsNullOrEmpty(ActiveDirectory.DomainName))
            {
                ADdomainTextBox.Text = ActiveDirectory.DomainName;
            }
            else
            {
                ADdomainTextBox.Text = string.Empty;
            }
            ADdomainTextBox.Focus();
        }
        private void ADcredentialsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ADuserTextBox.IsEnabled = false;
            ADpassPasswordBox.IsEnabled = false;

            ADuserTextBox.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            ADpassPasswordBox.Password = string.Empty;
        }
        private void ADcredentialsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ADuserTextBox.IsEnabled = true;
            ADpassPasswordBox.IsEnabled = true;

            if (!string.IsNullOrEmpty(ActiveDirectory.AuthenticatingUsername) && !string.IsNullOrEmpty(ActiveDirectory.AuthenticatingPassword))
            {
                ADuserTextBox.Text = ActiveDirectory.AuthenticatingUsername;
                ADpassPasswordBox.Password = ActiveDirectory.AuthenticatingPassword;
            }
            else
            {
                ADuserTextBox.Text = string.Empty;
                ADpassPasswordBox.Password = string.Empty;
            }

            ADuserTextBox.Focus();
        }

        public event EventHandler ConnectionVerified;
        private void ConnectAD_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            enableCredInput();
            if (ActiveDirectory.IsConnected)
            {
                Continue();
            }
            else
            {
                var converter = new System.Windows.Media.BrushConverter();
                var brush = (Brush)converter.ConvertFromString("#FFFF8080");
                credInputMessage.Foreground = brush;
                credInputMessage.Text = ActiveDirectory.ConnectionError;
                credInputMessage.Visibility = Visibility.Visible;
            }
        }
        private void Continue()
        {
            ConnectionVerified(this, new EventArgs());
        }
        private void User_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.ConnectAD.RunWorkerCompleted -= ConnectAD_Completed;
        }
    }
}