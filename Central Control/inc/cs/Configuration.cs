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
        public string AD_Domain { get; set; }
        public bool AD_UseLocalDomain { get; set; }
        public string AD_Username { get; set; }
        public string AD_Password { get; set; }
        public bool AD_UseLocalAuth { get; set; }

        public Configuration()
        {
            AD_Domain = null;
            AD_UseLocalDomain = true;
            AD_Username = null;
            AD_Password = null;
            AD_UseLocalAuth = true;
        }
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
