using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.DirectoryServices.ActiveDirectory;

namespace Central_Control.Settings
{
    /// <summary>
    /// Interaction logic for Credentials.xaml
    /// </summary>
    public partial class Credentials : Page
    {
        public Credentials()
        {
            InitializeComponent();
        }
        Configuration configs = new Configuration();

        private void Credentials_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateCredentials();
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
                saveLabelButton.Style = defaultMouseDownLabelButtonStyle;

                saveSettings();
            }
        }
        private void ADpassPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
                saveLabelButton.Style = defaultMouseDownLabelButtonStyle;

                saveSettings();
            }
        }
        
        private void resetLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultMouseDownLabelButtonStyle;

            PopulateCredentials();
        }
        private void resetLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultLabelButtonStyle;
        }
        private void resetLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            resetLabelButton.Style = defaultLabelButtonStyle;
        }

        private void saveLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultMouseDownLabelButtonStyle;
            resultMessage.Visibility = Visibility.Hidden;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            if (saveSettings())
            {
                resultMessage.Content = "Saved Successfully";
                resultMessage.Visibility = Visibility.Visible;
            }
            saveLabelButton.IsEnabled = true;
        }
        private void saveLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultLabelButtonStyle;
        }
        private void saveLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultLabelButtonStyle;
        }

        private bool saveSettings()
        {
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
                    try
                    {
                        ADdomainTextBox.Text = Domain.GetComputerDomain().ToString();
                    }
                    catch
                    {
                        ADdomainTextBox.Text = null;
                    }
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
                        return false;
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
            catch
            {
                return false;
            }
            ConfigActions.SaveConfig(configs);
            return true;
        }

        /// <summary>
        /// Hide result message with mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resultMessage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //resultMessage.Visibility = Visibility.Hidden;
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
    }
}
