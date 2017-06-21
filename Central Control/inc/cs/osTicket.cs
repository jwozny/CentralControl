using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security;
using System.Windows;

namespace Central_Control
{
    public static class osTicket
    {
        private static string ConnectionString { get; set; } = null;
        private static MySqlConnection Connection { get; set; } = null;
        public static string ConnectionError { get; set; } = null;

        internal static List<string> Databases = new List<string>();
        internal static string SelectedDatabase { get; set; } = GlobalConfig.Settings.OST_Database;
        internal static string TablePrefix { get; set; } = GlobalConfig.Settings.OST_TablePrefix;

        internal static List<HelpTopic> HelpTopics = new List<HelpTopic>();
        internal static HelpTopic SelectedHelpTopic { get; set; } = new HelpTopic(GlobalConfig.Settings.OST_HelpTopic_ID, GlobalConfig.Settings.OST_HelpTopic);

        internal static List<Form> Forms = new List<Form>();
        internal static Form SelectedForm { get; set; } = new Form(GlobalConfig.Settings.OST_Form_ID, GlobalConfig.Settings.OST_Form);

        internal static List<FormField> FormFields = new List<FormField>();
        internal static FormField NUName = new FormField(GlobalConfig.Settings.OST_NUNameField_ID, GlobalConfig.Settings.OST_NUNameField);
        internal static FormField NUDept = new FormField(GlobalConfig.Settings.OST_NUDeptField_ID, GlobalConfig.Settings.OST_NUDeptField);
        internal static FormField NUTitle = new FormField(GlobalConfig.Settings.OST_NUTitleField_ID, GlobalConfig.Settings.OST_NUTitleField);

        internal static List<string> NUDept_List = new List<string>();

        internal static List<NewUserTicket> Tickets = new List<NewUserTicket>();

        public static bool TestConnection(string Server, int Port, string Username, string Password, string Database)
        {
            if (String.IsNullOrEmpty(Database))
            {
				ConnectionString = "server=" + Server + ";uid=" + Username + ";pwd=" + Password + ";";
            }
            else
			{
				ConnectionString = "server=" + Server + ";uid=" + Username + ";pwd=" + Password + ";database=" + Database + ";";
            }

            try
            {
                Connection = new MySqlConnection(ConnectionString);
				Connection.Open();
				Connection.Close();
				return true;
            }
            catch (MySqlException E)
			{
				ConnectionError = E.Message;
				
				return false;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
				
				return false;
			}
		}

