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
        /// <summary>
        /// Primary function
        /// </summary>
        public Credentials()
        {
            InitializeComponent();
        }
        /// <summary>
        /// Event handler when the page finishes loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Credentials_Loaded(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        /* Select all current text when textbox gets focus */
        /// <summary>
        /// Select all text in the AD Domain box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_DomainTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_DomainTextBox.SelectAll();
        }
        /// <summary>
        /// Select all text in the AD Username box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_UsernameTextBox.SelectAll();
        }
        /// <summary>
        /// Select all text in the AD password box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_PasswordBox.SelectAll();
        }

        /* Checkbox actions*/
        /// <summary>
        /// Disable the domain box and assign null to the global config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_LocalDomainCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            AD_DomainTextBox.IsEnabled = false;
            GlobalConfig.Settings.AD_UseLocalDomain = true;

            try
            {
                AD_DomainTextBox.Text = Domain.GetComputerDomain().ToString();
            }
            catch
            {
                AD_DomainTextBox.Text = null;
            }
        }
        /// <summary>
        /// Enable the domain box and populate it if the info is in the global config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_LocalDomainCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            AD_DomainTextBox.IsEnabled = true;
            GlobalConfig.Settings.AD_UseLocalDomain = false;

            if (!string.IsNullOrEmpty(GlobalConfig.Settings.AD_Domain))
            {
                AD_DomainTextBox.Text = GlobalConfig.Settings.AD_Domain;
            }
            else
            {
                AD_DomainTextBox.Text = string.Empty;
            }
            AD_DomainTextBox.Focus();
        }
        /// <summary>
        /// Disable the username and password boxes and assign null to the global config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_LocalAuthCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            AD_UsernameTextBox.IsEnabled = false;
            AD_PasswordBox.IsEnabled = false;
            GlobalConfig.Settings.AD_UseLocalAuth = true;

            AD_UsernameTextBox.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            AD_PasswordBox.Password = string.Empty;
        }
        /// <summary>
        /// Enable the username and password boxes and populate them if the info is in the global config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_LocalAuthCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            AD_UsernameTextBox.IsEnabled = true;
            AD_PasswordBox.IsEnabled = true;
            GlobalConfig.Settings.AD_UseLocalAuth = false;

            if (!string.IsNullOrEmpty(GlobalConfig.Settings.AD_Username) && !string.IsNullOrEmpty(GlobalConfig.Settings.AD_Password))
            {
                AD_UsernameTextBox.Text = GlobalConfig.Settings.AD_Username;
                AD_PasswordBox.Password = GlobalConfig.Settings.AD_Password;
            }
            else
            {
                AD_UsernameTextBox.Text = string.Empty;
                AD_PasswordBox.Password = string.Empty;
            }

            AD_UsernameTextBox.Focus();
        }

        /* Button actions*/
        /// <summary>
        /// Button action to reset settings and repopulate from configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetButton.IsEnabled = false;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            LoadSettings();

            ResetButton.IsEnabled = true;
        }
        /// <summary>
        /// Button action to save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            ResultBox.Visibility = Visibility.Hidden;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            if (SaveSettings())
            {
                ResultMessage.Text = "Saved Successfully";
                ResultBox.Visibility = Visibility.Visible;
            }

            SaveButton.IsEnabled = true;
        }

        /* Settings functions */
        /// <summary>
        /// Get credentials from configuration and display in the page (passwords don't display)
        /// </summary>
        private void LoadSettings()
        {
            if (GlobalConfig.Settings.AD_UseLocalDomain)
            {
                AD_LocalDomainCheckbox.IsChecked = true;
            }
            else
            {
                AD_DomainTextBox.Text = GlobalConfig.Settings.AD_Domain;
                AD_LocalDomainCheckbox.IsChecked = false;
            }

            if (GlobalConfig.Settings.AD_UseLocalAuth)
            {
                AD_LocalAuthCheckbox.IsChecked = true;
            }
            else
            {
                AD_UsernameTextBox.Text = GlobalConfig.Settings.AD_Username;
                AD_PasswordBox.Password = GlobalConfig.Settings.AD_Password;
                AD_LocalAuthCheckbox.IsChecked = false;
            }
        }
        /// <summary>
        /// Save settings into configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool SaveSettings()
        {
            try
            {
                // Save AD domain to global config
                if (AD_LocalDomainCheckbox.IsChecked == false)
                {
                    if (string.IsNullOrEmpty(AD_DomainTextBox.Text))
                    {
                        GlobalConfig.Settings.AD_Domain = null;
                    }
                    else
                    {
                        GlobalConfig.Settings.AD_Domain = AD_DomainTextBox.Text;
                    }
                }
                else
                {
                    // Mark to use local domain
                    GlobalConfig.Settings.AD_UseLocalDomain = true;

                    GlobalConfig.Settings.AD_Domain = null;
                    try
                    {
                        AD_DomainTextBox.Text = Domain.GetComputerDomain().ToString();
                    }
                    catch
                    {
                        AD_DomainTextBox.Text = null;
                    }
                }

                // Save AD credentials to global config
                if (AD_LocalAuthCheckbox.IsChecked == false)
                {
                    if (string.IsNullOrEmpty(AD_UsernameTextBox.Text) ^ string.IsNullOrEmpty(AD_PasswordBox.Password))
                    {
                        // Missing AD username in textbox
                        if (string.IsNullOrEmpty(AD_UsernameTextBox.Text))
                        {
                            Style TextBox_Error = FindResource("TextBox_Error") as Style;
                            AD_UsernameTextBox.Style = TextBox_Error;
                        }
                        // Missing AD password in textbox
                        if (string.IsNullOrEmpty(AD_PasswordBox.Password))
                        {
                            Style PasswordBox_Error = FindResource("PasswordBox_Error") as Style;
                            AD_PasswordBox.Style = PasswordBox_Error;
                        }
                        // Cancel save
                        return false;
                    }
                    else
                    {
                        // Save AD username to global config
                        if (string.IsNullOrEmpty(AD_UsernameTextBox.Text))
                        {
                            GlobalConfig.Settings.AD_Username = null;
                        }
                        else
                        {
                            GlobalConfig.Settings.AD_Username = AD_UsernameTextBox.Text;
                        }
                        Style TextBox = FindResource("TextBox") as Style;
                        AD_UsernameTextBox.Style = TextBox;

                        // Save AD pasword to global config
                        if (string.IsNullOrEmpty(AD_PasswordBox.Password))
                        {
                            GlobalConfig.Settings.AD_Password = null;
                        }
                        else
                        {
                            GlobalConfig.Settings.AD_Password = AD_PasswordBox.Password;
                        }
                        Style PasswordBox = FindResource("PasswordBox") as Style;
                        AD_PasswordBox.Style = PasswordBox;
                    }
                }
                else
                {
                    // Mark to use local authentication
                    GlobalConfig.Settings.AD_UseLocalAuth = true;

                    // Remove saved AD username
                    GlobalConfig.Settings.AD_Username = null;
                    AD_UsernameTextBox.Text = System.Security.Principal.WindowsIdentity.GetCurrent().Name;

                    // Remove saved AD password
                    GlobalConfig.Settings.AD_Password = null;
                    AD_PasswordBox.Password = string.Empty;
                }
            }
            catch
            {
                // Caught unexpected error , reload global config from disk and return false 
                GlobalConfig.LoadFromDisk();
                return false;
            }

            // Save global config
            GlobalConfig.SaveToDisk();
            return true;
        }
    }
}
