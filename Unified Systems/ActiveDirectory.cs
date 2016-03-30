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
            if (stringItem == String.Empty) return false;
            return true;
        }

        public static bool InitializeDomain()
        {
            try
            {
                if (!ReferenceEquals(DomainName, null) && !ReferenceEquals(AuthenticatingUsername, null) && !ReferenceEquals(AuthenticatingPassword, null))
                {
                    Domain = new PrincipalContext(ContextType.Domain, DomainName, AuthenticatingUsername, AuthenticatingPassword);
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
            catch (System.DirectoryServices.AccountManagement.PrincipalServerDownException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.DirectoryServices.DirectoryServicesCOMException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            catch (System.DirectoryServices.Protocols.DirectoryOperationException E)
            {
                ConnectionError = E.Message.ToString();
                return false;
            }
            return true;
        }
        public static bool Connect()
        {
            IsConnected = InitializeDomain();
            if (IsConnected) RefreshUsers();
            return IsConnected;
        }
        public static bool IsConnected;
        public static string ConnectionError;

        public static PrincipalContext Domain;
        public static PrincipalSearcher Searcher;
        public static IEnumerable<UserPrincipal> RawUsers { get; set; } 
        public static List<UserPrincipal> Users { get; set; }

        public static UserPrincipal SelectedUser;

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
        
        /* Background Worker - GetAllUsers */
        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker GetAllUsers = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public static void GetAllUsers_Initialize()
        {
            GetAllUsers.WorkerReportsProgress = true;
            GetAllUsers.WorkerSupportsCancellation = true;
            GetAllUsers.DoWork += GetAllUsers_DoWork;
            GetAllUsers.ProgressChanged += GetAllUsers_ProgressChanged;
            GetAllUsers.RunWorkerCompleted += GetAllUsers_Completed;
        }
        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void GetAllUsers_DoWork(object sender, DoWorkEventArgs e)
        {
            // Work Started - Do this.
        }
        private static void GetAllUsers_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            // Do this when progress is reported.
        }
        private static void GetAllUsers_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            // Work Completed - Do this.
        }
    }
}