        public static void GetDatabases()
        {
            try
            {
                Databases.Clear();

                Connection.Open();

                // Fetch Databases
                MySqlCommand cmd = Connection.CreateCommand();
                cmd.CommandText = "SHOW DATABASES;";

                MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string Row = "";
                    for (int i = 0; i < Reader.FieldCount; i++)
                        Row += Reader.GetValue(i).ToString();
                    Databases.Add(Row);
                }
                Reader.Close();
                Connection.Close();
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        public static void GetHelpTopics(string Database)
        {
            try
            {
                HelpTopics.Clear();

                Connection.Open();

                // Fetch Help Topic Table
                List<string> Tables = new List<string>();
                MySqlCommand cmd = Connection.CreateCommand();
                cmd.CommandText = "SHOW TABLES FROM `" + Database + "` WHERE `Tables_in_" + Database + "` LIKE '%help_topic';";

                MySqlDataReader TableReader = cmd.ExecuteReader();
                while (TableReader.Read())
                {
                    string Row = "";
                    for (int i = 0; i < TableReader.FieldCount; i++)
                        Row += TableReader.GetValue(i).ToString();
                    Tables.Add(Row);
                }
                TableReader.Close();

                if (Tables.Count > 0)
                {
                    TablePrefix = Tables[0].Replace("help_topic", "");
                    GlobalConfig.Settings.OST_TablePrefix = TablePrefix;
                }

                cmd.CommandText = "USE `" + Database + "`; " +
                    "SELECT A.topic_id as 'ID', B.topic, A.topic " +
                        "FROM `" + TablePrefix + "help_topic` A " +
                        "LEFT JOIN `" + TablePrefix + "help_topic` B ON A.topic_pid = B.topic_id " +
                        "ORDER BY B.topic";

                MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string Name = "";
                    for (int i = 1; i < Reader.FieldCount; i++)
                        if (!String.IsNullOrEmpty(Reader.GetValue(i).ToString()))
                            Name += Reader.GetValue(i).ToString() + "/";

                    HelpTopics.Add(new HelpTopic(Reader.GetValue(0).ToString(), Name));
                }
                Reader.Close();
                Connection.Close();

                HelpTopics.Sort((x, y) => x.Name.CompareTo(y.Name));
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        public static void GetForms(string Database, string HelpTopic_ID)
        {
            try
            {
                Forms.Clear();

                Connection.Open();

                // Fetch Form Table
                MySqlCommand cmd = Connection.CreateCommand();

                cmd.CommandText = "USE `" + Database + "`; " +
                    "SELECT B.id, title " +
                        "FROM `" + TablePrefix + "help_topic_form` A " +
                        "LEFT JOIN `" + TablePrefix + "form` B ON A.form_id=B.id " +
                        "WHERE topic_id=" + HelpTopic_ID + ";";

                MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string Name = "";
                    for (int i = 1; i < Reader.FieldCount; i++)
                        if (!String.IsNullOrEmpty(Reader.GetValue(i).ToString()))
                            Name += Reader.GetValue(i).ToString();

                    Forms.Add(new Form(Reader.GetValue(0).ToString(), Name));
                }
                Reader.Close();

                Forms.Sort((x, y) => x.Name.CompareTo(y.Name));

                Connection.Close();
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        public static void GetFormFields(string Database, string Form_ID)
        {
            try
            {
                FormFields.Clear();

                Connection.Open();

                // Fetch Form Table
                MySqlCommand cmd = Connection.CreateCommand();

                cmd.CommandText = "USE `" + Database + "`; " +
                    "SELECT id, label, configuration " +
                        "FROM `" + TablePrefix + "form_field` " + 
                        "WHERE form_id=" + Form_ID + ";";

                MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string Name = "";
                    if (!String.IsNullOrEmpty(Reader.GetValue(1).ToString()))
                        Name = Reader.GetValue(1).ToString();

                    FormFields.Add(new FormField(Reader.GetValue(0).ToString(), Name));

                    string DeptList = "";
                    if (!String.IsNullOrEmpty(Reader.GetValue(2).ToString()))
                        DeptList = Reader.GetValue(2).ToString();
                }
                Reader.Close();

                FormFields.Sort((x, y) => x.Name.CompareTo(y.Name));

                Connection.Close();
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        public static void GetTickets()
        {
            if (String.IsNullOrEmpty(SelectedDatabase))
            {
                ConnectionString = "server=" + GlobalConfig.Settings.OST_Server + ";uid=" + GlobalConfig.Settings.OST_Username + ";pwd=" + GlobalConfig.Settings.OST_Password + ";";
            }
            else
            {
                ConnectionString = "server=" + GlobalConfig.Settings.OST_Server + ";uid=" + GlobalConfig.Settings.OST_Username + ";pwd=" + GlobalConfig.Settings.OST_Password + ";database=" + SelectedDatabase + ";";
            }

            try
            {
                Connection = new MySqlConnection(ConnectionString);
                Connection.Open();

                Tickets.Clear();

                // Fetch Form Table
                MySqlCommand cmd = Connection.CreateCommand();
                
                cmd.CommandText = "USE `" + SelectedDatabase + "`; " +
                    "SELECT ticket_id 'Ticket ID', " +
                        "number 'Ticket Number', " +
                        "stat.name 'Ticket Status', " +
                        "value 'New User', " +
                        "user.name 'Created By', " + 
                        "CONCAT(staff.firstname, ' ', staff.lastname) 'Assigned To' " +
                        "FROM `" + TablePrefix + "ticket` tic " +
                        "LEFT JOIN `" + TablePrefix + "ticket_status` stat ON tic.status_id = id " +
                        "LEFT JOIN `" + TablePrefix + "form_entry` ent ON ent.object_id = tic.ticket_id " +
                        "LEFT JOIN `" + TablePrefix + "form_entry_values` val ON val.entry_id = ent.id " +
                        "LEFT JOIN `" + TablePrefix + "user` user ON user.id = tic.user_id " +
                        "LEFT JOIN `" + TablePrefix + "staff` staff ON staff.staff_id = tic.staff_id " +
                        "WHERE topic_id = " + SelectedHelpTopic.ID + " " +
                            "AND( " +
                                "closed IS NULL " +
                                "OR NOT status_id = 3 " +
                            ") "+
                            "AND field_id = " + NUName.ID + "; ";

                using (MySqlDataReader Reader = cmd.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        string TicketID = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(0).ToString()))
                            TicketID = Reader.GetValue(0).ToString();

                        string TicketNumber = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(1).ToString()))
                            TicketNumber = Reader.GetValue(1).ToString();

                        string TicketStatus = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(2).ToString()))
                            TicketStatus = Reader.GetValue(2).ToString();

                        string NewUserName = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(3).ToString()))
                            NewUserName = Reader.GetValue(3).ToString();

