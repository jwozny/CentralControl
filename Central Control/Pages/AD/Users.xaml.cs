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
        public Users()
        {
            InitializeComponent();
        }

        #region Page Events
        /// <summary>
        /// Do this when the page loades
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Users_Loaded(object sender, RoutedEventArgs e)
        {
            BackgroundBlur(false);

            ActiveDirectory.Connector.RunWorkerCompleted += User_Fetcher_Completed;
            ActiveDirectory.User_Fetcher.RunWorkerCompleted += User_Fetcher_Completed;
            UpdateUserButtons();

            if (!ReferenceEquals(ActiveDirectory.Users, null) && ActiveDirectory.IsConnected)
            {
                UserList.ItemsSource = ActiveDirectory.Users;

                OUBox.ItemsSource = ActiveDirectory.OUs;
                OUBox.SelectedValuePath = "Path";

                SearchBox.Focus();

                ICollectionView UserView = CollectionViewSource.GetDefaultView(ActiveDirectory.Users);
                new TextSearchFilter(UserView, SearchBox);
            }
            else
            {
                ExitAD();
            }
        }
        /// <summary>
        /// When the AD Updater_Users background worker is finished, notify the user or exit the page if disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void User_Fetcher_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateUserButtons();

            if (ActiveDirectory.IsConnected)
            {
                ResultMessage.Text = "User List Updated";
                ResultBox.Visibility = Visibility.Visible;

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
        /// Remove event handler watchers when the page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Users_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.User_Fetcher.RunWorkerCompleted -= User_Fetcher_Completed;
            ActiveDirectory.Connector.RunWorkerCompleted -= User_Fetcher_Completed;
        }
        #endregion Page Events

        #region Common Functions
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
            if ((UserList.SelectedItem != null) && (UserList.SelectedIndex != -1))
            {
                ActiveDirectory.SelectedUser = UserList.Items[UserList.SelectedIndex] as ActiveDirectory.UserPrincipalEx;
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
            if (ActiveDirectory.Connector.IsBusy)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Fetching...";
            }
            else if(ActiveDirectory.User_Fetcher.IsBusy)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Refreshing...";
            }
            else 
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "Refresh Users";
            }

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

                if (ActiveDirectory.SelectedUser.Expiring)
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

                if (ActiveDirectory.SelectedUser.LockedOut)
                {
                    UnlockUserButton.IsEnabled = true;
                }
                else
                {
                    UnlockUserButton.IsEnabled = false;
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

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.CreatedDate, null))
                    CreatedDate.Text = ActiveDirectory.SelectedUser.CreatedDate;
                else
                    CreatedDate.Text = " ";

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

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.LockedOut, null))
                    LockedOut.Text = ActiveDirectory.SelectedUser.LockedOut.ToString();
                else
                    LockedOut.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.AccountLockoutTime, null))
                    AccountLockoutTime.Text = ActiveDirectory.SelectedUser.AccountLockoutTime.ToString();
                else
                    AccountLockoutTime.Text = " ";

                if (!ReferenceEquals(ActiveDirectory.SelectedUser.DistinguishedName, null))
                    DistinguishedName.Text = ActiveDirectory.SelectedUser.DistinguishedName.ToString();
                else
                    DistinguishedName.Text = " ";
                
                GroupList.ItemsSource = ActiveDirectory.SelectedUser.Groups;
            }
            else
            {
                ClearInfoGrid();
            }

        }
        /// <summary>
        /// Toggle the blur on the main content
        /// </summary>
        /// <param name="BlurOn"></param>
        private void BackgroundBlur(bool BlurOn)
        {
            if (BlurOn)
            {
                MainContent.Style = FindResource("Blur") as Style;
                MainContent.IsEnabled = false;
            }
            else
            {
                MainContent.Style = FindResource("NoBlur") as Style;
                MainContent.IsEnabled = true;
            }
        }
        /// <summary>
        /// Action to perform when the confirm button is clicked
        /// </summary>
        private string Action;
        /// <summary>
        /// Check and save the new user form, create the user if it all checks out.
        /// </summary>
        private void CreateUser()
        {
            ResultBox.Visibility = Visibility.Hidden;
            string ErrorMessage = null;

            FirstNameText.Style = FindResource("TextBox") as Style;
            UserNameText.Style = FindResource("TextBox") as Style;
            Password.Style = FindResource("PasswordBox") as Style;
            Password_Confirm.Style = FindResource("PasswordBox") as Style;

            bool ChecksOut = true;

            if (Password.Password != Password_Confirm.Password)
            {
                ChecksOut = false;
                Password_Confirm.Style = FindResource("PasswordBox_Error") as Style;
                ErrorMessage = "Passewords don't match";
            }
            if (string.IsNullOrEmpty(Password_Confirm.Password))
            {
                ChecksOut = false;
                Password_Confirm.Style = FindResource("PasswordBox_Error") as Style;
                ErrorMessage = "Please confirm the password";
            }
            if (Password.Password.Length < 8)
            {
                ChecksOut = false;
                Password.Style = FindResource("PasswordBox_Error") as Style;
                ErrorMessage = "Password is less than 8 characters";
            }
            if (string.IsNullOrEmpty(Password.Password))
            {
                ChecksOut = false;
                Password.Style = FindResource("PasswordBox_Error") as Style;
                ErrorMessage = "Password is required";
            }
            if (string.IsNullOrEmpty(UserNameText.Text))
            {
                ChecksOut = false;
                UserNameText.Style = FindResource("TextBox_Error") as Style;
                ErrorMessage = "Username is required";
            }
            if (OUBox.SelectedIndex == -1)
            {
                ChecksOut = false;
                ErrorMessage = "Select an OU";
            }
            if (string.IsNullOrEmpty(FullNameText.Text))
            {
                ChecksOut = false;
                FullNameText.Style = FindResource("TextBox_Error") as Style;
                ErrorMessage = "Full name is required";
            }
            if (string.IsNullOrEmpty(FirstNameText.Text))
            {
                ChecksOut = false;
                FirstNameText.Style = FindResource("TextBox_Error") as Style;
                ErrorMessage = "First name is required";
            }

            if (!ChecksOut)
            {
                if (ErrorMessage != null)
                {
                    ResultMessage.Text = ErrorMessage;
                    ResultBox.Visibility = Visibility.Visible;
                }
                return;
            }
            else
            {
                ActiveDirectory.NewUser.GivenName = FirstNameText.Text;
                ActiveDirectory.NewUser.Surname = LastNameText.Text;
                ActiveDirectory.NewUser.Name = FullNameText.Text;
                ActiveDirectory.NewUser.EmailAddress = EmailText.Text;
                ActiveDirectory.NewUser.Title = Title.Text;
                ActiveDirectory.NewUser.DistinguishedName = OUBox.SelectedValue.ToString();
                ActiveDirectory.NewUser.SamAccountName = UserNameText.Text;
                ActiveDirectory.NewUser.Password = Password.Password;
                Report(ActiveDirectory.NewUser.CreateUser(), "User Create Successfully");

                BackgroundBlur(false);
                NewUserBox.Visibility = Visibility.Hidden;

                FirstNameText.Text = null;
                LastNameText.Text = null;
                FullNameText.Text = null;
                EmailText.Text = null;
                Title.Text = null;
                OUBox.SelectedIndex = -1;
                UserNameText.Text = null;
                Password.Password = null;
                Password_Confirm.Password = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="SuccessMessage"></param>
        /// <returns></returns>
        private bool Report(bool Result, string SuccessMessage)
        {
            ResultBox.Visibility = Visibility.Hidden;

            if (Result)
            {
                ResultMessage.Text = SuccessMessage;
                ResultBox.Visibility = Visibility.Visible;

                if (Action == "ReallyDelete")
                {
                    ClearSelection();
                }

                UpdateSelectedUser();
                UpdateUserButtons();

                return true;
            }
            else
            {
                ResultMessage.Text = ActiveDirectory.ConnectionError;
                ResultBox.Visibility = Visibility.Visible;

                RefreshButton.IsEnabled = false;
                ActiveDirectory.Connect();

                return false;
            }
        }
        #endregion Common Functions

        #region Control Actions
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
        private void Password_Confirm_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateUser();
            }
        }

        #region User Buttons
        /// <summary>
        /// User refresh button action
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            ResultBox.Visibility = Visibility.Hidden;

            RefreshButton.IsEnabled = false;
            RefreshButton.Content = "Refreshing...";

            ActiveDirectory.Refresh("Users");
        }
        /// <summary>
        /// Button action to create a new user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewUserButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundBlur(true);
            NewUserBox.Visibility = Visibility.Visible;
        }
        /// <summary>
        /// Button action to import a user from an OSticket ticket
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportUserButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundBlur(true);
        }
        /// <summary>
        /// Button action to delete the selected user
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundBlur(true);
            WarningBox.Visibility = Visibility.Visible;

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
            if (ActiveDirectory.SelectedUser.Expiring)
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
            BackgroundBlur(true);
            WarningBox.Visibility = Visibility.Visible;

            WarningMessage.Text = "Are you sure you want to disable " + ActiveDirectory.SelectedUser.Name + "?";
            ConfirmButton.Content = "Disable " + ActiveDirectory.SelectedUser.SamAccountName;
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Disable";
        }
        #endregion User Buttons

        #region Password Buttons
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
            BackgroundBlur(true);
            WarningBox.Visibility = Visibility.Visible;
            WarningMessage.Text = "Enter a new password for " + ActiveDirectory.SelectedUser.Name + ":";

            PasswordInput.Visibility = Visibility.Visible;

            ConfirmButton.Content = "Reset Password";
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Reset";
        }
        #endregion Password Buttons

        #region New User Form
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FirstNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FirstNameText.Text.Length > 0 || LastNameText.Text.Length > 0)
            {
                FullNameText.Text = FirstNameText.Text + " " + LastNameText.Text;

                if (FirstNameText.Text.Length > 0)
                    UserNameText.Text = FirstNameText.Text[0] + LastNameText.Text;
                else
                    UserNameText.Text = LastNameText.Text;

                EmailText.Text = UserNameText.Text + "@" + ActiveDirectory.Domain.Name;
            }
            else
            {
                FullNameText.Text = null;
                UserNameText.Text = null;
                EmailText.Text = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastNameText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (FirstNameText.Text.Length > 0 || LastNameText.Text.Length > 0)
            {
                FullNameText.Text = FirstNameText.Text + " " + LastNameText.Text;

                if (FirstNameText.Text.Length > 0)
                    UserNameText.Text = FirstNameText.Text[0] + LastNameText.Text;
                else
                    UserNameText.Text = LastNameText.Text;

                EmailText.Text = UserNameText.Text + "@" + ActiveDirectory.Domain.Name;
            }
            else
            {
                FullNameText.Text = null;
                UserNameText.Text = null;
                EmailText.Text = null;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewUserSaveButton_Click(object sender, RoutedEventArgs e)
        {
            CreateUser();
        }
        private void NewUserCancelButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundBlur(false);
            NewUserBox.Visibility = Visibility.Hidden;
            ResultBox.Visibility = Visibility.Hidden;

            FirstNameText.Text = null;
            LastNameText.Text = null;
            FullNameText.Text = null;
            EmailText.Text = null;
            Title.Text = null;
            OUBox.SelectedIndex = -1;
            UserNameText.Text = null;
            Password.Password = null;
            Password_Confirm.Password = null;
        }
        #endregion New User Form

        #region Warning Message
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
                    BackgroundBlur(true);
                    WarningBox.Visibility = Visibility.Visible;

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
                    NewPassword.Style = FindResource("PasswordBox") as Style;
                    NewPassword_Confirm.Style = FindResource("PasswordBox") as Style;

                    if ((string.IsNullOrEmpty(NewPassword.Password) ^ string.IsNullOrEmpty(NewPassword_Confirm.Password)) || (string.IsNullOrEmpty(NewPassword.Password) && string.IsNullOrEmpty(NewPassword_Confirm.Password)))
                    {
                        if (string.IsNullOrEmpty(NewPassword.Password))
                        {
                            NewPassword.Style = FindResource("PasswordBox_Error") as Style;
                        }
                        if (string.IsNullOrEmpty(NewPassword_Confirm.Password))
                        {
                            NewPassword_Confirm.Style = FindResource("PasswordBox_Error") as Style;
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
                            NewPassword_Confirm.Style = FindResource("PasswordBox_Error") as Style;
                            return;
                        }
                    }

                    NewPassword.Password = null;
                    NewPassword_Confirm.Password = null;
                    break;
                case "Disable":
                    Report(ActiveDirectory.SelectedUser.DisableUser(), "User Disabled Successfully");
                    break;
                default:
                    break;
            }

            BackgroundBlur(false);
            WarningBox.Visibility = Visibility.Hidden;
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
            BackgroundBlur(false);
            WarningBox.Visibility = Visibility.Hidden;
            
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

            PasswordInput.Visibility = Visibility.Collapsed;

            Action = null;
        }
        #endregion Warning Message
        #endregion Control Actions

        #region Event Handlers and Triggers
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
        #endregion Event Handlers and Triggers
    }
}