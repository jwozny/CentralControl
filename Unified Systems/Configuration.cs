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
        private static string SecretKey;

        public static void SaveConfig(Configuration config)
        {
            string configPath = Path.GetTempPath() + @"\~onfig";
            string encryptedPath = Path.GetTempPath() + @"\..\config.eusc";
            string keyPath = Path.GetTempPath() + @"\..\config.eusk";

            if (SecretKey == null)
            {
                SecretKey = EncryptDecrypt.GenerateKey();
                File.WriteAllText(keyPath, SecretKey);
            }

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
            string encryptedPath = Path.GetTempPath() + @"\..\config.eusc";
            string decryptedPath = Path.GetTempPath() + @"\~onfig";
            string keyPath = Path.GetTempPath() + @"\..\config.eusk";

            SecretKey = File.ReadAllText(keyPath);

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
