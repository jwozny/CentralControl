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
    /// Interaction logic for Enable.xaml
    /// </summary>
    public partial class Extend : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Extend()
        {
            InitializeComponent();
        }
        private void Extend_Loaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.ConnectAD.RunWorkerCompleted += ConnectAD_Completed;
            if (!ReferenceEquals(ActiveDirectory.Users, null) && ActiveDirectory.IsConnected)
            {
                BuildList();
                lookupText.Focus();
            }
            else
            {
                ExitToLogin();
            }
        }

        /// <summary>
        /// Clears and rebuilds the list
        /// </summary>
        private void BuildList()
        {
            string temp_selectedUser = "";
            if ((userList.SelectedIndex != -1 || userList.SelectedIndex != 0) && !ReferenceEquals(userList.SelectedItem, null))
            {
                temp_selectedUser = userList.SelectedItem.ToString();
            }

            userList.Items.Clear();

            foreach (UserPrincipal User in ActiveDirectory.Users)
            {
                if (User.IsAccountExpired())
                {
                    userList.Items.Add(User.SamAccountName);
                }
            }

            extendLabelButton.IsEnabled = false;
            if (!ReferenceEquals(ActiveDirectory.SelectedUser, null))
            {
                foreach (string user in userList.Items)
                {
                    if (ActiveDirectory.SelectedUser.SamAccountName == user)
                    {
                        extendLabelButton.IsEnabled = true;
                        userList.SelectedItem = user;
                    }
                }
            }
            else if (!ReferenceEquals(temp_selectedUser, ""))
            {
                foreach (string user in userList.Items)
                {
                    if (temp_selectedUser == user.ToString())
                    {
                        extendLabelButton.IsEnabled = true;
                        userList.SelectedItem = user;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Displays more info on the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void userList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            userList.ScrollIntoView(userList.SelectedItem);
            groupList.Items.Clear();
            ActiveDirectory.GetUserProperties.RunWorkerCompleted += GetUserProperties_Completed;
            ActiveDirectory.GetUserProperties.ProgressChanged += GetUserProperties_ProgressChanged;

            if (userList.SelectedItem != null)
            {
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.SamAccountName == userList.SelectedItem.ToString())
                    {
                        ActiveDirectory.SelectedUser = User;
                        if (!ActiveDirectory.GetUserProperties.IsBusy)
                        {
                            ActiveDirectory.GetUserProperties.RunWorkerAsync();
                        }
                    }
                }
                HighlightSubmenus(this, new EventArgs());

                infoLabel.Content = ActiveDirectory.SelectedUser.Name;

                Username.Content = ActiveDirectory.SelectedUser.SamAccountName;

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.EmailAddress, null))
                { Email.Content = ActiveDirectory.SelectedUser.EmailAddress; }
                else { Email.Content = " "; }

                Title.Content = "Fetching...";

                Department.Content = "Fetching...";

                Company.Content = "Fetching...";

                CreatedDate.Content = "Fetching...";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountExpirationDate, null))
                { ExpiryDate.Content = ActiveDirectory.SelectedUser.AccountExpirationDate; }
                else { ExpiryDate.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastLogon, null))
                { LastLogonDate.Content = ActiveDirectory.SelectedUser.LastLogon; }
                else { LastLogonDate.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastBadPasswordAttempt, null))
                { LastBadPasswordAttempt.Content = ActiveDirectory.SelectedUser.LastBadPasswordAttempt; }
                else { LastBadPasswordAttempt.Content = " "; }

                LockedOut.Content = "Fetching...";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountLockoutTime, null))
                { AccountLockoutTime.Content = ActiveDirectory.SelectedUser.AccountLockoutTime; }
                else { AccountLockoutTime.Content = " "; }

                GroupsPlaceholder.Visibility = Visibility.Visible;

                extendLabelButton.IsEnabled = true;
            }
            else
            {
                ActiveDirectory.SelectedUser = null;
                HighlightSubmenus(this, new EventArgs());

                infoLabel.Content = "User Information";
                Username.Content = string.Empty;
                Email.Content = string.Empty;
                Title.Content = string.Empty;
                Department.Content = string.Empty;
                Company.Content = string.Empty;
                CreatedDate.Content = string.Empty;
                ExpiryDate.Content = string.Empty;
                LastLogonDate.Content = string.Empty;
                LastBadPasswordAttempt.Content = string.Empty;
                LockedOut.Content = string.Empty;
                AccountLockoutTime.Content = string.Empty;

                groupList.Items.Clear();
                GroupsPlaceholder.Visibility = Visibility.Hidden;

                extendLabelButton.IsEnabled = false;
            }
        }
        private void GetUserProperties_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Do this when progress is reported.
            if (userList.SelectedItem != null)
            {
                if (userList.SelectedItem.ToString() == ActiveDirectory.CurrentBackgroundUser.SamAccountName.ToString())
                {
                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Title, null))
                    { Title.Content = ActiveDirectory.SelectedUser_Title; }
                    else { Title.Content = "Fetching..."; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Department, null))
                    { Department.Content = ActiveDirectory.SelectedUser_Department; }
                    else { Department.Content = "Fetching..."; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Company, null))
                    { Company.Content = ActiveDirectory.SelectedUser_Company; }
                    else { Company.Content = "Fetching..."; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_CreatedDate, null))
                    { CreatedDate.Content = ActiveDirectory.SelectedUser_CreatedDate; }
                    else { CreatedDate.Content = "Fetching..."; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_IsAccountLockedOut, null))
                    {
                        LockedOut.Content = ActiveDirectory.SelectedUser_IsAccountLockedOut.ToString();
                        HighlightSubmenus(this, new EventArgs());
                    }
                    else { LockedOut.Content = "Fetching..."; }
                }
            }
        }
        private void GetUserProperties_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            // Work Completed - Do this.
            ActiveDirectory.GetUserProperties.RunWorkerCompleted -= GetUserProperties_Completed;
            ActiveDirectory.GetUserProperties.ProgressChanged -= GetUserProperties_ProgressChanged;

            if (userList.SelectedItem != null)
            {
                if (userList.SelectedItem.ToString() == ActiveDirectory.CurrentBackgroundUser.SamAccountName.ToString())
                {
                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Title, null))
                    { Title.Content = ActiveDirectory.SelectedUser_Title; }
                    else { Title.Content = " "; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Department, null))
                    { Department.Content = ActiveDirectory.SelectedUser_Department; }
                    else { Department.Content = " "; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Company, null))
                    { Company.Content = ActiveDirectory.SelectedUser_Company; }
                    else { Company.Content = " "; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_CreatedDate, null))
                    { CreatedDate.Content = ActiveDirectory.SelectedUser_CreatedDate; }
                    else { CreatedDate.Content = " "; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_IsAccountLockedOut, null))
                    { LockedOut.Content = ActiveDirectory.SelectedUser_IsAccountLockedOut.ToString(); }
                    else { LockedOut.Content = " "; }

                    groupList.Items.Clear();
                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_Groups, null))
                    {
                        foreach (GroupPrincipal Group in ActiveDirectory.SelectedUser_Groups)
                        {
                            groupList.Items.Add(Group);
                        }
                    }
                    GroupsPlaceholder.Visibility = Visibility.Hidden;
                }
                else
                {
                    ActiveDirectory.GetUserProperties.RunWorkerCompleted += GetUserProperties_Completed;
                    ActiveDirectory.GetUserProperties.ProgressChanged += GetUserProperties_ProgressChanged;
                    if (!ActiveDirectory.GetUserProperties.IsBusy)
                    {
                        ActiveDirectory.GetUserProperties.RunWorkerAsync();
                    }
                }
            }
            else
            {
                Title.Content = string.Empty;
                Department.Content = string.Empty;
                Company.Content = string.Empty;
                CreatedDate.Content = string.Empty;
                LockedOut.Content = string.Empty;
                groupList.Items.Clear();
            }
        }
        private void ClearSelection()
        {
            userList.SelectedIndex = -1;
            extendLabelButton.IsEnabled = false;
        }

        /* Search Functions */
        private int searchResult;
        private int searchCount;

        /// <summary>
        /// List search from lookupText
        /// </summary>
        private void fullLookup()
        {
            searchResult = 0;
            int retry = 0;
            while (retry <= 1)
            {
                foreach (string user in userList.Items)
                {
                    if (user == lookupText.Text)
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
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.IsAccountExpired())
                    {
                        if (User.Name.IndexOf(lookupText.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            searchResult++;
                            if (searchResult == searchCount)
                            {
                                userList.SelectedItem = User.SamAccountName;
                                return;
                            }
                        }
                    }
                }
                foreach (string user in userList.Items)
                {
                    if (user.IndexOf(lookupText.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        searchResult++;
                        if (searchResult == searchCount)
                        {
                            userList.SelectedItem = user;
                            return;
                        }
                    }
                }
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.IsAccountExpired())
                    {
                        if (lookupText.Text.IndexOf("@", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!ReferenceEquals(User.EmailAddress, null))
                            {
                                if (User.EmailAddress.IndexOf(lookupText.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                                {
                                    searchResult++;
                                    if (searchResult == searchCount)
                                    {
                                        userList.SelectedItem = User.SamAccountName;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.IsAccountExpired())
                    {
                        if (!ReferenceEquals(User.GetTitle(), null))
                        {
                            if (User.GetTitle().IndexOf(lookupText.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                searchResult++;
                                if (searchResult == searchCount)
                                {
                                    userList.SelectedItem = User.SamAccountName;
                                    return;
                                }
                            }
                        }
                    }
                }
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.IsAccountExpired())
                    {
                        if (!ReferenceEquals(User.GetDepartment(), null))
                        {
                            if (User.GetDepartment().IndexOf(lookupText.Text, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                searchResult++;
                                if (searchResult == searchCount)
                                {
                                    userList.SelectedItem = User.SamAccountName;
                                    return;
                                }
                            }
                        }
                    }
                }
                searchCount = 1;
                searchResult = 0;
                retry++;
            }
            ClearSelection();
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
                ClearSelection();
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

            refreshLabelButton.IsEnabled = false;
            ActiveDirectory.Connect();
            refreshLabelButton.Content = "Refreshing...";
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
                    ClearSelection();
                }
            }
            else if (e.Key == Key.Escape)
            {
                lookupText.Text = string.Empty;
                ClearSelection();
            }
        }
        private void lookupText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (lookupText.Text != "")
            {
                searchCount = 1;
                fullLookup();
            }
            else
            {
                ClearSelection();
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
        private void userList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                lookupText.Text = string.Empty;
                ClearSelection();
            }
        }

        /* Primary Action */
        /// <summary>
        /// Saves any changes made to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool confirmAction = false;
        private void extendLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            extendLabelButton.Style = defaultMouseDownLabelButtonStyle;
            resultMessage.Visibility = Visibility.Hidden;

            if (confirmAction)
            {
                curtain.Visibility = Visibility.Visible;
                confirmMessage.Content = "Are you sure you want to extend " + ActiveDirectory.SelectedUser.SamAccountName + "?";
                confirmYesLabelButton.Content = "Extend " + ActiveDirectory.SelectedUser.SamAccountName;
                confirmMessage.Visibility = Visibility.Visible;
                confirmYesLabelButton.Visibility = Visibility.Visible;
                confirmYesLabelButton.IsEnabled = true;
                confirmNoLabelButton.Visibility = Visibility.Visible;
                confirmNoLabelButton.IsEnabled = true;
            }
            else
            {
                if (ActiveDirectory.SelectedUser.ExtendUser(30))
                {
                    resultMessage.Content = "User Extended Successfully";
                    resultMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    resultMessage.Content = ActiveDirectory.ConnectionError;
                    resultMessage.Visibility = Visibility.Visible;
                    refreshLabelButton.IsEnabled = false;
                    ActiveDirectory.Connect();
                }
                extendLabelButton.IsEnabled = true;
            }
        }
        private void extendLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            extendLabelButton.Style = defaultLabelButtonStyle;
        }
        private void extendLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            extendLabelButton.Style = defaultLabelButtonStyle;
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

            if (ActiveDirectory.SelectedUser.ExtendUser(30))
            {
                resultMessage.Content = "User Extended Successfully";
                resultMessage.Visibility = Visibility.Visible;
            }
            else
            {
                resultMessage.Content = ActiveDirectory.ConnectionError;
                resultMessage.Visibility = Visibility.Visible;
                refreshLabelButton.IsEnabled = false;
                ActiveDirectory.Connect();
            }
            extendLabelButton.IsEnabled = true;
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

        public event EventHandler HighlightSubmenus;
        public event EventHandler Disconnected;
        private void ExitToLogin()
        {
            Disconnected(this, new EventArgs());
        }
        private void ConnectAD_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            refreshLabelButton.IsEnabled = true;
            if (ActiveDirectory.IsConnected)
            {
                refreshLabelButton.Content = "Refresh Users";

                BuildList();
                HighlightSubmenus(this, new EventArgs());
            }
            else
            {
                ExitToLogin();
            }
        }
        private void Extend_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.ConnectAD.RunWorkerCompleted -= ConnectAD_Completed;
        }
    }
}