using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
//using System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Net.NetworkInformation;
using System.ComponentModel;
using System.Net;
using System.Reflection;

namespace Central_Control.Settings
{
    /// <summary>
    /// Interaction logic for ActiveDirectory.xaml
    /// </summary>
    public partial class osTicket : Page
    {
        public osTicket()
        {
            InitializeComponent();
            TestServ.RunWorkerCompleted += TestServ_Completed;
            Central_Control.ActiveDirectory.OU_Fetcher.RunWorkerCompleted += OUFetcher_Completed;

            OST_HelpTopicComboBox.SelectedValuePath = "Name";
            OST_FormComboBox.SelectedValuePath = "Name";
            OST_NUNameComboBox.SelectedValuePath = "Name";
            OST_NUDeptComboBox.SelectedValuePath = "Name";
            OST_NUTitleComboBox.SelectedValuePath = "Name";

            OST_IntegrationCheckbox.IsChecked = GlobalConfig.Settings.OST_Integration;
            Form1.IsEnabled = GlobalConfig.Settings.OST_Integration;
        }

        #region Page Events
        /// <summary>
        /// Event handler when the page finishes loading
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void osTicket_Loaded(object sender, RoutedEventArgs e)
        {
			LoadSettings();
			Central_Control.ActiveDirectory.Refresh("OUs");
			if (GlobalConfig.Settings.OST_Integration == true) TestServer();
        }
        #endregion Page Events

        #region Common Functions

