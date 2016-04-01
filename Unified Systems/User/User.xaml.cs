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
            if (ActiveDirectory.Connect())
            {
                Continue();
            }
            else
            {
                credInputMessage.Text = ActiveDirectory.ConnectionError;
                credInputMessage.Visibility = Visibility.Visible;
                showCredInput();
            }
        }

        private void PopulateCredentials()
        {
            if (!String.IsNullOrEmpty(ActiveDirectory.DomainName))
            {
                ADdomainTextBox.Text = ActiveDirectory.DomainName;
                ADdomainCheckbox.IsChecked = false;
            }
            else
            {
                ADdomainCheckbox.IsChecked = true;
            }

            if (!String.IsNullOrEmpty(ActiveDirectory.AuthenticatingUsername) && !String.IsNullOrEmpty(ActiveDirectory.AuthenticatingPassword))
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

        private void ADpassPasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
                saveCredLabelButton.Style = defaultMouseDownLabelButtonStyle;

                saveCred();
            }
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
                    if (String.IsNullOrEmpty(ADdomainTextBox.Text))
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
                    if (String.IsNullOrEmpty(ADuserTextBox.Text) ^ String.IsNullOrEmpty(ADpassPasswordBox.Password))
                    {
                        if (String.IsNullOrEmpty(ADuserTextBox.Text))
                        {
                            Style errorTextBoxStyle = FindResource("errorTextBoxStyle") as Style;
                            ADuserTextBox.Style = errorTextBoxStyle;
                        }
                        if (String.IsNullOrEmpty(ADpassPasswordBox.Password))
                        {
                            Style errorPasswordBoxStyle = FindResource("errorPasswordBoxStyle") as Style;
                            ADpassPasswordBox.Style = errorPasswordBoxStyle;
                        }
                        return;
                    }
                    else
                    {
                        if (String.IsNullOrEmpty(ADuserTextBox.Text))
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

                        if (String.IsNullOrEmpty(ADpassPasswordBox.Password))
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
                    ADpassPasswordBox.Password = String.Empty;
                }

                PopulateCredentials();
            }
            catch { }
            ConfigActions.SaveConfig(configs);

            if (ActiveDirectory.Connect())
            {
                Continue();
            }
            else
            {
                credInputMessage.Text = ActiveDirectory.ConnectionError;
                credInputMessage.Visibility = Visibility.Visible;
            }
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

            ADdomainTextBox.Text = String.Empty;
        }
        private void ADcredentialsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            ADuserTextBox.IsEnabled = false;
            ADpassPasswordBox.IsEnabled = false;

            ADuserTextBox.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            ADpassPasswordBox.Password = String.Empty;
        }
        private void ADcredentialsCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            ADuserTextBox.IsEnabled = true;
            ADpassPasswordBox.IsEnabled = true;

            ADuserTextBox.Text = String.Empty;
            ADpassPasswordBox.Password = String.Empty;
        }

        public event EventHandler ConnectionVerified;
        private void Continue()
        {
            ConnectionVerified(this, new EventArgs());
        }
    }
}