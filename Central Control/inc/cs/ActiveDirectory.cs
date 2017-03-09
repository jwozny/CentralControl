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
        /* Connection Functions */
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
            GetUserProperties_Initialize();
            return true;
        }
        /// <summary>
        /// Connect to AD using a background worker and fetch all properties
        /// </summary>
        public static void Connect()
        {
            Connector_Initialize(null);
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
        
        /* Background Worker - ConnectAD */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker Connector = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void Connector_Initialize(string filter)
        {
            Connector.WorkerReportsProgress = true;
            Connector.WorkerSupportsCancellation = true;

            switch (filter)
            {
                case "Users":
                    Connector.DoWork += Connector_DoWork_Users;
                    break;
                case "Groups":
                    Connector.DoWork += Connector_DoWork_Groups;
                    break;
                default:
                    Connector.DoWork += Connector_DoWork;
                    break;
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
            Connector.ReportProgress(5);
            IsConnected = EstablishConnection();

            if (IsConnected)
            {
                RefreshUsers();
                RefreshGroups();
            }
        }
        /// <summary>
        /// Connector worker to only refresh users from AD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Connector_DoWork_Users(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            IsConnected = EstablishConnection();
            if (IsConnected) RefreshUsers();
        }
        /// <summary>
        /// Connector worker to only refresh groups from AD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Connector_DoWork_Groups(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            IsConnected = EstablishConnection();
            if (IsConnected) RefreshGroups();
        }

        /* Domain Properties */
        /// <summary>
        /// Current domain
        /// </summary>
        private static PrincipalContext Domain;
        /// <summary>
        /// Domain searcher
        /// </summary>
        private static PrincipalSearcher Searcher;
        /// <summary>
        /// List of AD users
        /// </summary>
        public static List<UserPrincipalEx> Users { get; set; } = new List<UserPrincipalEx>();
        /// <summary>
        /// List of AD groups
        /// </summary>
        public static List<GroupPrincipal> Groups { get; set; }

        /* Refresh Functions */
        /// <summary>
        /// Refresh everything from AD or a specific item
        /// </summary>
        /// <param name="filter">null, "users", "groups"</param>
        public static void Refresh(string filter)
        {
            Connector_Initialize(filter);
            if (!Connector.IsBusy)
            {
                Connector.RunWorkerAsync();
            }
        }
        /// <summary>
        /// Clear the list of users and repopulate it from AD
        /// </summary>
        private static void RefreshUsers()
        {
            Connector.ReportProgress(10);

            Searcher.Dispose();
            Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
                        
            UserPrincipalEx Test = UserPrincipalEx.FindByIdentity(Domain, IdentityType.SamAccountName, "jwozny");
            Users.Clear();

            Connector.ReportProgress(20);

            foreach (UserPrincipal User in Searcher.FindAll())
            {
                if (User != null)
                {
                    Users.Add(UserPrincipalEx.FindByIdentity(Domain, IdentityType.SamAccountName, User.SamAccountName));
                }
            }

            Users.Sort((x, y) => x.Name.CompareTo(y.Name));

            Connector.ReportProgress(30);

            foreach (UserPrincipalEx User in Users)
            {
                var sb = new StringBuilder();

                foreach (var propertyInfo in
                    from p in typeof(ActiveDirectory.UserPrincipalEx).GetProperties()
                    where Equals(p.PropertyType, typeof(String))
                    select p)
                {
                    sb.AppendLine(propertyInfo.GetValue(User, null) + " ");
                }

                string str = sb.ToString();
            }
        }
        /// <summary>
        /// Clear the list of groups and repopulate it from AD
        /// </summary>
        private static void RefreshGroups()
        {
            Connector.ReportProgress(50);
            Searcher.Dispose();
            Searcher = new PrincipalSearcher(new GroupPrincipal(Domain));

            Connector.ReportProgress(60);
            Groups = (from principal in Searcher.FindAll() select principal as GroupPrincipal).OrderBy(Groups => Groups.SamAccountName).ToList();
        }
        
        /* Selected User Properties */
        public static UserPrincipalEx SelectedUser;
        public static UserPrincipalEx CurrentBackgroundUser;
        public static string SelectedUser_CreatedDate;
        public static bool SelectedUser_IsAccountLockedOut;
        /// <summary>
        /// List of groups that the selected user belongs to
        /// </summary>
        public static List<Principal> SelectedUser_Groups;
        /// <summary>
        /// Checks if a string exists, returns false if string/property doesn't exist, is null, or empty
        /// </summary>
        /// <param name="stringItem"></param>
        /// <returns></returns>
        public static bool Exists(this string stringItem)
        {
            if (ReferenceEquals(stringItem, null)) return false;
            if (stringItem == null) return false;
            if (stringItem == "") return false;
            if (stringItem == string.Empty) return false;
            return true;
        }

        /* Functions for Extra Properties */
        private static string GetProperty(this Principal principal, string property)
        {
            DirectoryEntry directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
            if (directoryEntry.Properties.Contains(property))
                return directoryEntry.Properties[property].Value.ToString();
            else
                return string.Empty;
        }
        public static string GetCreatedDate(this Principal principal)
        {
            return principal.GetProperty("whenCreated");
        }
        /// <summary>
        /// Check if a user account is or will be expired in 8 days
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool IsAccountExpired(this UserPrincipal User)
        {
            if (!ReferenceEquals(User.AccountExpirationDate, null))
            {
                if (DateTime.Compare(User.AccountExpirationDate.Value, DateTime.Now.AddDays(8)) > 0)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /* Background Worker - GetUserProperties */
        /// <summary>
        /// Create background worker instance to pull extra user properties
        /// </summary>
        public static BackgroundWorker GetUserProperties = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void GetUserProperties_Initialize()
        {
            GetUserProperties.WorkerReportsProgress = true;
            GetUserProperties.WorkerSupportsCancellation = true;
            GetUserProperties.DoWork += GetUserProperties_DoWork;
        }
        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void GetUserProperties_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            CurrentBackgroundUser = SelectedUser;
            
            SelectedUser_CreatedDate = string.Empty;
            SelectedUser_IsAccountLockedOut = false;
            SelectedUser_Groups = null;
            
            SelectedUser_CreatedDate = CurrentBackgroundUser.GetCreatedDate();
            GetUserProperties.ReportProgress(60);
            SelectedUser_IsAccountLockedOut = CurrentBackgroundUser.IsAccountLockedOut();
            GetUserProperties.ReportProgress(75);
            
            SelectedUser_Groups = CurrentBackgroundUser.GetGroups().OrderBy(GroupItem => GroupItem.Name).ToList();
        }

        /* Action Functions */
        /// <summary>
        /// Template action on modifying an AD user
        /// </summary>
        /// <param name="User"></param>
        /// <returns></returns>
        public static bool SaveUser(this UserPrincipal User)
        {
            //try
            //{
            //DirectoryEntry usr;
            //if (!ReferenceEquals(GlobalConfig.Settings.AD_Domain, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Username, null) && !ReferenceEquals(GlobalConfig.Settings.AD_Password, null))
            //{
            //    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, GlobalConfig.Settings.AD_Username, GlobalConfig.Settings.AD_Password);
            //}
            //else
            //{
            //    usr = new DirectoryEntry("LDAP://" + User.DistinguishedName);
            //}
            //    int val = (int)usr.Properties["userAccountControl"].Value;
            //    usr.Properties["userAccountControl"].Value = val | 0x2;
            //    //ADS_UF_ACCOUNTDISABLE

            //    usr.CommitChanges();
            //    usr.Close();
            //}
            //catch (System.DirectoryServices.DirectoryServicesCOMException E)
            //{
            //ConnectionError = E.Message.ToString();
            //    return false;
            //}
            //catch (System.Runtime.InteropServices.COMException E)
            //{
            //    ConnectionError = E.Message.ToString();
            //    return false;
            //}
            //return true;
            return false;
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
        }
    }
}
