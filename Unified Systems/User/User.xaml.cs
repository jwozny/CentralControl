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
        private void showCredInput()
        {
            curtain.Visibility = Visibility.Visible;
            credInputs.Visibility = Visibility.Visible;

            ADdomainTextBox.Text = ActiveDirectory.DomainName;
            ADuserTextBox.Text = ActiveDirectory.AuthenticatingUsername;
            ADpassPasswordBox.Password = ActiveDirectory.AuthenticatingPassword;
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

        public event EventHandler ConnectionVerified;
        private void Continue()
        {
            ConnectionVerified(this, new EventArgs());
        }
    }
}