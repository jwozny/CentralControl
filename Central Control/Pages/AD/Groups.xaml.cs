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
    /// Interaction logic for Groups.xaml
    /// </summary>
    public partial class Groups : Page
    {
        /// <summary>
        /// Primary function
        /// </summary>
        public Groups()
        {
            InitializeComponent();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Groups_Loaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Updater_Groups.RunWorkerCompleted += Updater_Groups_Completed;
            UpdateGroupButtons();

            if (!ReferenceEquals(ActiveDirectory.Groups, null) && ActiveDirectory.IsConnected)
            {
                GroupList.ItemsSource = ActiveDirectory.Groups;
                SearchBox.Focus();

                ICollectionView GroupView = CollectionViewSource.GetDefaultView(ActiveDirectory.Groups);
                new TextSearchFilter(GroupView, SearchBox);
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
            GroupList.SelectedIndex = -1;
            
            string tmp = SearchBox.Text;
            SearchBox.Text = " ";
            SearchBox.Text = null;
            SearchBox.Text = tmp;
        }
        /// <summary>
        /// Reset the group action buttons with the appropriate actions for the selected group
        /// </summary>
        private void ClearInfoGrid()
        {
            Name.Text = string.Empty;
            Username.Text = string.Empty;
            DistinguishedName.Text = string.Empty;

            MemberList.ItemsSource = null;
        }
        /// <summary>
        /// Set the selected group to the selected group variable
        /// </summary>
        private void UpdateSelectedGroup()
        {
            if ((GroupList.SelectedItem != null) && (GroupList.SelectedIndex != -1))
            {
                foreach (ActiveDirectory.GroupPrincipalEx Group in ActiveDirectory.Groups)
                {
                    if (Group.Name == GroupList.SelectedItem.ToString())
                    {
                        ActiveDirectory.SelectedGroup = Group;
                    }
                }
            }
            else
            {
                ActiveDirectory.SelectedGroup = null;
            }
        }
        /// <summary>
        /// Reset the group action buttons with the appropriate actions for the selected group
        /// </summary>
        private void UpdateGroupButtons()
        {
            if (ActiveDirectory.Updater_Groups.IsBusy)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Refreshing...";
            }
            else
            {
                RefreshButton.IsEnabled = true;
                RefreshButton.Content = "Refresh Groups";
            }

            if (ActiveDirectory.SelectedGroup != null)
            {
                /* Group Buttons */
                DeleteGroupButton.IsEnabled = true;

                /* Member Buttons */
            }
            else
            {
                /* Group Buttons */
                DeleteGroupButton.IsEnabled = false;

                /* Member Buttons */
            }
        }
        /// <summary>
        /// Reset the group action buttons with the appropriate actions for the selected group
        /// </summary>
        private void UpdateMemberButtons()
        {
            if ((MemberList.SelectedItem != null) && (MemberList.SelectedIndex != -1))
            {
                RemoveMemberButton.IsEnabled = true;
            }
            else
            {
                RemoveMemberButton.IsEnabled = false;
            }
        }
        /// <summary>
        /// Reset the user info for the selected user
        /// </summary>
        private void UpdateGroupInfo()
        {
            if (ActiveDirectory.SelectedGroup != null)
            {
                Name.Text = ActiveDirectory.SelectedGroup.Name;

                Username.Text = ActiveDirectory.SelectedGroup.SamAccountName;

                if (!ReferenceEquals(ActiveDirectory.SelectedGroup.DistinguishedName, null))
                    DistinguishedName.Text = ActiveDirectory.SelectedGroup.DistinguishedName.ToString();
                else
                    DistinguishedName.Text = " ";

                MemberList.ItemsSource = ActiveDirectory.SelectedGroup.AllMembers;
            }
            else
            {
                ClearInfoGrid();
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
        /// Displays more info on the selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GroupList.ScrollIntoView(GroupList.SelectedItem);
            
            UpdateSelectedGroup();
            UpdateGroupInfo();
            UpdateGroupButtons();
        }
        /// <summary>
        /// Clear the selection when {ESC} is pressed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupList_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SearchBox.Text = string.Empty;
                ClearSelection();
            }
        }
        /// <summary>
        /// Enabled the group member action buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MemberList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMemberButtons();
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

            ActiveDirectory.Refresh("Groups");
        }

        /* User Button Actions */
        /// <summary>
        /// Button action to create a new group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGroupButton_Click(object sender, RoutedEventArgs e)
        {
        }
        /// <summary>
        /// Button action to delete the selected group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteGroupButton_Click(object sender, RoutedEventArgs e)
        {
            Warning.Visibility = Visibility.Visible;

            WarningMessage.Text = "Are you sure you want to delete " + ActiveDirectory.SelectedGroup.Name + "?";
            ConfirmButton.Content = "Delete " + ActiveDirectory.SelectedGroup.SamAccountName;
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Delete";
        }
        /// <summary>
        /// Button action to add a member to the group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddMemberButton_Click(object sender, RoutedEventArgs e)
        {
            //TODO: Show principal searcher/selector
        }
        /// <summary>
        /// Button action to remove a member to the group
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveMemberButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (ActiveDirectory.Member Member in ActiveDirectory.SelectedGroup.AllMembers)
            {
                if (Member.Name == MemberList.SelectedItem.ToString())
                {
                    Report(ActiveDirectory.SelectedGroup.RemoveMember(Member.DistinguishedName), "Member Removed Successfully");
                    return;
                }
            }
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

                    WarningMessage.Text = "This cannot be undone! Are you super sure to DELETE " + ActiveDirectory.SelectedGroup.Name + "?";
                    ConfirmButton.Content = "DELETE " + ActiveDirectory.SelectedGroup.SamAccountName;
                    ConfirmButton.IsEnabled = true;
                    CancelButton.IsEnabled = true;

                    Action = "ReallyDelete";
                    return;
                case "ReallyDelete":
                    Report(ActiveDirectory.SelectedGroup.DeleteGroup(), "Group Deleted Successfully");
                    break;
                default:
                    break;
            }

            Warning.Visibility = Visibility.Hidden;
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

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

                UpdateSelectedGroup();
                UpdateGroupButtons();

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
        /// When the AD Updater background worker is finished, notify the user or exit the page if disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Updater_Groups_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateGroupButtons();

            if (ActiveDirectory.IsConnected)
            {
                ResultMessage.Content = "Group List Updated";
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
        private void Groups_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Updater_Groups.RunWorkerCompleted -= Updater_Groups_Completed;
        }
    }
}