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

namespace Unified_Systems.Settings
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
            ADdomainTextBox.Text = ActiveDirectory.DomainName;
            ADuserTextBox.Text = ActiveDirectory.AuthenticatingUsername;
            ADpassPasswordBox.Password = ActiveDirectory.AuthenticatingPassword;
        }

        /* Primary Action */
        /// <summary>
        /// Saves any changes made to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool confirmAction = false;
        private void saveLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultMouseDownLabelButtonStyle;
            resultMessage.Visibility = Visibility.Hidden;

            if (confirmAction)
            {
                curtain.Visibility = Visibility.Visible;
                confirmMessage.Content = "Are you sure you want to save changes?";
                confirmYesLabelButton.Content = "Save Changes";
                confirmMessage.Visibility = Visibility.Visible;
                confirmYesLabelButton.Visibility = Visibility.Visible;
                confirmYesLabelButton.IsEnabled = true;
                confirmNoLabelButton.Visibility = Visibility.Visible;
                confirmNoLabelButton.IsEnabled = true;
            }
            else
            {
                if (saveSettings())
                {
                    resultMessage.Content = "Saved Successfully";
                    resultMessage.Visibility = Visibility.Visible;
                }
                saveLabelButton.IsEnabled = true;
            }
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
        /// <summary>
        /// Warning and confirmation template if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void confirmYesLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

            if (saveSettings())
            {
                resultMessage.Content = "Saved Successfully";
                resultMessage.Visibility = Visibility.Visible;
            }
            saveLabelButton.IsEnabled = true;
        }
        private void confirmNoLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

        }
        private void confirmLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }
        private void confirmLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }
        private bool saveSettings()
        {
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
    }
}