                        string CreatedBy = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(4).ToString()))
                            CreatedBy = Reader.GetValue(4).ToString();

                        string AssignedTo = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(5).ToString()))
                            AssignedTo = Reader.GetValue(5).ToString();

                        Tickets.Add(new NewUserTicket(TicketID, TicketNumber, TicketStatus, NewUserName, CreatedBy, AssignedTo));
                    }
                    Reader.Close();
                }
                
                cmd.CommandText = "USE `" + SelectedDatabase + "`; " +
                    "SELECT ticket_id 'Ticket ID', " +
                        "value 'New User' " +
                        "FROM `" + TablePrefix + "ticket` tic " +
                        "LEFT JOIN `" + TablePrefix + "form_entry` ent ON ent.object_id = tic.ticket_id " +
                        "LEFT JOIN `" + TablePrefix + "form_entry_values` val ON val.entry_id = ent.id " +
                        "WHERE topic_id = " + SelectedHelpTopic.ID + " " +
                            "AND( " +
                                "closed IS NULL " +
                                "OR NOT status_id = 3 " +
                            ") " +
                            "AND field_id = " + NUTitle.ID + "; ";

                using (MySqlDataReader Reader = cmd.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        string TicketID = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(0).ToString()))
                            TicketID = Reader.GetValue(0).ToString();

                        string Title = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(1).ToString()))
                            Title = Reader.GetValue(1).ToString();

                        Tickets.Find(p => p.TicketID == TicketID).Title = Title;
                    }
                    Reader.Close();
                }

                cmd.CommandText = "USE `" + SelectedDatabase + "`; " +
                    "SELECT ticket_id 'Ticket ID', " +
                        "value 'New User' " +
                        "FROM `" + TablePrefix + "ticket` tic " +
                        "LEFT JOIN `" + TablePrefix + "form_entry` ent ON ent.object_id = tic.ticket_id " +
                        "LEFT JOIN `" + TablePrefix + "form_entry_values` val ON val.entry_id = ent.id " +
                        "WHERE topic_id = " + SelectedHelpTopic.ID + " " +
                            "AND( " +
                                "closed IS NULL " +
                                "OR NOT status_id = 3 " +
                            ") " +
                            "AND field_id = " + NUDept.ID + "; ";

                using (MySqlDataReader Reader = cmd.ExecuteReader())
                {
                    while (Reader.Read())
                    {
                        string TicketID = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(0).ToString()))
                            TicketID = Reader.GetValue(0).ToString();

                        string Department = "";
                        if (!String.IsNullOrEmpty(Reader.GetValue(1).ToString()))
                            Department = Reader.GetValue(1).ToString();

                        Department = Department.Trim('{', '}').Split(':')[0].Replace("\"", "");

                        Tickets.Find(p => p.TicketID == TicketID).Department = Department;
                    }
                    Reader.Close();
                }

                Connection.Close();
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
			}
		}

        public static List<string> GetFieldChoices(string Database, string Field_ID)
        {
            try
            {
                List<string> Choices = new List<string>();

                Connection.Open();

                // Fetch Form Table
                MySqlCommand cmd = Connection.CreateCommand();

                cmd.CommandText = "USE `" + Database + "`; " +
                    "SELECT configuration " +
                        "FROM `" + TablePrefix + "form_field` " +
                        "WHERE id=" + Field_ID + ";";

                MySqlDataReader Reader = cmd.ExecuteReader();
                while (Reader.Read())
                {
                    string ChoiceString = "";
                    List<string> ChoiceProperties = new List<string>();
                    if (!String.IsNullOrEmpty(Reader.GetValue(0).ToString()))
                    {
                        ChoiceString = Reader.GetValue(0).ToString();
                        if (ChoiceString.StartsWith("{\"choices\":"))
                        {
                            ChoiceString = ChoiceString.Replace("{", "").Replace("}", "");
                            ChoiceProperties = ChoiceString.Split(',').ToList();
                            Choices = ChoiceProperties[0].Replace("\"choices\":\"", "").Replace("\"", "").Replace("\\r\\n", ";").Split(';').ToList();
                        }
                    }

                }
                Reader.Close();

                Connection.Close();

                Choices.Sort();
                return Choices;
            }
            catch (MySqlException E)
            {
                Connection.Close();
                ConnectionError = E.Message;
                return null;
			}
			catch (Exception E)
			{
				MessageBox.Show(E.Message, "ERROR");
				return null;
			}
		}

        public class HelpTopic
        {
            public string ID { get; set; }
            public string Name { get; set; } = null;

            public HelpTopic(string id, string name)
            {
                ID = id;
                Name = name;
            }

            [SecurityCritical]
            public override string ToString()
            {
                return Name;
            }
        }

        public class Form
        {
            public string ID { get; set; }
            public string Name { get; set; } = null;

            public Form(string id, string name)
            {
                ID = id;
                Name = name;
            }

            [SecurityCritical]
            public override string ToString()
            {
                return Name;
            }
        }

        public class FormField
        {
            public string ID { get; set; }
            public string Name { get; set; } = null;

            public FormField(string id, string name)
            {
                ID = id;
                Name = name;
            }

            [SecurityCritical]
            public override string ToString()
            {
                return Name;
            }
        }

        public class NewUserTicket
        {
            public string TicketID { get; set; }
            public string TicketNumber { get; set; }
            public string TicketStatus { get; set; }
            public string NewUserName { get; set; } = null;
            public string Title { get; set; } = null;
            public string Department { get; set; } = null;
            public string CreatedBy { get; set; }
            public string AssignedTo { get; set; }

            public NewUserTicket(string ticketID, string ticketNumber, string ticketStatus, string newUserName, string createdBy, string assignedTo)
            {
                TicketID = ticketID;
                TicketNumber = ticketNumber;
                TicketStatus = ticketStatus;
                NewUserName = newUserName;
                CreatedBy = createdBy;
                AssignedTo = assignedTo;
            }

            [SecurityCritical]
            public override string ToString()
            {
                return TicketNumber;
            }
        }
    }
}
