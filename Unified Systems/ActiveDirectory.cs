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
using Unified_Systems.User;
using WinInterop = System.Windows.Interop;

namespace Unified_Systems
{
    /// <summary>
    /// Static class to store active directory user info collection and action scripts
    /// </summary>
    public static class ActiveDirectory
    {
        public static string DomainName { get; set; }
        public static string AuthenticatingUsername { internal get; set; }
        public static string AuthenticatingPassword { internal get; set; }

        public static bool Exists(this string stringItem)
        {
            if (ReferenceEquals(stringItem, null)) return false;
            if (stringItem == null) return false;
            if (stringItem == "") return false;
            if (stringItem == string.Empty) return false;
            return true;
        }

        public static bool InitializeDomain()
        {
            try
            {
                if (!ReferenceEquals(DomainName, null) && !(ReferenceEquals(AuthenticatingUsername, null) ^ ReferenceEquals(AuthenticatingPassword, null)))
                {
                    Domain = new PrincipalContext(ContextType.Domain, DomainName, AuthenticatingUsername, AuthenticatingPassword);
                }
                else if (!(ReferenceEquals(AuthenticatingUsername, null) ^ ReferenceEquals(AuthenticatingPassword, null)))
                {
                    Domain = new PrincipalContext(ContextType.Domain, System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain().ToString(), AuthenticatingUsername, AuthenticatingPassword);
                }
                else if (!ReferenceEquals(DomainName, null))
                {
                    Domain = new PrincipalContext(ContextType.Domain, DomainName);
                }
                else
                {
                    Domain = new PrincipalContext(ContextType.Domain);
                }
                Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
                Users = new List<UserPrincipal>();
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
        public static void Connect()
        {
            ConnectAD_Initialize();
            if (!ConnectAD.IsBusy)
            {
                ConnectAD.RunWorkerAsync();
            }
        }
        public static bool IsConnected;
        public static string ConnectionError;

        public static PrincipalContext Domain;
        public static PrincipalSearcher Searcher;
        public static IEnumerable<UserPrincipal> RawUsers { get; set; } 
        public static List<UserPrincipal> Users { get; set; }

        public static UserPrincipal SelectedUser;
        public static UserPrincipal CurrentBackgroundUser;
        public static string SelectedUser_Title;
        public static string SelectedUser_Department;
        public static string SelectedUser_Company;
        public static string SelectedUser_CreatedDate;
        public static bool SelectedUser_IsAccountLockedOut;
        public static PrincipalSearchResult<Principal> SelectedUser_Groups;

        public static void RefreshUsers()
        {
            Searcher.Dispose();
            Searcher = new PrincipalSearcher(new UserPrincipal(Domain));
            //if (!ReferenceEquals(Users, null))
            //{
            //    Users.Clear();
            //}
            RawUsers = (from principal in Searcher.FindAll() select principal as UserPrincipal).ToList();
            Users = RawUsers.OrderBy(Users => Users.SamAccountName).ToList();
        }

        /* Extensions */
        private static string GetProperty(this Principal principal, string property)
        {
            DirectoryEntry directoryEntry = principal.GetUnderlyingObject() as DirectoryEntry;
            if (directoryEntry.Properties.Contains(property))
                return directoryEntry.Properties[property].Value.ToString();
            else
                return string.Empty;
        }
        public static string GetTitle(this Principal principal)
        {
            return principal.GetProperty("title");
        }
        public static string GetDepartment(this Principal principal)
        {
            return principal.GetProperty("department");
        }
        public static string GetCompany(this Principal principal)
        {
            return principal.GetProperty("company");
        }
        public static string GetCreatedDate(this Principal principal)
        {
            return principal.GetProperty("whenCreated");
        }

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

        public static bool SaveUser(this UserPrincipal User)
        {
            //try
            //{
            //DirectoryEntry usr;
            //if (!ReferenceEquals(DomainName, null) && !ReferenceEquals(AuthenticatingUsername, null) && !ReferenceEquals(AuthenticatingPassword, null))
            //{
            //    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, AuthenticatingUsername, AuthenticatingPassword);
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
        public static bool ExtendUser(this UserPrincipal User, int Days)
        {
            try
            {
                //DirectoryEntry user = User.GetUnderlyingObject() as DirectoryEntry;
                DirectoryEntry usr;
                if (!ReferenceEquals(DomainName, null) && !ReferenceEquals(AuthenticatingUsername, null) && !ReferenceEquals(AuthenticatingPassword, null))
                {
                    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, AuthenticatingUsername, AuthenticatingPassword);
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
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.Runtime.InteropServices.COMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        public static bool EnableUser(this UserPrincipal User)
        {
            try
            {
                DirectoryEntry usr;
                if (!ReferenceEquals(DomainName, null) && !ReferenceEquals(AuthenticatingUsername, null) && !ReferenceEquals(AuthenticatingPassword, null))
                {
                    usr = new DirectoryEntry("LDAP://" + Domain.ConnectedServer + "/" + User.DistinguishedName, AuthenticatingUsername, AuthenticatingPassword);
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
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.Runtime.InteropServices.COMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        public static bool DisableUser(this UserPrincipal User)
        {
            try
            {
                DirectoryEntry usr;
                if (!ReferenceEquals(DomainName, null) && !ReferenceEquals(AuthenticatingUsername, null) && !ReferenceEquals(AuthenticatingPassword, null))
                {
                    usr = new DirectoryEntry("LDAP://" + Domain.Name + "/" + User.DistinguishedName, AuthenticatingUsername, AuthenticatingPassword);
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
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.Runtime.InteropServices.COMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        
        /* Background Worker - ConnectAD */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker ConnectAD = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void ConnectAD_Initialize()
        {
            ConnectAD.WorkerReportsProgress = true;
            ConnectAD.WorkerSupportsCancellation = true;
            ConnectAD.DoWork += ConnectAD_DoWork;
        }
        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void ConnectAD_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
            IsConnected = InitializeDomain();
            if (IsConnected) RefreshUsers();
        }

        /* Background Worker - GetUserProperties */
        /// <summary>
        /// Create background worker instance
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

            SelectedUser_Title = string.Empty;
            SelectedUser_Department = string.Empty;
            SelectedUser_Company = string.Empty;
            SelectedUser_CreatedDate = string.Empty;
            SelectedUser_IsAccountLockedOut = false;
            SelectedUser_Groups = null;

            SelectedUser_Title = CurrentBackgroundUser.GetTitle();
            GetUserProperties.ReportProgress(15);
            SelectedUser_Department = CurrentBackgroundUser.GetDepartment();
            GetUserProperties.ReportProgress(30);
            SelectedUser_Company = CurrentBackgroundUser.GetCompany();
            GetUserProperties.ReportProgress(45);
            SelectedUser_CreatedDate = CurrentBackgroundUser.GetCreatedDate();
            GetUserProperties.ReportProgress(60);
            SelectedUser_IsAccountLockedOut = CurrentBackgroundUser.IsAccountLockedOut();
            GetUserProperties.ReportProgress(75);
            SelectedUser_Groups = CurrentBackgroundUser.GetGroups();
        }
    }
}
