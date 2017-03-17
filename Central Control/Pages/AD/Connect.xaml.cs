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

namespace Central_Control.AD
{
    /// <summary>
    /// Interaction logic for Connect.xaml
    /// </summary>
    public partial class Connect : Page
    {
        public Connect()
        {
            InitializeComponent();
        }
        
        #region Page Events
        /// <summary>
        /// Event handler when the page finishes loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connect_Loaded(object sender, RoutedEventArgs e)
        {
            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            if (ActiveDirectory.IsConnected && !ActiveDirectory.Connector.IsBusy)
            {
                Continue();
            }
            else if (!ActiveDirectory.Connector.IsBusy)
            {
                FormReset();
                FormConnect();
            }
            else
            {
                ActiveDirectory.Connector.RunWorkerCompleted += Connector_Completed;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connector_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Style Message = FindResource("Message") as Style;
            Style Message_Success = FindResource("Message_Success") as Style;
            Style Progress = FindResource("Progress") as Style;
            Style Progress_Success = FindResource("Progress_Success") as Style;

            StatusMessage.Text = e.UserState.ToString();

            // Do this when progress is reported.
            if (e.UserState.ToString() == "Connecting…")
            {
                StatusProgress.IsIndeterminate = true;
                StatusProgress.Style = Progress;

                StatusMessage.Style = Message;
            }
            else
            {
                StatusProgress.IsIndeterminate = false;
                StatusProgress.Style = Progress_Success;

                StatusMessage.Style = Message_Success;
            }

            if (e.UserState.ToString() == "Finding Users")
            {
                StatusProgress.IsIndeterminate = true;
                StatusMessage.Text = "(1/6) " + e.UserState.ToString();
            }

            if (e.UserState.ToString() == "Retrieving User")
            {
                StatusProgress.Maximum = ActiveDirectory.UserCount;
                StatusProgress.Value = ActiveDirectory.Users.Count;

                StatusMessage.Text = "(2/6) " + e.UserState.ToString() + " " + StatusProgress.Value.ToString() + "/" + StatusProgress.Maximum.ToString();
            }

            if (e.UserState.ToString() == "Retrieving User Properties")
            {
                StatusProgress.Maximum = ActiveDirectory.Users.Count;
                StatusProgress.Value = ActiveDirectory.UserCount;

                StatusMessage.Text = "(3/6) " + e.UserState.ToString() + " " + StatusProgress.Value.ToString() + "/" + StatusProgress.Maximum.ToString();
            }

            if (e.UserState.ToString() == "Finding Groups")
            {
                StatusProgress.IsIndeterminate = true;
                StatusMessage.Text = "(4/6) " + e.UserState.ToString();
            }

            if (e.UserState.ToString() == "Retrieving Group")
            {
                StatusProgress.Maximum = ActiveDirectory.GroupCount;
                StatusProgress.Value = ActiveDirectory.Groups.Count;

                StatusMessage.Text = "(5/6) " + e.UserState.ToString() + " " + StatusProgress.Value.ToString() + "/" + StatusProgress.Maximum.ToString();
            }

            if (e.UserState.ToString() == "Retrieving Group Members")
            {
                StatusProgress.Maximum = ActiveDirectory.Groups.Count;
                StatusProgress.Value = ActiveDirectory.GroupCount;

                StatusMessage.Text = "(6/6) " + e.UserState.ToString() + " " + StatusProgress.Value.ToString() + "/" + StatusProgress.Maximum.ToString();
            }
        }
        /// <summary>
        /// Once the connection worker is complete, check if AD connected successfully and continue or display error message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connector_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            DisableCurtain();
            ActiveDirectory.Connector.RunWorkerCompleted -= Connector_Completed;
            ActiveDirectory.Connector.ProgressChanged -= Connector_ProgressChanged;

            if (ActiveDirectory.IsConnected)
            {
                Continue();
            }
            else
            {
                StatusProgress.Visibility = Visibility.Hidden;
                StatusMessage.Visibility = Visibility.Visible;
                StatusMessage.Text = ActiveDirectory.ConnectionError.Replace("\r\n", String.Empty);
                StatusMessage.Style = FindResource("Message_Error") as Style;

                if (ActiveDirectory.ConnectionError == "The local computer is not joined to a domain or the domain cannot be contacted.")
                {
                    AD_DomainTextBox.Style = FindResource("TextBox_Error") as Style;
                }
                else if (ActiveDirectory.ConnectionError == "The server is unavailable.")
                {
                    AD_DomainTextBox.Style = FindResource("TextBox_Error") as Style;
                }
                else if (ActiveDirectory.ConnectionError == "The user name or password is incorrect.\r\n")
                {
                    AD_UsernameTextBox.Style = FindResource("TextBox_Error") as Style;
                    AD_PasswordBox.Style = FindResource("PasswordBox_Error") as Style;
                }

            }
        }
        /// <summary>
        /// Event handler when the page unloads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connect_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Connector.RunWorkerCompleted -= Connector_Completed;
        }
        #endregion Page Events