        private void TestServer()
		{
			try
			{
				Server = OST_ServerTextBox.Text;
				if (String.IsNullOrEmpty(OST_ServerPortTextBox.Text)) Port = 3306;
				else Port = Int32.Parse(OST_ServerPortTextBox.Text);

				Username = OST_UsernameTextBox.Text;
				Password = OST_PasswordBox.Password;

				Connection = false;
				Ping = null;
				Error = null;

				if (String.IsNullOrEmpty(Server))
				{
					TestServResult.Text = "Database server not specified.";
					TestServResult.Style = FindResource("Message_Error") as Style;
				}
				else if (String.IsNullOrEmpty(Username) || String.IsNullOrEmpty(Password))
				{
					TestServResult.Text = "Username and password required.";
					TestServResult.Style = FindResource("Message_Error") as Style;
				}
				//else if (String.IsNullOrEmpty(Database))
				//{
				//    TestServResult.Text = "Database is not specified.";
				//    TestServResult.Style = FindResource("Message_Error") as Style;
				//}
				else
				{
					TestServButton.IsEnabled = false;
					TestServResult.Text = null;
					TestServ_Initialize();
				}
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}

		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="combobox"></param>
        private void ClearComboBox(System.Windows.Controls.ComboBox combobox)
        {
            combobox.ItemsSource = null;
            combobox.Items.Clear();
            combobox.Text = null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="DeptVal"></param>
        /// <param name="OUVal"></param>
        private void AddDeptOUMap(string DeptVal = null, string OUVal = null)
        {
            DockPanel Dept_OU = new DockPanel();
            Dept_OU.Name = "Dept_OU_" + Dept_OU_Count;

            ComboBox Dept = new ComboBox();
            Dept.Name = "Dept_" + Dept_OU_Count;
            Dept.IsEditable = true;
            Dept.ItemsSource = Central_Control.osTicket.NUDept_List;
            if(DeptVal != null) Dept.Text = DeptVal;

            ComboBox OU = new ComboBox();
            OU.Name = "OU_" + Dept_OU_Count;
            OU.IsEditable = true;
            OU.HorizontalAlignment = HorizontalAlignment.Right;
            OU.ItemsSource = Central_Control.ActiveDirectory.OUs;
            OU.Margin = new Thickness(3, 2, 2, 2);
            if(OUVal != null) OU.Text = OUVal;

            Button Remove = new Button();
            Remove.Name = "Remove_" + Dept_OU_Count;
            Remove.FontSize = 8;
            Remove.Style = FindResource("TinyButton") as Style;
            Remove.HorizontalAlignment = HorizontalAlignment.Right;
            Remove.Content = "-";
            Remove.Click += RemoveDeptOUMapping_Click;

            System.Windows.Shapes.Path MinusSign_Path = new System.Windows.Shapes.Path();
            MinusSign_Path.StrokeThickness = 1;
            MinusSign_Path.Height = 8;
            MinusSign_Path.Width = 8;
            MinusSign_Path.Style = FindResource("ButtonPath") as Style;

            LineGeometry MinusSign_Data = new LineGeometry();
            MinusSign_Data.StartPoint = new Point(0, 4);
            MinusSign_Data.EndPoint = new Point(8, 4);

            Dept_OU_List.Children.Add(Dept_OU);
            Dept_OU.Children.Add(Dept);
            Dept_OU.Children.Add(OU);
            MinusSign_Path.Data = MinusSign_Data;
            Remove.Content = MinusSign_Path;
            Dept_OU.Children.Add(Remove);

            Dept_OU_Count++;
        }
        private int Dept_OU_Count = 0;

        #endregion Common Functions

        #region Settings Functions

        /// <summary>
        /// Get credentials from configuration and display in the page (passwords don't display)
        /// </summary>
        private void LoadSettings()
        {
            if (GlobalConfig.Settings.OST_Integration == true) OST_IntegrationCheckbox.IsChecked = true;
            else OST_IntegrationCheckbox.IsChecked = false;
            Form1.IsEnabled = GlobalConfig.Settings.OST_Integration;
            OST_ServerTextBox.Text = GlobalConfig.Settings.OST_Server;
            OST_ServerPortTextBox.Text = GlobalConfig.Settings.OST_ServerPort;
            OST_UsernameTextBox.Text = GlobalConfig.Settings.OST_Username;
            OST_PasswordBox.Password = GlobalConfig.Settings.OST_Password;
            OST_DatabaseComboBox.Text = GlobalConfig.Settings.OST_Database;
            OST_HelpTopicComboBox.Text = GlobalConfig.Settings.OST_HelpTopic;
            OST_FormComboBox.Text = GlobalConfig.Settings.OST_Form;
            OST_NUNameComboBox.Text = GlobalConfig.Settings.OST_NUNameField;
            OST_NUDeptComboBox.Text = GlobalConfig.Settings.OST_NUDeptField;
            OST_NUTitleComboBox.Text = GlobalConfig.Settings.OST_NUTitleField;

            Dept_OU_List.Children.Clear();
            Dept_OU_Count = 0;
			if (GlobalConfig.Settings.OST_OU != null && GlobalConfig.Settings.OST_Dept != null)
			{
				for (int i = 0; i < GlobalConfig.Settings.OST_OU.Count; i++)
				{
					AddDeptOUMap(GlobalConfig.Settings.OST_Dept[i], GlobalConfig.Settings.OST_OU[i]);
				}
			}
        }
        /// <summary>
        /// Save settings into configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private bool SaveSettings()
        {
            try
            {
                if (OST_IntegrationCheckbox.IsChecked == true) GlobalConfig.Settings.OST_Integration = true;
                else GlobalConfig.Settings.OST_Integration = false;
                GlobalConfig.Settings.OST_Server = OST_ServerTextBox.Text;
                GlobalConfig.Settings.OST_ServerPort = OST_ServerPortTextBox.Text;
                GlobalConfig.Settings.OST_Username = OST_UsernameTextBox.Text;
                GlobalConfig.Settings.OST_Password = OST_PasswordBox.Password;
                if (OST_DatabaseComboBox.SelectedIndex != -1)
                {
                    GlobalConfig.Settings.OST_Database = OST_DatabaseComboBox.SelectedItem.ToString();
                }
                if (OST_HelpTopicComboBox.SelectedIndex != -1)
                {
                    Central_Control.osTicket.HelpTopic helptopic = OST_HelpTopicComboBox.Items[OST_HelpTopicComboBox.SelectedIndex] as Central_Control.osTicket.HelpTopic;
                    GlobalConfig.Settings.OST_HelpTopic_ID = helptopic.ID;
                    GlobalConfig.Settings.OST_HelpTopic = helptopic.Name;
                }
                if (OST_FormComboBox.SelectedIndex != -1)
                {
                    Central_Control.osTicket.Form form = OST_FormComboBox.Items[OST_FormComboBox.SelectedIndex] as Central_Control.osTicket.Form;
                    GlobalConfig.Settings.OST_Form_ID = form.ID;
                    GlobalConfig.Settings.OST_Form = form.Name;
                }
                
                if (OST_NUNameComboBox.SelectedIndex != -1)
                {
                    Central_Control.osTicket.FormField formfield = OST_NUNameComboBox.Items[OST_NUNameComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
                    GlobalConfig.Settings.OST_NUNameField_ID = formfield.ID;
                    GlobalConfig.Settings.OST_NUNameField = formfield.Name;
                }
                if (OST_NUDeptComboBox.SelectedIndex != -1)
                {
                    Central_Control.osTicket.FormField formfield = OST_NUDeptComboBox.Items[OST_NUDeptComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
                    GlobalConfig.Settings.OST_NUDeptField_ID = formfield.ID;
                    GlobalConfig.Settings.OST_NUDeptField = formfield.Name;
                }
                if (OST_NUTitleComboBox.SelectedIndex != -1)
                {
                    Central_Control.osTicket.FormField formfield = OST_NUTitleComboBox.Items[OST_NUTitleComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
                    GlobalConfig.Settings.OST_NUTitleField_ID = formfield.ID;
                    GlobalConfig.Settings.OST_NUTitleField = formfield.Name;
                }
				
				GlobalConfig.Settings.OST_Dept.Clear();
				GlobalConfig.Settings.OST_OU.Clear();
                for ( int i=0; i < Dept_OU_List.Children.Count; i++)
                {
                    DockPanel Panel = Dept_OU_List.Children[i] as DockPanel;
					ComboBox Dept = Panel.Children[0] as ComboBox;
					ComboBox OU = Panel.Children[1] as ComboBox;

					if (!String.IsNullOrEmpty(Dept.SelectedValue.ToString()) && !String.IsNullOrEmpty(OU.SelectedValue.ToString()))
					{
						GlobalConfig.Settings.OST_Dept.Add(Dept.SelectedValue.ToString());
						GlobalConfig.Settings.OST_OU.Add(OU.SelectedValue.ToString());
					}
                }
            }
            catch (Exception E)
            {
				// Caught unexpected error , reload global config from disk and return false 
				MessageBox.Show(E.Message, "ERROR");
				GlobalConfig.LoadFromDisk();
                return false;
            }

            // Save global config
            GlobalConfig.SaveToDisk();
            return true;
        }

        #endregion Settings Functions

        #region Control Actions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_IntegrationCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            Form1.IsEnabled = false;
            Form2.IsEnabled = false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_IntegrationCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            Form1.IsEnabled = true;
            Form2.IsEnabled = true;
			if (!String.IsNullOrEmpty(OST_ServerTextBox.Text) && !String.IsNullOrEmpty(OST_UsernameTextBox.Text) && !String.IsNullOrEmpty(OST_PasswordBox.Password))
			{
				TestServer();
			}
		}
        /// <summary>
        /// Select all text in the AD Domain box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_ServerTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OST_ServerTextBox.SelectAll();
        }
        /// <summary>
        /// Select all text in the AD Domain box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_ServerPortTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OST_ServerPortTextBox.SelectAll();
        }
        /// <summary>
        /// Select all text in the AD Username box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_UsernameTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OST_UsernameTextBox.SelectAll();
        }
        /// <summary>
        /// Select all text in the AD password box when getting focus
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_PasswordBox_GotFocus(object sender, RoutedEventArgs e)
        {
            OST_PasswordBox.SelectAll();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestServButton_Click(object sender, RoutedEventArgs e)
        {
            TestServer();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_DatabaseComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OST_DatabaseComboBox.SelectedIndex != -1)
            {
                Central_Control.osTicket.SelectedDatabase = OST_DatabaseComboBox.SelectedItem.ToString();
                Central_Control.osTicket.GetHelpTopics(Central_Control.osTicket.SelectedDatabase);
                HelpTopic_Selector.IsEnabled = true;
            }
            else
            {
                HelpTopic_Selector.IsEnabled = false;
                Form_Selector.IsEnabled = false;
                Field_Name_Selector.IsEnabled = false;
                Field_Dept_Selector.IsEnabled = false;
                Field_Title_Selector.IsEnabled = false;
            }
            ClearComboBox(OST_HelpTopicComboBox);
            OST_HelpTopicComboBox.ItemsSource = Central_Control.osTicket.HelpTopics;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_HelpTopicComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OST_HelpTopicComboBox.SelectedIndex != -1)
            {
                Central_Control.osTicket.SelectedHelpTopic = OST_HelpTopicComboBox.Items[OST_HelpTopicComboBox.SelectedIndex] as Central_Control.osTicket.HelpTopic;
                Central_Control.osTicket.GetForms(Central_Control.osTicket.SelectedDatabase, Central_Control.osTicket.SelectedHelpTopic.ID);
                Form_Selector.IsEnabled = true;
            }
            else
            {
                Form_Selector.IsEnabled = false;
                Field_Name_Selector.IsEnabled = false;
                Field_Dept_Selector.IsEnabled = false;
                Field_Title_Selector.IsEnabled = false;
            }
            ClearComboBox(OST_FormComboBox);
            OST_FormComboBox.ItemsSource = Central_Control.osTicket.Forms;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OST_FormComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (OST_FormComboBox.SelectedIndex != -1)
            {
                Central_Control.osTicket.SelectedForm = OST_FormComboBox.Items[OST_FormComboBox.SelectedIndex] as Central_Control.osTicket.Form;
                Central_Control.osTicket.GetFormFields(Central_Control.osTicket.SelectedDatabase, Central_Control.osTicket.SelectedForm.ID);
                Field_Name_Selector.IsEnabled = true;
                Field_Dept_Selector.IsEnabled = true;
                Field_Title_Selector.IsEnabled = true;
            }
            else
            {
                Field_Name_Selector.IsEnabled = false;
                Field_Dept_Selector.IsEnabled = false;
                Field_Title_Selector.IsEnabled = false;
            }
            // Name
            ClearComboBox(OST_NUNameComboBox);
            OST_NUNameComboBox.ItemsSource = Central_Control.osTicket.FormFields;

            // Department
            ClearComboBox(OST_NUDeptComboBox);
            OST_NUDeptComboBox.ItemsSource = Central_Control.osTicket.FormFields;

            // Title
            ClearComboBox(OST_NUTitleComboBox);
            OST_NUTitleComboBox.ItemsSource = Central_Control.osTicket.FormFields;
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OST_NUNameComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (OST_NUNameComboBox.SelectedIndex != -1)
			{
				Central_Control.osTicket.NUName = OST_NUNameComboBox.Items[OST_NUNameComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OST_NUDeptComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (OST_NUDeptComboBox.SelectedIndex != -1)
			{
				Central_Control.osTicket.NUDept = OST_NUDeptComboBox.Items[OST_NUDeptComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
				Central_Control.osTicket.NUDept_List = Central_Control.osTicket.GetFieldChoices(Central_Control.osTicket.SelectedDatabase, Central_Control.osTicket.NUDept.ID);
			}
		}
		/// <summary>
		/// 
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void OST_NUTitleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (OST_NUTitleComboBox.SelectedIndex != -1)
			{
				Central_Control.osTicket.NUTitle = OST_NUTitleComboBox.Items[OST_NUTitleComboBox.SelectedIndex] as Central_Control.osTicket.FormField;
			}
		}
		/// <summary>
		/// Add a row of dept-to-ou mapping
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void AddDeptOUMapping_Click(object sender, RoutedEventArgs e)
        {
            AddDeptOUMap();
        }
        /// <summary>
        /// Remove a row of dept-to-ou mapping
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveDeptOUMapping_Click(object sender, RoutedEventArgs e)
        {
            Button thisButton = sender as Button;
            DockPanel Parent = thisButton.Parent as DockPanel;
            StackPanel List = Parent.Parent as StackPanel;

            List.Children.Remove(Parent);
        }
        /// <summary>
        /// Button action to reset settings and repopulate from configuration
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            ResetButton.IsEnabled = false;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            LoadSettings();

            ResetButton.IsEnabled = true;
        }
        /// <summary>
        /// Button action to save settings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveButton.IsEnabled = false;
            ResultBox.Visibility = Visibility.Hidden;

            var scope = FocusManager.GetFocusScope(this);
            FocusManager.SetFocusedElement(scope, null);
            Keyboard.ClearFocus();

            if (SaveSettings())
            {
                ResultMessage.Text = "Saved Successfully";
                ResultBox.Visibility = Visibility.Visible;
            }

            SaveButton.IsEnabled = true;
        }

