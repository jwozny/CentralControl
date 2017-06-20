using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Central_Control
{
    public class Configuration
    {
        /* Active Directory */
        public string AD_Domain { get; set; } = null;
        public bool AD_UseLocalDomain { get; set; } = true;
        public string AD_Username { get; set; } = null;
        public string AD_Password { get; set; } = null;
        public bool AD_UseLocalAuth { get; set; } = true;

        /* osTicket */
        public bool OST_Integration { get; set; } = false;
        public string OST_Server { get; set; } = null;
        public string OST_ServerPort { get; set; } = null;
        public string OST_Username { get; set; } = null;
        public string OST_Password { get; set; } = null;
        public string OST_Database { get; set; } = null;
        public string OST_TablePrefix { get; set; } = null;
        public string OST_HelpTopic_ID { get; set; } = null;
        public string OST_HelpTopic { get; set; } = null;
        public string OST_Form_ID { get; set; } = null;
        public string OST_Form { get; set; } = null;
        public string OST_NUNameField_ID { get; set; } = null;
        public string OST_NUNameField { get; set; } = null;
        public string OST_NUDeptField_ID { get; set; } = null;
        public string OST_NUDeptField { get; set; } = null;
        public string OST_NUTitleField_ID { get; set; } = null;
        public string OST_NUTitleField { get; set; } = null;
        public List<String> OST_Dept { get; set; } = null;
        public List<String> OST_OU { get; set; } = null;
    }

    public static class GlobalConfig
    {
        public static Configuration Settings = new Configuration();

        private static string Key;

        public static void SaveToDisk()
        {
            // Define paths and serializer type
            string decryptedPath = Path.GetTempPath() + @"\~onfig";
            string encryptedPath = Path.GetTempPath() + @"\..\config.eusc";
            XmlSerializer formatter = new XmlSerializer(GlobalConfig.Settings.GetType());

            if (GlobalConfig.Key == null)
            {
                // Generate a new secret key if one does not exist
                GlobalConfig.Key = EncryptDecrypt.GenerateKey();
                File.WriteAllText(Path.GetTempPath() + @"\..\config.eusk", GlobalConfig.Key);
            }

            // Create decrypted file
            FileStream configFile = File.Create(decryptedPath);

            // Write from memory to the decrypted file
            formatter.Serialize(configFile, GlobalConfig.Settings);

            // Close the decrypted file
            configFile.Close();

            // Encrypt the file
            EncryptDecrypt.EncryptFile(decryptedPath,
               encryptedPath,
               GlobalConfig.Key);

            // Delete the decrypted file
            File.Delete(decryptedPath);
        }
        public static void LoadFromDisk()
        {
            // Define paths and serializer type
            string encryptedPath = Path.GetTempPath() + @"\..\config.eusc";
            string decryptedPath = Path.GetTempPath() + @"\~onfig";
            XmlSerializer formatter = new XmlSerializer(GlobalConfig.Settings.GetType());

            try
            {
                // Get the secret key
                GlobalConfig.Key = File.ReadAllText(Path.GetTempPath() + @"\..\config.eusk");
            }
            catch
            {
                // Secret key file doesn't exist, abort load and use null config
                return;
            }

            // Decrypt the file
            EncryptDecrypt.DecryptFile(encryptedPath,
                decryptedPath,
                GlobalConfig.Key);

            // Open the decrypted file
            FileStream configFile = new FileStream(decryptedPath, FileMode.Open);

            // Read from the decrypted file
            byte[] buffer = new byte[configFile.Length];
            configFile.Read(buffer, 0, (int)configFile.Length);

            // Close the decrypted file
            configFile.Close();

            // Delete the decrypted file
            File.Delete(decryptedPath);

            // Move config from buffer to memory (global config variable)
            MemoryStream stream = new MemoryStream(buffer);
            GlobalConfig.Settings = (Configuration)formatter.Deserialize(stream);

        }
    }
}