        #region Common Functions
        /// <summary>
        /// Show a curtain over the form controls
        /// </summary>
        private void EnableCurtain()
        {
            Curtain.Visibility = Visibility.Visible;
            MainForm.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            ResetButton.Content = "Cancel";
        }
        /// <summary>
        /// Hide the curtain over the form controls
        /// </summary>
        private void DisableCurtain()
        {
            Curtain.Visibility = Visibility.Hidden;
            MainForm.IsEnabled = true;
            ConnectButton.IsEnabled = true;
            ResetButton.Content = "Reset";
        }
        /// <summary>
        /// Save credentials in the connection form
        /// </summary>
        private bool FormSave()
        {
            try
            {
                // Save AD domain to global config
                if (AD_LocalDomainCheckbox.IsChecked == false)
                {
                    GlobalConfig.Settings.AD_UseLocalDomain = false;

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
                }

                if (AD_LocalAuthCheckbox.IsChecked == false)
                {
                    GlobalConfig.Settings.AD_UseLocalAuth = false;

                    if ((string.IsNullOrEmpty(AD_UsernameTextBox.Text) ^ string.IsNullOrEmpty(AD_PasswordBox.Password)) || (string.IsNullOrEmpty(AD_UsernameTextBox.Text) && string.IsNullOrEmpty(AD_PasswordBox.Password)))
                    {
                        // Missing username in textbox
                        if (string.IsNullOrEmpty(AD_UsernameTextBox.Text))
                        {
                            Style TextBox_Error = FindResource("TextBox_Error") as Style;
                            AD_UsernameTextBox.Style = TextBox_Error;
                        }
                        // Missing password in textbox
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
                        // Save username to global config
                        GlobalConfig.Settings.AD_Username = AD_UsernameTextBox.Text;

                        // Save password to global config
                        GlobalConfig.Settings.AD_Password = AD_PasswordBox.Password;
                    }
                }
                else
                {
                    // Mark to use local authentication
                    GlobalConfig.Settings.AD_UseLocalAuth = true;

                    // Remove saved AD username
                    GlobalConfig.Settings.AD_Username = null;

                    // Remove saved AD password
                    GlobalConfig.Settings.AD_Password = null;
                }

                GlobalConfig.SaveToDisk();
                return true;
            }
            catch
            {
                // Caught unexpected error , reload global config from disk and return false 
                GlobalConfig.LoadFromDisk();
                return false;
            }
        }
        /// <summary>
        /// Use the connection form to connect to specified AD
        /// </summary>
        private void FormConnect()
        {
            EnableCurtain();

            ActiveDirectory.Connect();
            ActiveDirectory.Connector.ProgressChanged += Connector_ProgressChanged;
            ActiveDirectory.Connector.RunWorkerCompleted += Connector_Completed;

            StatusProgress.Visibility = Visibility.Visible;
            StatusMessage.Visibility = Visibility.Visible;

        }
        /// <summary>
        /// Reset the connection form
        /// </summary>
        private void FormReset()
        {
            StatusMessage.Visibility = Visibility.Collapsed;
            StatusProgress.Visibility = Visibility.Collapsed;

            AD_DomainTextBox.Style = FindResource("TextBox") as Style;
            AD_UsernameTextBox.Style = FindResource("TextBox") as Style;
            AD_PasswordBox.Style = FindResource("PasswordBox") as Style;

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
        #endregion Common Functions

        #region Control Actions

        #region Form
        /// <summary>
        /// Select all text in the domain box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_DomainTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_DomainTextBox.SelectAll();
            AD_DomainTextBox.Style = FindResource("TextBox") as Style;
        }
        /// <summary>
        /// Select all text in the username box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_UsernameTextBox.SelectAll();
            AD_UsernameTextBox.Style = FindResource("TextBox") as Style;
        }
        /// <summary>
        /// Select all text in the password box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            AD_PasswordBox.SelectAll();
            AD_PasswordBox.Style = FindResource("PasswordBox") as Style;
        }

        /// <summary>
        /// Disable the domain box and assign null to the global config
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_LocalDomainCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Style TextBox = FindResource("TextBox") as Style;
            AD_DomainTextBox.Style = TextBox;
            AD_DomainTextBox.IsEnabled = false;

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

        /// <summary>
        /// Checks for the {ENTER} key to run connect function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_DomainTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FormSave();
                FormConnect();
            }
        }
        /// <summary>
        /// Checks for the {ENTER} key to run the connect function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AD_PasswordBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                FormSave();
                FormConnect();
            }
        }
        #endregion Form

        #region Buttons
        /// <summary>
        /// Reset button action
        /// </summary>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetButton.IsEnabled = false;

            ActiveDirectory.Connector.CancelAsync();

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            AD_DomainTextBox.Style = FindResource("TextBox") as Style;
            AD_UsernameTextBox.Style = FindResource("TextBox") as Style;
            AD_PasswordBox.Style = FindResource("PasswordBox") as Style;

            FormReset();

            ResetButton.IsEnabled = true;
        }
        /// <summary>
        /// Connect button action
        /// </summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {

            ConnectButton.IsEnabled = false;
            
            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            AD_DomainTextBox.Style = FindResource("TextBox") as Style;
            AD_UsernameTextBox.Style = FindResource("TextBox") as Style;
            AD_PasswordBox.Style = FindResource("PasswordBox") as Style;

            FormSave();
            FormConnect();
        }
        #endregion Buttons

        #endregion Control Actions

        #region Event Handlers and Triggers
        /// <summary>
        /// Event handler to let the MainWindow know that connection has been made (switches menu options to expand and select Users option)
        /// </summary>
        public event EventHandler ConnectionVerified;
        /// <summary>
        /// Continue to the Users page after AD connection is made
        /// </summary>
        private void Continue()
        {
            ConnectionVerified(this, new EventArgs());
        }
        #endregion Event Handlers and Triggers
    }
}