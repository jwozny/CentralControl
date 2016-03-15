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
    /// Interaction logic for Enable.xaml
    /// </summary>
    public partial class Enable : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Enable()
        {
            InitializeComponent();
            ActiveDirectory.GetAllUsers.RunWorkerCompleted += this.GetAllUsers_Completed;
            ActiveDirectory.EnableUser.RunWorkerCompleted += this.EnableUser_Completed;

            if (ActiveDirectory.Users != null)
            {// If users are already synced, then build the list
                BuildList();
                if (!ActiveDirectory.GetAllUsers.IsBusy)
                {
                    refreshLabelButton.Content = "Refresh Users";
                    refreshLabelButton.IsEnabled = true;
                }
            }
            else if (ActiveDirectory.GetAllUsers.IsBusy)
            {// If no users, but there's a sync in progress
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.Visibility = Visibility.Visible;
                syncLabelButton.Content = "Please Wait";
                syncLabelButton.IsEnabled = false;
            }
            else if (MainWindow.RSATneeded)
            {// if no users, no sync in progress, and RSAT is needed.
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.Visibility = Visibility.Visible;
                syncLabelButton.Content = "Install RSAT to continue";
                syncLabelButton.IsEnabled = false;
            }
            else
            {// otherwise, show the curtain and the sync button, start a sync automatically
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.IsEnabled = true;
                syncLabelButton.Visibility = Visibility.Visible;

                Style defaultSyncMouseDownLabelButtonStyle = FindResource("defaultSyncMouseDownLabelButtonStyle") as Style;
                syncLabelButton.Style = defaultSyncMouseDownLabelButtonStyle;

                if (!ActiveDirectory.GetAllUsers.IsBusy)
                {
                    syncLabelButton.Content = "Please Wait";
                    syncLabelButton.IsEnabled = false;
                    ActiveDirectory.GetAllUsers.RunWorkerAsync();
                }
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

            if (!ActiveDirectory.GetAllUsers.IsBusy)
            {
                syncLabelButton.Content = "Please Wait";
                syncLabelButton.IsEnabled = false;
                ActiveDirectory.GetAllUsers.RunWorkerAsync();
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
        private void GetAllUsers_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ActiveDirectory.GetAllUsersResults != null)
            {
                Style defaultSyncErrorLabelButtonStyle = FindResource("defaultSyncErrorLabelButtonStyle") as Style;
                syncLabelButton.Style = defaultSyncErrorLabelButtonStyle;
                syncLabelButton.Content = "Retry Synchronization";
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
        }

        /// <summary>
        /// Clears and rebuilds the list
        /// </summary>
        private void BuildList()
        {
            userList.Items.Clear();
            if (ActiveDirectory.GetAllUsers.IsBusy)
            {
                foreach (PSObject User in ActiveDirectory.PreviousUsers)
                {
                    if (User.Properties["Enabled"].Value.ToString() == "False")
                    {
                        userList.Items.Add(User.Properties["SamAccountName"].Value.ToString());
                    }
                }
            }
            else
            {
                foreach (PSObject User in ActiveDirectory.Users)
                {
                    if (User.Properties["Enabled"].Value.ToString() == "False")
                    {
                        userList.Items.Add(User.Properties["SamAccountName"].Value.ToString());
                    }
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
            if (!ReferenceEquals(userList.SelectedItem, null))
            {
                ActiveDirectory.selectedUser = userList.SelectedItem.ToString();
            }
            userList.ScrollIntoView(userList.SelectedItem);
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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
                if (User.Properties["Enabled"].Value.ToString() == "False")
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

            if (!ActiveDirectory.GetAllUsers.IsBusy)
            {
                refreshLabelButton.Content = "Please Wait";
                refreshLabelButton.IsEnabled = false;
                ActiveDirectory.GetAllUsers.RunWorkerAsync();
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
        private bool confirmAction = false;
        private void enableLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            enableLabelButton.Style = defaultMouseDownLabelButtonStyle;

            if (confirmAction)
            {
                resultMessage.Visibility = Visibility.Hidden;
                curtain.Visibility = Visibility.Visible;
                confirmMessage.Visibility = Visibility.Visible;
                confirmYesLabelButton.Visibility = Visibility.Visible;
                confirmYesLabelButton.IsEnabled = true;
                confirmNoLabelButton.Visibility = Visibility.Visible;
                confirmNoLabelButton.IsEnabled = true;
            }
            else
            {
                if (!ActiveDirectory.EnableUser.IsBusy)
                {
                    ActiveDirectory.EnableUser.RunWorkerAsync();
                    enableLabelButton.IsEnabled = false;
                }
            }
        }
        private void enableLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            enableLabelButton.Style = defaultLabelButtonStyle;
        }
        private void enableLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            enableLabelButton.Style = defaultLabelButtonStyle;
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

            if (!ActiveDirectory.EnableUser.IsBusy)
            {
                ActiveDirectory.EnableUser.RunWorkerAsync();
                enableLabelButton.IsEnabled = false;
            }
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
        /// <summary>
        /// Custom background worker complete event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableUser_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            if (ActiveDirectory.EnableUserResults == null)
            {
                resultMessage.Content = "User Enabled Successfully";
                resultMessage.Visibility = Visibility.Visible;

                if (!ActiveDirectory.GetAllUsers.IsBusy)
                {
                    refreshLabelButton.Content = "Please Wait";
                    refreshLabelButton.IsEnabled = false;
                    ActiveDirectory.GetAllUsers.RunWorkerAsync();
                }
            }
            enableLabelButton.IsEnabled = true;
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
    }
}
