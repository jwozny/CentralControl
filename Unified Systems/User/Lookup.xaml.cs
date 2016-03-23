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
    /// Interaction logic for Lookup.xaml
    /// </summary>
    public partial class Lookup : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Lookup()
        {
            InitializeComponent();
        }
        private void Lookup_Loaded(object sender, RoutedEventArgs e)
        {
            if (ReferenceEquals(ActiveDirectory.Users, null))
            {
                if (ActiveDirectory.Connect())
                {
                    resultMessage.Content = "AD Connection Successful";
                    resultMessage.Visibility = Visibility.Visible;
                }
                else
                {
                    ExitToLogin();
                }
            }
            BuildList();
            lookupText.Focus();
        }
        
        /// <summary>
        /// Clears and rebuilds the list
        /// </summary>
        private void BuildList()
        {
            userList.Items.Clear();

            foreach (UserPrincipal User in ActiveDirectory.Users)
            {
                userList.Items.Add(User.SamAccountName);
            }

            saveLabelButton.IsEnabled = false;
            if (!ReferenceEquals(ActiveDirectory.SelectedUser, null))
            {
                foreach (string user in userList.Items)
                {
                    if (ActiveDirectory.SelectedUser.SamAccountName == user)
                    {
                        saveLabelButton.IsEnabled = true;
                        userList.SelectedItem = user;
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
            userList.ScrollIntoView(userList.SelectedItem);
            groupList.Items.Clear();

            if (userList.SelectedItem != null)
            {
                foreach (UserPrincipal User in ActiveDirectory.Users)
                {
                    if (User.SamAccountName == userList.SelectedItem.ToString())
                    {
                        ActiveDirectory.SelectedUser = User;
                    }
                }

                infoLabel.Content = ActiveDirectory.SelectedUser.Name;

                Username.Content = ActiveDirectory.SelectedUser.SamAccountName;

                /*if (!ReferenceEquals(User.EmailAddress, null))
                { Email.Content = User.EmailAddress; }
                else { Email.Content = " "; }*/
                Email.Content = ActiveDirectory.SelectedUser.EmailAddress;

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.GetTitle(), null))
                { Title.Content = ActiveDirectory.SelectedUser.GetTitle(); }
                else { Title.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.GetDepartment(), null))
                { Department.Content = ActiveDirectory.SelectedUser.GetDepartment(); }
                else { Department.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.GetCompany(), null))
                { Company.Content = ActiveDirectory.SelectedUser.GetCompany(); }
                else { Company.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.GetCreatedDate(), null))
                { CreatedDate.Content = ActiveDirectory.SelectedUser.GetCreatedDate(); }
                else { CreatedDate.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountExpirationDate, null))
                { ExpiryDate.Content = ActiveDirectory.SelectedUser.AccountExpirationDate; }
                else { ExpiryDate.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastLogon, null))
                { LastLogonDate.Content = ActiveDirectory.SelectedUser.LastLogon; }
                else { LastLogonDate.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastBadPasswordAttempt, null))
                { LastBadPasswordAttempt.Content = ActiveDirectory.SelectedUser.LastBadPasswordAttempt; }
                else { LastBadPasswordAttempt.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.IsAccountLockedOut(), null))
                { LockedOut.Content = ActiveDirectory.SelectedUser.IsAccountLockedOut().ToString(); }
                else { LockedOut.Content = " "; }

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountLockoutTime, null))
                { AccountLockoutTime.Content = ActiveDirectory.SelectedUser.AccountLockoutTime; }
                else { AccountLockoutTime.Content = " "; }

                foreach (var Group in ActiveDirectory.SelectedUser.GetGroups())
                {
                    groupList.Items.Add(Group.Name);
                }

                saveLabelButton.IsEnabled = true;
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

                saveLabelButton.IsEnabled = false;
            }
        }
        private void ClearSelection()
        {
            userList.SelectedIndex = -1;
            saveLabelButton.IsEnabled = false;
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
                foreach (UserPrincipal User in ActiveDirectory.Users)
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
                foreach (UserPrincipal User in ActiveDirectory.Users)
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

            if (ActiveDirectory.Connect())
            {
                resultMessage.Content = "User List Updated";
                resultMessage.Visibility = Visibility.Visible;
            }
            else
            {
                ExitToLogin();
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
                    ClearSelection();
                }
            }
            else if (e.Key == Key.Escape)
            {
                lookupText.Text = String.Empty;
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

        /* Primary Action */
        /// <summary>
        /// Saves any changes made to the user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool confirmAction = true;
        private void saveLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultMouseDownLabelButtonStyle;
            resultMessage.Visibility = Visibility.Hidden;

            if (confirmAction)
            {
                curtain.Visibility = Visibility.Visible;
                confirmMessage.Content = "Are you sure you want to save changes made to " + ActiveDirectory.SelectedUser.SamAccountName + "?";
                confirmYesLabelButton.Content = "Save Changes to " + ActiveDirectory.SelectedUser.SamAccountName;
                confirmMessage.Visibility = Visibility.Visible;
                confirmYesLabelButton.Visibility = Visibility.Visible;
                confirmYesLabelButton.IsEnabled = true;
                confirmNoLabelButton.Visibility = Visibility.Visible;
                confirmNoLabelButton.IsEnabled = true;
            }
            else
            {
                if (ActiveDirectory.SelectedUser.SaveUser())
                {
                    resultMessage.Content = "User Saved Successfully";
                    resultMessage.Visibility = Visibility.Visible;
                    ActiveDirectory.RefreshUsers();
                    BuildList();
                }
                else if (ActiveDirectory.Connect())
                {
                    resultMessage.Content = ActiveDirectory.ConnectionError;
                    resultMessage.Visibility = Visibility.Visible;
                    BuildList();
                }
                else
                {
                    ExitToLogin();
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

            if (ActiveDirectory.SelectedUser.SaveUser())
            {
                resultMessage.Content = "User Saved Successfully";
                resultMessage.Visibility = Visibility.Visible;
                ActiveDirectory.RefreshUsers();
                BuildList();
            }
            else if (ActiveDirectory.Connect())
            {
                resultMessage.Content = ActiveDirectory.ConnectionError;
                resultMessage.Visibility = Visibility.Visible;
                BuildList();
            }
            else
            {
                ExitToLogin();
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

        public event EventHandler Disconnected;
        private void ExitToLogin()
        {
            Disconnected(this, new EventArgs());
        }
    }
}