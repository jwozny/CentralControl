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

namespace Unified_Systems.User
{
    /// <summary>
    /// Interaction logic for Disable.xaml
    /// </summary>
    public partial class Disable : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Disable()
        {
            InitializeComponent();
            syncWorker_Initialize();
            commandWorker_Initialize();

            if (ActiveDirectory.Users != null)
            {// If users are already synced, then build the list
                BuildList();
            }
            else if (!MainWindow.RSATneeded)
            {// Otherwise show the curtain and the sync button
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.IsEnabled = true;
                syncLabelButton.Visibility = Visibility.Visible;

                Style defaultSyncMouseDownLabelButtonStyle = FindResource("defaultSyncMouseDownLabelButtonStyle") as Style;
                syncLabelButton.Style = defaultSyncMouseDownLabelButtonStyle;

                if (syncWorker.IsBusy != true)
                {
                    syncLabelButton.Content = "Please Wait";
                    syncLabelButton.IsEnabled = false;
                    syncWorker.RunWorkerAsync();
                }
            }
            else
            {
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.Visibility = Visibility.Visible;
                syncLabelButton.Content = "Install RSAT to continue";
                syncLabelButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Initial AD user sync button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void syncLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultSyncMouseDownLabelButtonStyle = FindResource("defaultSyncMouseDownLabelButtonStyle") as Style;
            syncLabelButton.Style = defaultSyncMouseDownLabelButtonStyle;

            if (syncWorker.IsBusy != true)
            {
                syncLabelButton.Content = "Please Wait";
                syncLabelButton.IsEnabled = false;
                syncWorker.RunWorkerAsync();
            }
        }
        private void syncLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultSyncLabelButtonStyle = FindResource("defaultSyncLabelButtonStyle") as Style;
            //syncLabelButton.Style = defaultSyncLabelButtonStyle;
        }
        private void syncLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultSyncLabelButtonStyle = FindResource("defaultSyncLabelButtonStyle") as Style;
            //syncLabelButton.Style = defaultSyncLabelButtonStyle;
        }

        /// <summary>
        /// Clears and rebuilds the list
        /// </summary>
        private void BuildList()
        {
            userList.Items.Clear();
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    userList.Items.Add(User.Properties["SamAccountName"].Value.ToString());
                }
            }
        }

        /// <summary>
        /// Displays more info on the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (userList.SelectedItem != null)
                {
                    if (User.Properties["SamAccountName"].Value.ToString() == userList.SelectedItem.ToString())
                    {
                        infoLabel.Content = User.Properties["Name"].Value.ToString();

                        Username.Content = User.Properties["SamAccountName"].Value.ToString();

                        if (!ReferenceEquals(User.Properties["EmailAddress"].Value, null))
                        { Email.Content = User.Properties["EmailAddress"].Value.ToString(); }
                        else { Email.Content = " "; }

                        if (!ReferenceEquals(User.Properties["Title"].Value, null))
                        { Title.Content = User.Properties["Title"].Value.ToString(); }
                        else { Title.Content = " "; }

                        if (!ReferenceEquals(User.Properties["Department"].Value, null))
                        { Department.Content = User.Properties["Department"].Value.ToString(); }
                        else { Department.Content = " "; }

                        if (!ReferenceEquals(User.Properties["Company"].Value, null))
                        { Company.Content = User.Properties["Company"].Value.ToString(); }
                        else { Company.Content = " "; }

                        CreatedDate.Content = User.Properties["Created"].Value.ToString();

                        if (!ReferenceEquals(User.Properties["AccountExpirationDate"].Value, null))
                        { ExpiryDate.Content = User.Properties["AccountExpirationDate"].Value.ToString(); }
                        else { ExpiryDate.Content = " "; }

                        if (!ReferenceEquals(User.Properties["LastLogonDate"].Value, null))
                        { LastLogonDate.Content = User.Properties["LastLogonDate"].Value.ToString(); }
                        else { LastLogonDate.Content = " "; }

                        if (!ReferenceEquals(User.Properties["LastBadPasswordAttempt"].Value, null))
                        { LastBadPasswordAttempt.Content = User.Properties["LastBadPasswordAttempt"].Value.ToString(); }
                        else { LastBadPasswordAttempt.Content = " "; }

                        if (!ReferenceEquals(User.Properties["LockedOut"].Value, null))
                        { LockedOut.Content = User.Properties["LockedOut"].Value.ToString(); }
                        else { LockedOut.Content = " "; }

                        if (!ReferenceEquals(User.Properties["AccountLockoutTime"].Value, null))
                        { AccountLockoutTime.Content = User.Properties["AccountLockoutTime"].Value.ToString(); }
                        else { AccountLockoutTime.Content = " "; }
                    }
                }
                else
                {
                    infoLabel.Content = "User Information";
                    Username.Content = String.Empty;
                    Email.Content = String.Empty;
                    Title.Content = String.Empty;
                    Department.Content = String.Empty;
                    Company.Content = String.Empty;
                    CreatedDate.Content = String.Empty;
                    ExpiryDate.Content = String.Empty;
                    LastLogonDate.Content = String.Empty;
                    LastBadPasswordAttempt.Content = String.Empty;
                    LockedOut.Content = String.Empty;
                    AccountLockoutTime.Content = String.Empty;
                }
            }
            userList.ScrollIntoView(userList.SelectedItem);
        }

        /* Search Functions */
        private int searchResult;
        private int searchCount;

        /// <summary>
        /// List search from lookupText
        /// </summary>
        private void quickLookup()
        {
            foreach (string user in userList.Items)
            {
                if (user == lookupText.ToString())
                {
                    userList.SelectedItem = user;
                    return;
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (User.Properties["Name"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                        return;
                    }
                }
            }
            foreach (string user in userList.Items)
            {
                if (user.IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    userList.SelectedItem = user;
                    return;
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (lookupText.Text.ToString().IndexOf("@", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!ReferenceEquals(User.Properties["EmailAddress"].Value, null))
                    {
                        if (User.Properties["EmailAddress"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            searchResult++;
                            if (searchResult == searchCount)
                            {
                                userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                                return;
                            }
                        }
                    }
                }
                }
            }
            userList.SelectedIndex = -1;
        }
        private void fullLookup()
        {
            searchResult = 0;
            foreach (string user in userList.Items)
            {
                if (user == lookupText.ToString())
                {
                    searchResult++;
                    if (searchResult == searchCount)
                    {
                        userList.SelectedItem = user;
                        int selected = userList.SelectedIndex;
                        return;
                    }
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (User.Properties["Name"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    searchResult++;
                    if (searchResult == searchCount)
                    {
                        userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                        return;
                    }
                }
                }
            }
            foreach (string user in userList.Items)
            {
                if (user.IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    searchResult++;
                    if (searchResult == searchCount)
                    {
                        userList.SelectedItem = user;
                        return;
                    }
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (lookupText.Text.ToString().IndexOf("@", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!ReferenceEquals(User.Properties["EmailAddress"].Value, null))
                    {
                        if (User.Properties["EmailAddress"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            searchResult++;
                            if (searchResult == searchCount)
                            {
                                userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                                return;
                            }
                        }
                    }
                }
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (!ReferenceEquals(User.Properties["Title"].Value, null))
                    {
                        if (User.Properties["Title"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            searchResult++;
                            if (searchResult == searchCount)
                            {
                                userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                                return;
                            }
                        }
                    }
                }
            }
            foreach (PSObject User in ActiveDirectory.Users)
            {
                if (User.Properties["Enabled"].Value.ToString() == "True")
                {
                    if (!ReferenceEquals(User.Properties["Department"].Value, null))
                {
                    if (User.Properties["Department"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        searchResult++;
                        if (searchResult == searchCount)
                        {
                            userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                            return;
                        }
                    }
                }
                }
            }
            searchCount = 0;
        }

        /// <summary>
        /// Lookup label with button characteristics
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lookupLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            lookupLabelButton.Style = defaultMouseDownLabelButtonStyle;

            if (lookupText.Text != "")
            {
                searchCount++;
                fullLookup();
            }
            else
            {
                userList.SelectedIndex = -1;
            }
        }
        private void lookupLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            lookupLabelButton.Style = defaultLabelButtonStyle;
        }
        private void lookupLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            lookupLabelButton.Style = defaultLabelButtonStyle;
        }

        /// <summary>
        /// User AD refresh button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refreshLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            resultMessage.Visibility = Visibility.Hidden;

            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            refreshLabelButton.Style = defaultMouseDownLabelButtonStyle;

            if (syncWorker.IsBusy != true)
            {
                refreshLabelButton.Content = "Please Wait";
                refreshLabelButton.IsEnabled = false;
                syncLabelButton.IsEnabled = false;
                syncWorker.RunWorkerAsync();
            }
        }
        private void refreshLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            refreshLabelButton.Style = defaultLabelButtonStyle;
        }
        private void refreshLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            refreshLabelButton.Style = defaultLabelButtonStyle;
        }

        /// <summary>
        /// Pressing Enter in searchbox invokes fullLookup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lookupText_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (lookupText.Text != "")
                {
                    searchCount++;
                    fullLookup();
                }
                else
                {
                    userList.SelectedIndex = -1;
                }
            }
            else if (e.Key == Key.Escape)
            {
                lookupText.Text = String.Empty;
            }
        }
        private void lookupText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lookupText.Text != "")
            {
                searchCount = 1;
                quickLookup();
            }
            else
            {
                userList.SelectedIndex = -1;
            }
        }

        /// <summary>
        /// Clicking the userlist (selecting an item) resets the search index
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            searchCount = 1;
        }

        /* Primary Action */
        /// <summary>
        /// Saves any changes made to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void disableLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            disableLabelButton.Style = defaultMouseDownLabelButtonStyle;

            selectedUser = userList.SelectedItem.ToString();

            resultMessage.Visibility = Visibility.Hidden;
            curtain.Visibility = Visibility.Visible;
            confirmMessage.Visibility = Visibility.Visible;
            confirmYesLabelButton.Visibility = Visibility.Visible;
            confirmYesLabelButton.IsEnabled = true;
            confirmNoLabelButton.Visibility = Visibility.Visible;
            confirmNoLabelButton.IsEnabled = true;
        }
        private void disableLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            disableLabelButton.Style = defaultLabelButtonStyle;
        }
        private void disableLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            disableLabelButton.Style = defaultLabelButtonStyle;
        }


        /// <summary>
        /// Warning and confirmation template if needed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /* Optional */
        private void confirmYesLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

            if (commandWorker.IsBusy != true)
            {
                commandWorker.RunWorkerAsync();
                disableLabelButton.IsEnabled = false;
            }
        }
        /* Optional */
        private void confirmNoLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            curtain.Visibility = Visibility.Hidden;
            confirmMessage.Visibility = Visibility.Hidden;
            confirmYesLabelButton.Visibility = Visibility.Hidden;
            confirmYesLabelButton.IsEnabled = false;
            confirmNoLabelButton.Visibility = Visibility.Hidden;
            confirmNoLabelButton.IsEnabled = false;

        }
        /* Optional */
        private void confirmLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }
        /* Optional */
        private void confirmLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style warningConfirmLabelButtonStyle = FindResource("warningConfirmLabelButtonStyle") as Style;
            Style warningCancelLabelButtonStyle = FindResource("warningCancelLabelButtonStyle") as Style;
            confirmYesLabelButton.Style = warningConfirmLabelButtonStyle;
            confirmNoLabelButton.Style = warningCancelLabelButtonStyle;
        }

        /// <summary>
        /// Hide result message with mouse
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resultMessage_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            resultMessage.Visibility = Visibility.Hidden;
        }

        /* Background Sync Worker */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        private BackgroundWorker syncWorker = new BackgroundWorker();
        private Exception syncResults;

        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        private void syncWorker_Initialize()
        {
            syncWorker.WorkerReportsProgress = true;
            syncWorker.WorkerSupportsCancellation = true;
            syncWorker.DoWork += syncWorker_DoWork;
            syncWorker.ProgressChanged += syncWorker_ProgressChanged;
            syncWorker.RunWorkerCompleted += syncWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void syncWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainWindow.closePrevention[5] = 1;
            syncResults = ActiveDirectory.GetAllUsers();
        }
        private void syncWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // No Progress to report
        }
        private void syncWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (syncResults != null)
            {
                Style defaultSyncErrorLabelButtonStyle = FindResource("defaultSyncErrorLabelButtonStyle") as Style;
                syncLabelButton.Style = defaultSyncErrorLabelButtonStyle;
                if (syncResults.Message.Contains("The term 'Get-ADUser' is not recognized as the name of a cmdlet"))
                {
                    MainWindow.RSATneeded = true;
                    System.Windows.Forms.MessageBox.Show(
                        "Remote Server Administrative Tools are missing on this system.",
                        "RSAT Missing",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk);
                    syncLabelButton.Content = "Retry Synchronization";
                }
                else if (syncResults.Message.Contains("Unable to find a default server with Active Directory"))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "Unable to find a default server with Active Directory.",
                        "Cannot Locate Server",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Asterisk);
                    syncLabelButton.Content = "Retry Synchronization";
                }
                else
                {
                    if (System.Windows.Forms.MessageBox.Show(
                        syncResults.ToString() + "\n\nEmail the Developer?",
                        "Powershell Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Asterisk) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start("mailto:support@dragonfire-llc.com");
                    }
                    syncLabelButton.Content = "Retry Synchronization";
                }
            }
            else
            {
                syncLabelButton.Content = "Synchronize Users";
                curtain.Visibility = Visibility.Hidden;
                syncLabelButton.Visibility = Visibility.Hidden;
                resultMessage.Visibility = Visibility.Hidden;
                resultMessage.Content = "User List Updated";
                resultMessage.Visibility = Visibility.Visible;
                BuildList();
            }

            refreshLabelButton.Content = "Refresh Users";
            refreshLabelButton.IsEnabled = true;
            syncLabelButton.IsEnabled = true;
            MainWindow.closePrevention[5] = 0;
        }

        /* Background Command Worker */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        private BackgroundWorker commandWorker = new BackgroundWorker();
        private Exception commandResults;

        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        private void commandWorker_Initialize()
        {
            commandWorker.WorkerReportsProgress = true;
            commandWorker.WorkerSupportsCancellation = true;
            commandWorker.DoWork += commandWorker_DoWork;
            commandWorker.ProgressChanged += commandWorker_ProgressChanged;
            commandWorker.RunWorkerCompleted += commandWorker_RunWorkerCompleted;
        }

        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static string selectedUser;
        private void commandWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            MainWindow.closePrevention[6] = 1;
            commandResults = ActiveDirectory.DisableUser(selectedUser);
        }
        private void commandWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Nothing to report
        }
        private void commandWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (commandResults != null)
            {
                System.Windows.Forms.MessageBox.Show(commandResults.ToString(), "Powershell Error", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
            }
            else
            {
                resultMessage.Content = "User Disabled Successfully";
                resultMessage.Visibility = Visibility.Visible;


                bool waiting = true;
                while (waiting)
                {
                    if (syncWorker.IsBusy != true)
                    {
                        refreshLabelButton.Content = "Please Wait";
                        refreshLabelButton.IsEnabled = false;
                        syncWorker.RunWorkerAsync();
                        waiting = false;
                    }
                    else
                    {
                        Thread.Sleep(200);
                        waiting = true;
                    }
                }
            }
            disableLabelButton.IsEnabled = true;
            MainWindow.closePrevention[6] = 0;
        }
    }
}
