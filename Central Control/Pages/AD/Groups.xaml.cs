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
        public Groups()
        {
            InitializeComponent();
        }

        #region Page Events
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Groups_Loaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Connector.RunWorkerCompleted += Group_Fetcher_Completed;
            ActiveDirectory.Group_Fetcher.RunWorkerCompleted += Group_Fetcher_Completed;
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
        /// <summary>
        /// When the AD Updater background worker is finished, notify the user or exit the page if disconnected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Group_Fetcher_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            UpdateGroupButtons();

            if (ActiveDirectory.IsConnected)
            {
                ResultMessage.Text = "Group List Updated";
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
        /// Remove event handler watchers when the page is unloaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Groups_Unloaded(object sender, RoutedEventArgs e)
        {
            ActiveDirectory.Group_Fetcher.RunWorkerCompleted -= Group_Fetcher_Completed;
            ActiveDirectory.Connector.RunWorkerCompleted -= Group_Fetcher_Completed;
        }
        #endregion Page Events
        
        #region Common Functions
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
            if (ActiveDirectory.Connector.IsBusy)
            {
                RefreshButton.IsEnabled = false;
                RefreshButton.Content = "Fetching...";
            }
            else if (ActiveDirectory.Group_Fetcher.IsBusy)
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
        /// <summary>
        /// Action to perform when the confirm button is clicked
        /// </summary>
        private string Action;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Result"></param>
        /// <param name="SuccessMessage"></param>
        /// <returns></returns>
        private bool Report(bool Result, string SuccessMessage)
        {
            ResultMessage.Visibility = Visibility.Hidden;

            if (Result)
            {
                ResultMessage.Text = SuccessMessage;
                ResultMessage.Visibility = Visibility.Visible;

                if (Action == "ReallyDelete")
                {
                    ClearSelection();
                }

                UpdateSelectedGroup();
                UpdateGroupButtons();

                return true;
            }
            else
            {
                ResultMessage.Text = ActiveDirectory.ConnectionError;
                ResultMessage.Visibility = Visibility.Visible;

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

        #region Group Buttons
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
            MainContent.Style = FindResource("Blur") as Style;
            MainContent.IsEnabled = false;
            WarningBox.Visibility = Visibility.Visible;

            WarningMessage.Text = "Are you sure you want to delete " + ActiveDirectory.SelectedGroup.Name + "?";
            ConfirmButton.Content = "Delete " + ActiveDirectory.SelectedGroup.SamAccountName;
            ConfirmButton.IsEnabled = true;
            CancelButton.IsEnabled = true;

            Action = "Delete";
        }
        #endregion Group Buttons

        #region Member Buttons
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
        #endregion Member Buttons
        
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
                    MainContent.Style = FindResource("Blur") as Style;
                    MainContent.IsEnabled = false;
                    WarningBox.Visibility = Visibility.Visible;

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
            
            MainContent.Style = FindResource("NoBlur") as Style;
            MainContent.IsEnabled = true;
            WarningBox.Visibility = Visibility.Hidden;
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
            MainContent.Style = FindResource("NoBlur") as Style;
            MainContent.IsEnabled = true;
            WarningBox.Visibility = Visibility.Hidden;
            
            ConfirmButton.IsEnabled = false;
            CancelButton.IsEnabled = false;

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