        #endregion Control Actions

        #region Background Workers

        private static string Server = null;
        private static int Port = 3306;
        private static string Username = null;
        private static string Password = null;
        private static string Database = null;
        private static PingReply Ping = null;
        private static bool Connection = false;
        private static string Error = null;

        /// <summary>
        /// Create background worker instance
        /// </summary>
        public static BackgroundWorker TestServ = new BackgroundWorker();
        /// <summary>
        /// Initialize background worker with actions
        /// </summary>
        public void TestServ_Initialize()
        {
            TestServ.DoWork -= TestServ_DoWork;

            if (!TestServ.IsBusy)
            {
                TestServ.DoWork += TestServ_DoWork;
                TestServ.RunWorkerAsync();
            }
        }
        /// <summary>
        /// Define background worker actions
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestServ_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                IPAddress Address = Dns.GetHostAddresses(Server)[0];
                Ping = new Ping().Send(Address);

                if (Ping.Status == IPStatus.Success)
                {
                    Connection = Central_Control.osTicket.TestConnection(Server, Port, Username, Password, Database);
                    if (!Connection) Error = Central_Control.osTicket.ConnectionError;
                }
                else
                {
                    Error = Ping.Status.ToString();
                }
            }
            catch (System.Net.Sockets.SocketException E)
            {
                Error = E.Message;
            }
            catch (System.FormatException E)
            {
                Error = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}

		}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TestServ_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            TestServButton.IsEnabled = true;

			try
			{
				if (String.IsNullOrEmpty(Error) && Connection)
				{
					TestServResult.Text = "Success";
					TestServResult.Style = FindResource("Message_Success") as Style;

					ClearComboBox(OST_DatabaseComboBox);
					ClearComboBox(OST_HelpTopicComboBox);
					ClearComboBox(OST_FormComboBox);
					ClearComboBox(OST_NUNameComboBox);
					ClearComboBox(OST_NUDeptComboBox);
					ClearComboBox(OST_NUTitleComboBox);

					Central_Control.osTicket.GetDatabases();
					OST_DatabaseComboBox.ItemsSource = Central_Control.osTicket.Databases;

					if (!String.IsNullOrEmpty(Central_Control.osTicket.SelectedDatabase))
					{
						OST_DatabaseComboBox.SelectedValue = Central_Control.osTicket.SelectedDatabase;

						if (OST_DatabaseComboBox.SelectedIndex != -1)
						{
							Central_Control.osTicket.GetHelpTopics(Central_Control.osTicket.SelectedDatabase);
							OST_HelpTopicComboBox.ItemsSource = Central_Control.osTicket.HelpTopics;

							if (!String.IsNullOrEmpty(Central_Control.osTicket.SelectedHelpTopic.Name))
							{
								OST_HelpTopicComboBox.SelectedValue = Central_Control.osTicket.SelectedHelpTopic.Name;

								if (OST_HelpTopicComboBox.SelectedIndex != -1)
								{
									Central_Control.osTicket.GetForms(Central_Control.osTicket.SelectedDatabase, Central_Control.osTicket.SelectedHelpTopic.ID);
									OST_FormComboBox.ItemsSource = Central_Control.osTicket.Forms;

									if (!String.IsNullOrEmpty(Central_Control.osTicket.SelectedForm.Name))
									{
										OST_FormComboBox.SelectedValue = Central_Control.osTicket.SelectedForm.Name;

										if (OST_FormComboBox.SelectedIndex != -1)
										{
											Central_Control.osTicket.GetFormFields(Central_Control.osTicket.SelectedDatabase, Central_Control.osTicket.SelectedForm.ID);
											OST_NUNameComboBox.ItemsSource = Central_Control.osTicket.FormFields;
											OST_NUDeptComboBox.ItemsSource = Central_Control.osTicket.FormFields;
											OST_NUTitleComboBox.ItemsSource = Central_Control.osTicket.FormFields;

											if (!String.IsNullOrEmpty(Central_Control.osTicket.NUName.Name))
											{
												OST_NUNameComboBox.SelectedValue = Central_Control.osTicket.NUName.Name;
											}
											if (!String.IsNullOrEmpty(Central_Control.osTicket.NUDept.Name))
											{
												OST_NUDeptComboBox.SelectedValue = Central_Control.osTicket.NUDept.Name;
											}
											if (!String.IsNullOrEmpty(Central_Control.osTicket.NUTitle.Name))
											{
												OST_NUTitleComboBox.SelectedValue = Central_Control.osTicket.NUTitle.Name;
											}
										}
									}
								}
							}
						}
					}
				}
				else
				{
					TestServResult.Text = Error;
					TestServResult.Style = FindResource("Message_Error") as Style;
				}
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        private void OUFetcher_Completed(object sender, RunWorkerCompletedEventArgs e)
        {
            LoadSettings();
        }

        #endregion Background Workers
    }
}
