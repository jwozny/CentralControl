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

namespace Total_Control.User
{
    /// <summary>
    /// Interaction logic for User.xaml
    /// </summary>
    public partial class User : Page
    {
        int searchResult;
        int searchCount;

        public User()
        {
            InitializeComponent();
            if (ActiveDirectory.Users != null)
            {
                BuildList();
            }
            else
            {
                curtain.Visibility = Visibility.Visible;
                syncLabelButton.IsEnabled = true;
                syncLabelButton.Visibility = Visibility.Visible;
            }
        }

        public void BuildList()
        {
            userList.Items.Clear();
            foreach (PSObject User in ActiveDirectory.Users)
            {
                userList.Items.Add(User.Properties["SamAccountName"].Value.ToString());
            }
        }

        private void syncLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            var converter = new BrushConverter();
            var ButtonRed = (Brush)converter.ConvertFromString("#FFFD3333");

            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            syncLabelButton.Style = defaultMouseDownLabelButtonStyle;

            Exception Results = ActiveDirectory.GetAllUsers();

            
            if (Results != null)
            {
                if (Results.Message.Contains("The term 'Get-ADUser' is not recognized as the name of a cmdlet"))
                {
                    syncLabelButton.Foreground = ButtonRed;
                    syncLabelButton.BorderBrush = ButtonRed;
                    if (Environment.OSVersion.ToString().Contains("10.0")
                        || Environment.OSVersion.ToString().Contains("6.3")
                        || Environment.OSVersion.ToString().Contains("6.2")
                        || Environment.OSVersion.ToString().Contains("6.1")
                        || Environment.OSVersion.ToString().Contains("6.0"))
                    {
                        if (System.Windows.Forms.MessageBox.Show(
                            "Remote Server Administrative Tools are missing on this system.\nGo to RSAT download website?\n",
                            "RSAT Missing",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Asterisk) == DialogResult.Yes)
                        {
                            if (Environment.OSVersion.ToString().Contains("10.0") || Environment.OSVersion.ToString().Contains("6.3") || Environment.OSVersion.ToString().Contains("6.2"))
                            {
                                System.Diagnostics.Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=45520");
                            }
                            if (Environment.OSVersion.ToString().Contains("6.1"))
                            {
                                System.Diagnostics.Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=7887");
                            }
                            if (Environment.OSVersion.ToString().Contains("6.0"))
                            {
                                System.Diagnostics.Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=21090");
                            }
                        }
                    }
                    else
                    {
                        if (System.Windows.Forms.MessageBox.Show(
                        "Remote Server Administrative Tools are missing on this system.\nView more details?\n",
                        "RSAT Missing",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Asterisk) == DialogResult.Yes)
                        {
                            System.Diagnostics.Process.Start("https://support.microsoft.com/en-us/kb/2693643");
                        }
                    }

                    syncLabelButton.Content = "Retry Synchronization";
                }
                else
                {
                    syncLabelButton.Background = ButtonRed;
                    if (System.Windows.Forms.MessageBox.Show(
                        Results.ToString() + "\nEmail the Developer?",
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
                curtain.Visibility = Visibility.Collapsed;
                syncLabelButton.Visibility = Visibility.Collapsed;
                BuildList();
            }
        }
        private void syncLabelButton_MouseUp(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            lookupLabelButton.Style = defaultLabelButtonStyle;
        }
        private void syncLabelButton_MouseLeave(object sender, RoutedEventArgs e)
        {
            Style defaultLabelButtonStyle = FindResource("defaultLabelButtonStyle") as Style;
            lookupLabelButton.Style = defaultLabelButtonStyle;
        }

        /* Search Functions */
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
                    Username.Content = " ";
                    Email.Content = " ";
                    Title.Content = " ";
                    Department.Content = " ";
                    Company.Content = " ";
                    CreatedDate.Content = " ";
                    ExpiryDate.Content = " ";
                    LastLogonDate.Content = " ";
                    LockedOut.Content = " ";
                }
            }
            userList.ScrollIntoView(userList.SelectedItem);
        }

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
                if (User.Properties["Name"].Value.ToString().IndexOf(lookupText.Text.ToString(), StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    userList.SelectedItem = User.Properties["SamAccountName"].Value.ToString();
                    return;
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
            foreach (PSObject User in ActiveDirectory.Users)
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
            foreach (PSObject User in ActiveDirectory.Users)
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
            searchCount = 0;
        }

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

        private void userList_MouseDown(object sender, MouseButtonEventArgs e)
        {
            searchCount = 1;
        }
        /* End Search Functions */

        private void saveLabelButton_MouseDown(object sender, RoutedEventArgs e)
        {
            Style defaultMouseDownLabelButtonStyle = FindResource("defaultMouseDownLabelButtonStyle") as Style;
            saveLabelButton.Style = defaultMouseDownLabelButtonStyle;

            //Do stuff here...
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
    }

    public static class ActiveDirectory
    {
        private static Collection<PSObject> users;

        public static Collection<PSObject> Users
        {
            get
            {
                return users;
            }
            set
            {
                users = value;
            }
        }

        public static Exception ExecutePowershell(string Command)
        {
            Runspace runspace = null;
            Pipeline pipeline = null;
            Exception results = null;

            try
            {
                runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                pipeline = runspace.CreatePipeline();
                pipeline.Commands.AddScript(Command);
                users = pipeline.Invoke();
            }
            catch (Exception exception)
            {
                results = exception;
            }
            finally
            {
                if (pipeline != null) pipeline.Dispose();
                if (runspace != null) runspace.Dispose();
            }
            return results;
        }

        public static Exception GetAllUsers()
        {
            return ExecutePowershell("Get-ADUser -Filter * -Properties * | Sort-Object SamAccountName");
        }

        public static Exception EnableUser(string User)
        {
            return ExecutePowershell("Enable-ADAccount -Identity " + User);
        }

        public static Exception DisableUser(string User)
        {
            return ExecutePowershell("Disable-ADAccount -Identity " + User);
        }
    }
}
