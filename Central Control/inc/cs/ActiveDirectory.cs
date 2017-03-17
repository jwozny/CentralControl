using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Serialization;
using WinInterop = System.Windows.Interop;

namespace Central_Control
{
    /// <summary>
    /// Static class to store active directory user info collection and action scripts
    /// </summary>
    public static class ActiveDirectory
    {
        #region Connection Function and Status
        /// <summary>
        /// Connect to AD using current or provided credentials
        /// </summary>
        /// <returns></returns>
        private static bool EstablishConnection()
        {
            try
            {
                if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !(ReferenceEquals(GlobalConfig.Settings.AD_Username, null) ^ ReferenceEquals(GlobalConfig.Settings.AD_Password, null)))
                {
                    Domain = new PrincipalContext(ContextType.Domain, GlobalConfig.Settings.AD_Domain, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                }
                else if (!(ReferenceEquals(GlobalConfig.Settings.AD_Username, null) ^ ReferenceEquals(GlobalConfig.Settings.AD_Password, null)))
                {
                    Domain = new PrincipalContext(ContextType.Domain, System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().ToString(), GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                }
                else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                {
                    Domain = new PrincipalContext(ContextType.Domain, GlobalConfig.Settings.AD_Domain);
                }
                else
                {
                    Domain = new PrincipalContext(ContextType.Domain);
                }
                Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
            }
            catch (COMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (PrincipalServerDownException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.DirectoryServices.Protocols.DirectoryOperationException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.DirectoryServices.ActiveDirectory.ActiveDirectoryObjectNotFoundException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Connect to AD using a background worker and fetch all properties
        /// </summary>
        public static void Connect()
        {
            Connector_Initialize();
            if (!Connector.IsBusy)
            {
                Connector.RunWorkerAsync();
            }
        }
        /// <summary>
        /// Bool defining if the application is communicating with AD
        /// </summary>
        public static bool IsConnected;
        /// <summary>
        /// Error returned from AD connector
        /// </summary>
        public static string ConnectionError;
        #endregion Connection Function and Status

        #region Background Workers

        #region Background Worker - Connector
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker Connector = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void Connector_Initialize()
        {
            Connector.WorkerReportsProgress = true;
            Connector.WorkerSupportsCancellation = true;

            Connector.DoWork -= Connector_DoWork;

            if (!Connector.IsBusy)
            {
                Connector.DoWork += Connector_DoWork;
            }
        }
        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Connector_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            Connector.ReportProgress(0, "Connecting...");
            IsConnected = EstablishConnection();

            if (IsConnected)
            {
                if (!Connector.CancellationPending) RefreshUsers(Connector);
                if (!Connector.CancellationPending) RefreshGroups(Connector);
            }
            
            if (Connector.CancellationPending)
            {
                IsConnected = false;
                ConnectionError = "Connection Canceled";
            }
        }
        #endregion Background Worker - Connector

        #region Background Worker - Updater_Users
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker Updater_Users = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void Updater_Users_Initialize()
        {
            Updater_Users.WorkerReportsProgress = true;
            Updater_Users.WorkerSupportsCancellation = true;

            Updater_Users.DoWork += Updater_Users_DoWork;
        }
        /// <summary>
        /// Updater worker to only refresh users from AD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Updater_Users_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            IsConnected = EstablishConnection();
            if (IsConnected) RefreshUsers(Updater_Users);
        }
        #endregion Background Worker - Updater_Users

        #region Background Worker - Updater_Groups
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker Updater_Groups = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void Updater_Groups_Initialize()
        {
            Updater_Groups.WorkerReportsProgress = true;
            Updater_Groups.WorkerSupportsCancellation = true;

            Updater_Groups.DoWork += Updater_Groups_DoWork;
        }
        /// <summary>
        /// Updater worker to only refresh groups from AD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Updater_Groups_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            IsConnected = EstablishConnection();
            if (IsConnected) RefreshGroups(Updater_Groups);
        }
        #endregion Background Worker - Updater_Groups

        #endregion Background Workers

        #region Domain Properties
        /// <summary>
        /// Current domain
        /// </summary>
        public static PrincipalContext Domain;
        /// <summary>
        /// Domain searcher
        /// </summary>
        private static PrincipalSearcher Searcher;
        /// <summary>
        /// Temporary reference to the count of users found by the searcher
        /// </summary>
        public static int UserCount;
        /// <summary>
        /// Temporary reference to the count of groups found by the searcher
        /// </summary>
        public static int GroupCount;
        /// <summary>
        /// List of AD users
        /// </summary>
        public static List<UserPrincipalEx> Users { get; set; } = new List<UserPrincipalEx>();
        public static UserPrincipalEx SelectedUser;
        /// <summary>
        /// List of AD groups
        /// </summary>
        public static List<GroupPrincipalEx> Groups { get; set; } = new List<GroupPrincipalEx>();
        public static GroupPrincipalEx SelectedGroup;
        #endregion Domain Properties

        #region Refresh Functions
        /// <summary>
        /// Refresh everything from AD or a specific item
        /// </summary>
        /// <param name="filter">null, "Users", "Groups"</param>
        public static void Refresh(string filter)
        {
            switch (filter)
            {
                case "Users":
                    Updater_Users_Initialize();
                    if (!Updater_Users.IsBusy)
                    {
                        Updater_Users.RunWorkerAsync();
                    }
                    break;
                case "Groups":
                    Updater_Groups_Initialize();
                    if (!Updater_Groups.IsBusy)
                    {
                        Updater_Groups.RunWorkerAsync();
                    }
                    break;
                default:
                    Updater_Users_Initialize();
                    if (!Updater_Users.IsBusy)
                    {
                        Updater_Users.RunWorkerAsync();
                    }
                    Updater_Groups_Initialize();
                    if (!Updater_Groups.IsBusy)
                    {
                        Updater_Groups.RunWorkerAsync();
                    }
                    break;
            }
        }
        /// <summary>
        /// Clear the list of users and repopulate it from AD
        /// </summary>
        private static void RefreshUsers(BackgroundWorker Worker)
        {
            try
            {
                Searcher.Dispose();
                Searcher = new PrincipalSearcher(new UserPrincipal(Domain));

                Worker.ReportProgress(0, "Finding Users");
                if (Worker.CancellationPending) return;

                Users.Clear();
                UserCount = Searcher.FindAll().Count();

                Worker.ReportProgress(0, "Retrieving User");
                if (Worker.CancellationPending) return;

                foreach (UserPrincipal User in Searcher.FindAll())
                {
                    if (User != null)
                    {
                        Users.Add(UserPrincipalEx.FindByIdentity(Domain, IdentityType.SamAccountName, User.SamAccountName));
                    }
                    Worker.ReportProgress(0, "Retrieving User");
                    if (Worker.CancellationPending) return;
                }

                Users.Sort((x, y) => x.Name.CompareTo(y.Name));

                UserCount = 0;
                foreach (UserPrincipalEx User in Users)
                {
                    var sb = new StringBuilder();

                    foreach (var propertyInfo in
                        from p in typeof(UserPrincipalEx).GetProperties()
                        where Equals(p.PropertyType, typeof(String))
                        select p)
                    {
                        sb.AppendLine(propertyInfo.GetValue(User, null) + " ");
                    }

                    User.CreatedDate = User.GetProperty("whenCreated");

                    if (!ReferenceEquals(User.AccountExpirationDate, null))
                        if (DateTime.Compare(User.AccountExpirationDate.Value, DateTime.Now.AddDays(8)) <= 0)
                            User.Expiring = true;

                    User.LockedOut = User.IsAccountLockedOut();

                    User.Groups = User.GetGroups().OrderBy(GroupItem => GroupItem.Name).ToList();

                    UserCount++;
                    Worker.ReportProgress(0, "Retrieving User Properties");
                    if (Worker.CancellationPending) return;
                }
            }
            catch
            {
                IsConnected = EstablishConnection();
            }
        }
        /// <summary>
        /// Clear the list of groups and repopulate it from AD
        /// </summary>
        private static void RefreshGroups(BackgroundWorker Worker)
        {
            try
            {
                Searcher.Dispose();
                Searcher = new PrincipalSearcher(new GroupPrincipal(Domain));

                Worker.ReportProgress(0, "Finding Groups");
                if (Worker.CancellationPending) return;

                Groups.Clear();
                GroupCount = Searcher.FindAll().Count();

                Worker.ReportProgress(0, "Retrieving Group");
                if (Worker.CancellationPending) return;

                foreach (GroupPrincipal Group in Searcher.FindAll())
                {
                    if (Group != null)
                    {
                        Groups.Add(GroupPrincipalEx.FindByIdentity(Domain, IdentityType.SamAccountName, Group.SamAccountName));
                    }
                    Worker.ReportProgress(0, "Retrieving Group");
                    if (Worker.CancellationPending) return;
                }

                Groups.Sort((x, y) => x.Name.CompareTo(y.Name));

                GroupCount = 0;
                foreach (GroupPrincipalEx Group in Groups)
                {
                    List<Member> Members = new List<Member>(1);
                    Members.Clear();

                    DirectoryEntry thisGroup = Group.GetUnderlyingObject() as DirectoryEntry;
                    PropertyValueCollection pvcMembers = thisGroup.Properties["Member"];
                    foreach (object pvcMember in pvcMembers)
                    {
                        DirectoryEntry deMember;
                        if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                        {
                            deMember = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + pvcMember.ToString(), GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                        }
                        else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                        {
                            deMember = new DirectoryEntry("LDAP://" + pvcMember.ToString(), GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                        }
                        else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                        {
                            deMember = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + pvcMember.ToString());
                        }
                        else
                        {
                            deMember = new DirectoryEntry("LDAP://" + pvcMember.ToString());
                        }

                        Members.Add(new Member()
                        {
                            Name = deMember.Properties["Name"][0].ToString(),
                            DistinguishedName = pvcMember.ToString(),
                            SchemaClassName = deMember.SchemaClassName.ToUpperInvariant()
                        });
                    }

                    Members.Sort((x, y) => x.Name.CompareTo(y.Name));
                    Group.AllMembers = Members;

                    var sb = new StringBuilder();

                    foreach (var propertyInfo in
                        from p in typeof(GroupPrincipalEx).GetProperties()
                        where Equals(p.PropertyType, typeof(String))
                        select p)
                    {
                        sb.AppendLine(propertyInfo.GetValue(Group, null) + " ");
                    }

                    GroupCount++;
                    Worker.ReportProgress(0, "Retrieving Group Members");
                    if (Worker.CancellationPending) return;
                }
            }
            catch
            {
                IsConnected = EstablishConnection();
            }
        }
        #endregion Refresh Functions

        #region Extended User Functions
        /// <summary>
        /// Get additional properties not included in the User Principal object
        /// </summary>
        /// <param name="principal"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        private static string GetProperty(this Principal principal, string property)
        {
            DirectoryEntry directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
            if (directoryEntry.Properties.Contains(property))
                return directoryEntry.Properties[property].Value.ToString();
            else
                return string.Empty;
        }
        /// <summary>
        /// Template action on modifying an AD user
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool SaveUser(this UserPrincipal User)
        {
            try
            {
                DirectoryEntry usr;
                if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                {
                    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                }
                else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                {
                    usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                }
                else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                {
                    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                }
                else
                {
                    usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                }

                int val = (int)usr.Properties["userAccountControl"].Value;
                usr.Properties["userAccountControl"].Value = val | 0x2;
                //ADS_UF_ACCOUNTDISABLE

                usr.CommitChanges();
                usr.Close();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Refresh the user locally from the store
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool RefreshUser(this UserPrincipal User)
        {
            try
            {
                for (int i=0; i < Users.Count; i++)
                {
                    if (Users[i].SamAccountName == User.SamAccountName)
                    {
                        Users[i] = UserPrincipalEx.FindByIdentity(Domain, IdentityType.SamAccountName, User.SamAccountName);
                        break;
                    }
                }
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        /// <summary>
        /// Remove the account expiration
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool DeleteUser(this UserPrincipal User)
        {
            try
            {
                for (int i = 0; i < Users.Count; i++)
                {
                    if (Users[i].SamAccountName == User.SamAccountName)
                    {
                        Users.RemoveAt(i);
                    }
                }
                User.Delete();
                SelectedUser = null;
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            return true;
        }
        /// <summary>
        /// Enable a user account
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool EnableUser(this UserPrincipal User)
        {
            try
            {
                User.Enabled = true;
                User.Save();
            }
            catch
            {
                try
                {
                    DirectoryEntry usr;
                    if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                    }
                    else
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                    }

                    int val = (int)usr.Properties["userAccountControl"].Value;
                    usr.Properties["userAccountControl"].Value = val & ~0x2;
                    //ADS_UF_ACCOUNTDISABLE

                    usr.CommitChanges();
                    usr.Close();
                }
                catch (Exception E)
                {
                    ConnectionError = E.Message.ToString();
                    return false;
                }
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Add X days to the account expiration
        /// </summary>
        /// <param name="User"></param>
        /// <param name="Days"></param>
        /// <returns></returns>
        public static bool AddExpiry(this UserPrincipal User, int Days)
        {
            try
            {
                User.AccountExpirationDate = DateTime.Now.AddDays(Days);
                User.Save();
            }
            catch
            {
                try
                {
                    //DirectoryEntry user = User.GetUnderlyingObject() as DirectoryEntry;
                    DirectoryEntry usr;
                    if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                    }
                    else
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                    }

                    DateTime expire = System.DateTime.Now.AddDays(Days);
                    usr.Properties["accountExpires"].Value = Convert.ToString((Int64)expire.ToFileTime());

                    usr.CommitChanges();
                    usr.Close();
                }
                catch (Exception E)
                {
                    ConnectionError = E.Message.ToString();
                    return false;
                }
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Remove the account expiration
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool RemoveExpiry(this UserPrincipal User)
        {
            try
            {
                User.AccountExpirationDate = null;
                User.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Unlock a user
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool UnlockUser(this UserPrincipal User)
        {
            try
            {
                User.UnlockAccount();
                User.RefreshExpiredPassword();
                User.Save();
            }
            catch
            {
                try
                {
                    DirectoryEntry usr;
                    if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                    }
                    else
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                    }

                    usr.Properties["LockOutTime"].Value = 0;

                    usr.CommitChanges();
                    usr.Close();
                }
                catch (Exception E)
                {
                    ConnectionError = E.Message.ToString();
                    return false;
                }
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Disable a user account
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool DisableUser(this UserPrincipal User)
        {
            try
            {
                User.Enabled = false;
                User.Save();
            }
            catch
            {
                try
                {
                    DirectoryEntry usr;
                    if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                    }
                    else
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                    }

                    int val = (int)usr.Properties["userAccountControl"].Value;
                    usr.Properties["userAccountControl"].Value = val | 0x2;
                    //ADS_UF_ACCOUNTDISABLE

                    usr.CommitChanges();
                    usr.Close();
                }
                catch (Exception E)
                {
                    ConnectionError = E.Message.ToString();
                    return false;
                }
            }
            
            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Set so that the user's password expires
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool AddPasswordExpiry(this UserPrincipal User)
        {
            try
            {
                User.PasswordNeverExpires = false;
                User.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Set so that the user's password never expires
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool RemovePasswordExpiry(this UserPrincipal User)
        {
            try
            {
                User.PasswordNeverExpires = true;
                User.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Refresh a user's password expiration
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool RefreshPassword(this UserPrincipal User)
        {
            try
            {
                User.RefreshExpiredPassword();
                User.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Expire a user's password, forcing a password change on next login
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool ExpirePassword(this UserPrincipal User)
        {
            try
            {
                User.ExpirePasswordNow();
                User.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            User.RefreshUser();
            return true;
        }
        /// <summary>
        /// Reset a user's password with given string
        /// </summary>
        /// <param name="User"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public static bool ResetUser(this UserPrincipal User, string newPassword)
        {
            try
            {
                User.SetPassword(newPassword);
                User.RefreshExpiredPassword();
                User.Save();
            }
            catch
            {
                try
                {
                    DirectoryEntry usr;
                    if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
                    }
                    else if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null))
                    {
                        usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName);
                    }
                    else
                    {
                        usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
                    }

                    usr.Invoke("SetPassword", new object[] { newPassword });
                    usr.Properties["LockOutTime"].Value = 0; //unlock account

                    usr.Close();
                }
                catch (Exception E)
                {
                    ConnectionError = E.Message.ToString();
                    return false;
                }
            }

            User.RefreshUser();
            return true;
        }
        #endregion Extended User Functions

        #region Extended Group Functions
        /* Group Extended Functions */
        /// <summary>
        /// Delete the selected groups from the store
        /// </summary>
        /// <param name="Group"></param>
        /// <returns></returns>
        public static bool DeleteGroup(this GroupPrincipal Group)
        {
            try
            {
                for (int i = 0; i < Groups.Count; i++)
                {
                    if (Groups[i].SamAccountName == Group.SamAccountName)
                    {
                        Groups.RemoveAt(i);
                    }
                }
                Group.Delete();
                SelectedGroup = null;
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            return true;
        }
        /// <summary>
        /// Remove the user from the selected group
        /// </summary>
        /// <param name="DistinguishedName">DistinguishedName</param>
        /// <returns></returns>
        public static bool RemoveMember(this GroupPrincipal Group, string DistinguishedName)
        {
            try
            {
                Group.Members.Remove(Domain, IdentityType.DistinguishedName, DistinguishedName);
                Group.Save();
            }
            catch (Exception E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }

            return true;
        }
        #endregion Extended Group Functions

        #region Custom Classes

        #region UserPrincipalEx
        [DirectoryRdnPrefix("CN")]
        [DirectoryObjectClass("user")]
        public class UserPrincipalEx : UserPrincipal
        {
            // Inplement the constructor using the base class constructor. 
            public UserPrincipalEx(PrincipalContext context) : base(context) { }

            // Implement the constructor with initialization parameters.    
            public UserPrincipalEx(PrincipalContext context, string samAccountName, string password, bool enabled) : base(context, samAccountName, password, enabled) { }

            // Implement the overloaded search method FindByIdentity.
            public static new UserPrincipalEx FindByIdentity(PrincipalContext context, string identityValue)
            {
                return (UserPrincipalEx)FindByIdentityWithType(context, typeof(UserPrincipalEx), identityValue);
            }

            // Implement the overloaded search method FindByIdentity. 
            public static new UserPrincipalEx FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
            {
                return (UserPrincipalEx)FindByIdentityWithType(context, typeof(UserPrincipalEx), identityType, identityValue);
            }

            // Create the "Title" property.    
            [DirectoryProperty("title")]
            public string Title
            {
                get
                {
                    if (ExtensionGet("title").Length != 1)
                        return string.Empty;

                    return (string)ExtensionGet("title")[0];
                }
                set { ExtensionSet("title", value); }
            }

            // Create the "Department" property.    
            [DirectoryProperty("department")]
            public string Department
            {
                get
                {
                    if (ExtensionGet("department").Length != 1)
                        return string.Empty;

                    return (string)ExtensionGet("department")[0];
                }
                set { ExtensionSet("department", value); }
            }

            // Create the "Company" property.    
            [DirectoryProperty("company")]
            public string Company
            {
                get
                {
                    if (ExtensionGet("company").Length != 1)
                        return string.Empty;

                    return (string)ExtensionGet("company")[0];
                }
                set { ExtensionSet("company", value); }
            }

            // Create the "CreatedDate" property.    
            [DirectoryProperty("createddate")]
            public string CreatedDate { get; set; }

            // Create the "Expiring" property.    
            [DirectoryProperty("expiring")]
            public bool Expiring { get; set; } = false;

            // Create the "LockedOut" property.    
            [DirectoryProperty("lockedout")]
            public bool LockedOut { get; set; } = false;

            // Create the "Groups" property.    
            [DirectoryProperty("groups")]
            public List<Principal> Groups { get; set; } = new List<Principal>();
        }
        #endregion UserPrincipalEx

        #region GroupPrincipalEx
        [DirectoryRdnPrefix("CN")]
        [DirectoryObjectClass("group")]
        public class GroupPrincipalEx : GroupPrincipal
        {
            // Inplement the constructor using the base class constructor. 
            public GroupPrincipalEx(PrincipalContext context) : base(context) { }

            // Implement the constructor with initialization parameters.    
            public GroupPrincipalEx(PrincipalContext context, string samAccountName) : base(context, samAccountName) { }

            // Implement the overloaded search method FindByIdentity.
            public static new GroupPrincipalEx FindByIdentity(PrincipalContext context, string identityValue)
            {
                return (GroupPrincipalEx)FindByIdentityWithType(context, typeof(GroupPrincipalEx), identityValue);
            }

            // Implement the overloaded search method FindByIdentity. 
            public static new GroupPrincipalEx FindByIdentity(PrincipalContext context, IdentityType identityType, string identityValue)
            {
                return (GroupPrincipalEx)FindByIdentityWithType(context, typeof(GroupPrincipalEx), identityType, identityValue);
            }

            // Create the "Members" property.    
            [DirectoryProperty("allmembers")]
            public List<Member> AllMembers { get; set; } = new List<Member>();
            
            // Create the "Email" property.    
            [DirectoryProperty("email")]
            public string Email
            {
                get
                {
                    if (ExtensionGet("email").Length != 1)
                        return string.Empty;

                    return (string)ExtensionGet("email")[0];
                }
                set { ExtensionSet("email", value); }
            }
        }
        #endregion GroupPrincipalEx

        /// <summary>
        /// Custom member class for GroupPrincipalEx
        /// </summary>
        public class Member
        {
            public string Name { get; set; }
            public string DistinguishedName { get; set; }
            public string SchemaClassName { get; set; }
        }

        #endregion Custom Classes
    }
}
