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

namespace Central_Control.AD
{
    /// <summary>
    /// Interaction logic for Users.xaml
    /// </summary>
    public partial class Users : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Users()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Users_Loaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Connector.RunWorkerCompleted += Connector_Completed;
            if (!ReferenceEquals(ActiveDirectory.Users, null) && ActiveDirectory.IsConnected)
            {
                UserList.ItemsSource = ActiveDirectory.Users;
                SearchBox.Focus();

                ICollectionView view = CollectionViewSource.GetDefaultView(ActiveDirectory.Users);
                new TextSearchFilter(view, SearchBox);
            }
            else
            {
                ExitAD();
            }
        }

        /* Functions */
        /// <summary>
        /// 
        /// </summary>
        private void ClearSelection()
        {
            UserList.SelectedIndex = -1;
            
            string tmp = SearchBox.Text;
            SearchBox.Text = " ";
            SearchBox.Text = null;
            SearchBox.Text = tmp;
        }
        /// <summary>
        /// Reset the user action buttons with the appropriate actions for the selected user
        /// </summary>
        private void ClearInfoGrid()
        {
            Name.Text = string.Empty;
            Username.Text = string.Empty;
            Email.Text = string.Empty;
            Title.Text = string.Empty;
            Department.Text = string.Empty;
            Company.Text = string.Empty;
            CreatedDate.Text = string.Empty;
            ExpiryDate.Text = string.Empty;
            LastLogonDate.Text = string.Empty;
            LastBadPasswordAttempt.Text = string.Empty;
            LockedOut.Text = string.Empty;
            AccountLockoutTime.Text = string.Empty;
            DistinguishedName.Text = string.Empty;
            
            GroupList.ItemsSource = null;
        }
        /// <summary>
        /// Set the selected user to the selected user variable
        /// </summary>
        private void UpdateSelectedUser()
        {
            ActiveDirectory.GetUserProperties.RunWorkerCompleted += GetUserProperties_Completed;
            ActiveDirectory.GetUserProperties.ProgressChanged += GetUserProperties_ProgressChanged;

            if ((UserList.SelectedItem != null) && (UserList.SelectedIndex != -1))
            {
                foreach (ActiveDirectory.UserPrincipalEx User in ActiveDirectory.Users)
                {
                    if (User.Name == UserList.SelectedItem.ToString())
                    {
                        ActiveDirectory.SelectedUser = User;
                        if (!ActiveDirectory.GetUserProperties.IsBusy)
                        {
                            ActiveDirectory.GetUserProperties.RunWorkerAsync();
                        }
                    }
                }
            }
            else
            {
                ActiveDirectory.SelectedUser = null;
            }
        }
        /// <summary>
        /// Reset the user action buttons with the appropriate actions for the selected user
        /// </summary>
        private void UpdateUserButtons()
        {
            if (ActiveDirectory.SelectedUser != null)
            {
                /* Account Buttons */

                DeleteUserButton.IsEnabled = true;

                if (!ActiveDirectory.SelectedUser.Enabled.Value)
                {
                    EnableUserButton.IsEnabled = true;
                    DisableUserButton.IsEnabled = false;
                }
                else
                {
                    EnableUserButton.IsEnabled = false;
                    DisableUserButton.IsEnabled = true;
                }

                if (ActiveDirectory.SelectedUser.IsAccountExpired())
                {
                    ExtendUserButton.Content = "Extend";
                    ExtendUserButton.IsEnabled = true;
                }
                else if (ActiveDirectory.SelectedUser.AccountExpirationDate == null)
                {
                    ExtendUserButton.Content = "Set Expiry";
                    ExtendUserButton.IsEnabled = true;
                }
                else
                {
                    ExtendUserButton.Content = "Remove Expiry";
                    ExtendUserButton.IsEnabled = true;
                }
                if (!ActiveDirectory.GetUserProperties.IsBusy)
                {
                    if (ActiveDirectory.SelectedUser.IsAccountLockedOut())
                    {
                        UnlockUserButton.IsEnabled = true;
                    }
                    else
                    {
                        UnlockUserButton.IsEnabled = false;
                    }
                }

                /* Password Buttons */

                if (ActiveDirectory.SelectedUser.PasswordNeverExpires)
                {
                    ExpiryPasswordButton.Content = "Set Expiry";
                    ExpiryPasswordButton.IsEnabled = true;

                    RefreshPasswordButton.IsEnabled = false;

                    ExpirePasswordButton.IsEnabled = false;
                }
                else
                {
                    ExpiryPasswordButton.Content = "Remove Expiry";
                    ExpiryPasswordButton.IsEnabled = true;

                    RefreshPasswordButton.IsEnabled = true;

                    ExpirePasswordButton.IsEnabled = true;
                }

                ResetPasswordButton.IsEnabled = true;
            }
            else
            {
                /* Account Buttons */
                DeleteUserButton.IsEnabled = false;

                EnableUserButton.IsEnabled = false;
                ExtendUserButton.Content = "Extend";
                ExtendUserButton.IsEnabled = false;
                UnlockUserButton.IsEnabled = false;
                DisableUserButton.IsEnabled = false;

                /* Password Buttons */
                ExpiryPasswordButton.IsEnabled = false;
                RefreshPasswordButton.IsEnabled = false;
                ExpirePasswordButton.IsEnabled = false;
                ResetPasswordButton.IsEnabled = false;
            }
        }
        /// <summary>
        /// Reset the user info for the selected user
        /// </summary>
        private void UpdateUserInfo()
        {
            if (ActiveDirectory.SelectedUser != null)
            {
                Name.Text = ActiveDirectory.SelectedUser.Name;

                Username.Text = ActiveDirectory.SelectedUser.SamAccountName;

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.EmailAddress, null))
                    Email.Text = ActiveDirectory.SelectedUser.EmailAddress;
                else
                    Email.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.Title, null))
                    Title.Text = ActiveDirectory.SelectedUser.Title;
                else
                    Title.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.Department, null))
                    Department.Text = ActiveDirectory.SelectedUser.Department;
                else
                    Department.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.Company, null))
                    Company.Text = ActiveDirectory.SelectedUser.Company;
                else
                    Company.Text = " ";

                CreatedDate.Text = "Fetching...";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountExpirationDate, null))
                    ExpiryDate.Text = ActiveDirectory.SelectedUser.AccountExpirationDate.ToString();
                else
                    ExpiryDate.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastLogon, null))
                    LastLogonDate.Text = ActiveDirectory.SelectedUser.LastLogon.ToString();
                else
                    LastLogonDate.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LastBadPasswordAttempt, null))
                    LastBadPasswordAttempt.Text = ActiveDirectory.SelectedUser.LastBadPasswordAttempt.ToString();
                else
                    LastBadPasswordAttempt.Text = " ";

                LockedOut.Text = "Fetching...";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountLockoutTime, null))
                    AccountLockoutTime.Text = ActiveDirectory.SelectedUser.AccountLockoutTime.ToString();
                else
                    AccountLockoutTime.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.DistinguishedName, null))
                    DistinguishedName.Text = ActiveDirectory.SelectedUser.DistinguishedName.ToString();
                else
                    DistinguishedName.Text = " ";

                GroupsPlaceholder.Visibility = Visibility.Visible;
            }
            else
            {
                ClearInfoGrid();

                GroupsPlaceholder.Visibility = Visibility.Hidden;
            }

        }

        /* Control Actions */
        /// <summary>
        /// Pressing Enter in searchbox invokes fullLookup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchBox.Text = string.Empty;
                ClearSelection();
            }
        }
        /// <summary>
        /// Do a search when text is entered, if the text is null then clear the selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //if (SearchBox.Text != "")
            //{
            //    searchCount = 1;
            //    fullLookup();
            //}
            //else
            //{
            //    ClearSelection();
            //}
        }
        /// <summary>
        /// Displays more info on the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserList.ScrollIntoView(UserList.SelectedItem);
            
            UpdateSelectedUser();
            UpdateUserInfo();
            UpdateUserButtons();
        }
        /// <summary>
        /// Clear the selection when {ESC} is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchBox.Text = string.Empty;
                ClearSelection();
            }
        }
        /// <summary>
        /// User refresh button action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ResultMessage.Visibility = Visibility.Hidden;

            RefreshButton.IsEnabled = false;
            RefreshButton.Content = "Refreshing...";

            ActiveDirectory.Refresh("Users");
        }

        /* User Button Actions */
        /// <summary>
        /// Button action to create a new user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
        }
        /// <summary>
        /// Button action to import a user from an OSticket ticket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportUserButton_Click(object sender, RoutedEventArgs e)
        {
        }
        /// <summary>
        /// Button action to delete the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.Visibility = Visibility.Visible;

            WarningMessage.Text = "Are you sure you want to delete " + ActiveDirectory.SelectedUser.Name + "?";
            ConfirmButton.Content = "Delete " + ActiveDirectory.SelectedUser.SamAccountName;
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Delete";
        }
        /// <summary>
        /// Button action to enable the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnableUserButton_Click(object sender, RoutedEventArgs e)
        {
            Report(ActiveDirectory.SelectedUser.EnableUser(), "User Enabled Successfully");
        }
        /// <summary>
        /// Button action to extend expiration on the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExtendUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveDirectory.SelectedUser.IsAccountExpired())
            {
                Report(ActiveDirectory.SelectedUser.AddExpiry(30), "Account Extended Successfully (30 Days)");
            }
            else if (ActiveDirectory.SelectedUser.AccountExpirationDate == null)
            {
                Report(ActiveDirectory.SelectedUser.AddExpiry(30), "Account Expiration Set Successfully (30 Days)");
            }
            else
            {
                Report(ActiveDirectory.SelectedUser.RemoveExpiry(), "Account Expiration Removed Successfully");
            }
        }
        /// <summary>
        /// Button action to unlock the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UnlockUserButton_Click(object sender, RoutedEventArgs e)
        {
            Report(ActiveDirectory.SelectedUser.UnlockUser(), "User Unlocked Successfully");
        }
        /// <summary>
        /// Button action to disable the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DisableUserButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.Visibility = Visibility.Visible;

            WarningMessage.Text = "Are you sure you want to disable " + ActiveDirectory.SelectedUser.Name + "?";
            ConfirmButton.Content = "Disable " + ActiveDirectory.SelectedUser.SamAccountName;
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Disable";
        }
        /* Password Button Actions */
        /// <summary>
        /// Button action to refresh the selected user's password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpiryPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (ActiveDirectory.SelectedUser.PasswordNeverExpires)
            {
                Report(ActiveDirectory.SelectedUser.AddPasswordExpiry(), "Account Password Expiration Added Successfully");
            }
            else
            {
                Report(ActiveDirectory.SelectedUser.RemovePasswordExpiry(), "Account Password Expiration Removed Successfully");
            }
        }
        /// <summary>
        /// Button action to refresh the selected user's password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            Report(ActiveDirectory.SelectedUser.RefreshPassword(), "User Password Refreshed Successfully");
        }
        /// <summary>
        /// Button action to expire the selected user's password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExpirePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            Report(ActiveDirectory.SelectedUser.ExpirePassword(), "User Password Expired Succcessfully");
        }
        /// <summary>
        /// Button action to reset the selected user's password
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.Visibility = Visibility.Visible;

            WarningMessage.Text = "Enter a new password for " + ActiveDirectory.SelectedUser.Name + ":";
            ConfirmButton.Content = "Reset Password";
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            PasswordInput.Visibility = Visibility.Visible;

            Action = "Reset";
        }

        /* Confirm Message and Actions */
        /// <summary>
        /// Action to perform when the confirm button is clicked
        /// </summary>
        private string Action;
        /// <summary>
        /// Confirm the action and continue
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {            
            switch (Action)
            {
                case "Delete":
                    Warning.Visibility = Visibility.Visible;

                    WarningMessage.Text = "This cannot be undone! Are you super sure to DELETE " + ActiveDirectory.SelectedUser.Name + "?";
                    ConfirmButton.Content = "DELETE " + ActiveDirectory.SelectedUser.SamAccountName;
                    ConfirmButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;

                    Action = "ReallyDelete";
                    return;
                case "ReallyDelete":
                    Report(ActiveDirectory.SelectedUser.DeleteUser(), "User Deleted Successfully");
                    break;
                case "Reset":
                    Style PasswordBox = FindResource("PasswordBox") as Style;
                    Style PasswordBox_Error = FindResource("PasswordBox_Error") as Style;

                    NewPassword.Style = PasswordBox;
                    NewPassword_Confirm.Style = PasswordBox;

                    if ((string.IsNullOrEmpty(NewPassword.Password) ^ string.IsNullOrEmpty(NewPassword_Confirm.Password)) || (string.IsNullOrEmpty(NewPassword.Password) && string.IsNullOrEmpty(NewPassword_Confirm.Password)))
                    {
                        if (string.IsNullOrEmpty(NewPassword.Password))
                        {
                            NewPassword.Style = PasswordBox_Error;
                        }
                        if (string.IsNullOrEmpty(NewPassword_Confirm.Password))
                        {
                            NewPassword_Confirm.Style = PasswordBox_Error;
                        }
                        return;
                    }
                    else
                    {
                        if (NewPassword.Password == NewPassword_Confirm.Password)
                        {
                            Report(ActiveDirectory.SelectedUser.ResetUser(NewPassword.Password), "User Password Reset Successfully");
                        }
                        else
                        {
                            NewPassword_Confirm.Style = PasswordBox_Error;
                            return;
                        }
                    }
                    break;
                case "Disable":
                    Report(ActiveDirectory.SelectedUser.DisableUser(), "User Disabled Successfully");
                    break;
                default:
                    break;
            }

            Warning.Visibility = Visibility.Hidden;
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            PasswordInput.Visibility = Visibility.Collapsed;
            Action = null;
        }
        /// <summary>
        /// Cancel the action and return to the default stage
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.Visibility = Visibility.Hidden;
            
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            PasswordInput.Visibility = Visibility.Collapsed;

            Action = null;
        }
        private bool Report(bool Result, string SuccessMessage)
        {
            ResultMessage.Visibility = Visibility.Hidden;

            if (Result)
            {
                ResultMessage.Content = SuccessMessage;
                ResultMessage.Visibility = Visibility.Visible;

                if(Action == "ReallyDelete")
                {
                    ClearSelection();
                }

                UpdateSelectedUser();
                UpdateUserButtons();

                return true;
            }
            else
            {
                ResultMessage.Content = ActiveDirectory.ConnectionError;
                ResultMessage.Visibility = Visibility.Visible;

                RefreshButton.IsEnabled = false;
                ActiveDirectory.Connect();

                return false;
            }
        }

        /* Event Handlers */
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetUserProperties_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Do this when progress is reported.
            if (UserList.SelectedItem != null)
            {
                if (UserList.SelectedItem.ToString() == ActiveDirectory.CurrentBackgroundUser.Name.ToString())
                {
                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_IsAccountLockedOut, null))
                    {
                        LockedOut.Text = ActiveDirectory.SelectedUser_IsAccountLockedOut.ToString();
                        
                        if (ActiveDirectory.SelectedUser_IsAccountLockedOut)
                        {
                            UnlockUserButton.IsEnabled = true;
                        }
                        else
                        {
                            UnlockUserButton.IsEnabled = false;
                        }
                    }
                    else { LockedOut.Text = "Fetching..."; }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GetUserProperties_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            // Work Completed - Do this.
            ActiveDirectory.GetUserProperties.RunWorkerCompleted -= GetUserProperties_Completed;
            ActiveDirectory.GetUserProperties.ProgressChanged -= GetUserProperties_ProgressChanged;

            if (UserList.SelectedItem != null)
            {
                if (UserList.SelectedItem.ToString() == ActiveDirectory.CurrentBackgroundUser.Name.ToString())
                {
                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_CreatedDate, null))
                    { CreatedDate.Text = ActiveDirectory.SelectedUser_CreatedDate.ToString(); }
                    else { CreatedDate.Text = " "; }

                    if (!ReferenceEquals(ActiveDirectory.SelectedUser_IsAccountLockedOut, null))
                    { LockedOut.Text = ActiveDirectory.SelectedUser_IsAccountLockedOut.ToString(); }
                    else { LockedOut.Text = " "; }

                    GroupList.ItemsSource = ActiveDirectory.SelectedUser_Groups;
                    GroupsPlaceholder.Visibility = Visibility.Hidden;

                    UpdateUserButtons();
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
                CreatedDate.Text = string.Empty;
                LockedOut.Text = string.Empty;
            }
        }
        /// <summary>
        /// When the AD connector background worker is finished, notify the user or exit the page if disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Connector_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            RefreshButton.IsEnabled = true;
            if (ActiveDirectory.IsConnected)
            {
                RefreshButton.Content = "Refresh Users";

                ResultMessage.Content = "User List Updated";
                ResultMessage.Visibility = Visibility.Visible;
                
                string tmp = SearchBox.Text;
                SearchBox.Text = " ";
                SearchBox.Text = null;
                SearchBox.Text = tmp;
            }
            else
            {
                ExitAD();
            }
        }
        /// <summary>
        /// Event handler when AD is no longer connected
        /// </summary>
        public event EventHandler Disconnected;
        /// <summary>
        /// Trigger the Disconnected event handler
        /// </summary>
        private void ExitAD()
        {
            Disconnected(this, new EventArgs());
        }
        /// <summary>
        /// Remove event handler watchers when the page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Users_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Connector.RunWorkerCompleted -= Connector_Completed;
        }
    }
}