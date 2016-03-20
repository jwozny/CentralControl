using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Unified_Systems
{
    public class Configuration
    {
        /* Active Directory */
        public string DomainName { get; set; }
        public string ADUsername { get; set; }
        public string ADPassword { get; set; }
    }
    
    public static class ConfigActions
    {
        private static string SecretKey = "???1)\"\u001f4";
        public static void SaveConfig(Configuration config)
        {
            string configPath = @".\~onfig";
            string encryptedPath = @".\config";

            FileStream configFile = File.Create(configPath);
            XmlSerializer formatter = new XmlSerializer(config.GetType());

            formatter.Serialize(configFile, config);
            configFile.Close();

            // Encrypt the file.
            EncryptDecrypt.EncryptFile(configPath,
               encryptedPath,
               SecretKey);

            File.Delete(configPath);
        }
        public static Configuration LoadConfig()
        {
            string encryptedPath = @".\config";
            string decryptedPath = @".\~onfig";

            // Decrypt the file.
            EncryptDecrypt.DecryptFile(encryptedPath,
               decryptedPath,
               SecretKey);

            Configuration config = new Configuration();
            FileStream configFile = new FileStream(decryptedPath, FileMode.Open);
            XmlSerializer formatter = new XmlSerializer(config.GetType());
            byte[] buffer = new byte[configFile.Length];

            configFile.Read(buffer, 0, (int)configFile.Length);
            configFile.Close();
            File.Delete(decryptedPath);

            MemoryStream stream = new MemoryStream(buffer);
            return (Configuration)formatter.Deserialize(stream);
        }
    }
}
