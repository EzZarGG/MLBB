using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasySaveV2._0.Models
{
    public class EncryptionKey
    {
        private static readonly string KEY_FILE = Path.Combine(AppContext.BaseDirectory, "encryption.key");
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        public byte[] Key { get; private set; } = Array.Empty<byte>();
        public byte[] IV { get; private set; } = Array.Empty<byte>();

        public EncryptionKey()
        {
            if (File.Exists(KEY_FILE))
            {
                LoadKey();
            }
            else
            {
                GenerateKey();
            }
        }

        private void GenerateKey()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();
                Key = aes.Key;
                IV = aes.IV;
                SaveKey();
            }
        }

        private void LoadKey()
        {
            try
            {
                var encryptedData = File.ReadAllBytes(KEY_FILE);
                var keyData = ProtectedData.Unprotect(encryptedData, SALT, DataProtectionScope.CurrentUser);
                using (var ms = new MemoryStream(keyData))
                using (var reader = new BinaryReader(ms))
                {
                    var keyLength = reader.ReadInt32();
                    var ivLength = reader.ReadInt32();
                    Key = reader.ReadBytes(keyLength);
                    IV = reader.ReadBytes(ivLength);
                }
            }
            catch
            {
                // If there's any error loading the key, generate a new one
                GenerateKey();
            }
        }

        private void SaveKey()
        {
            try
            {
                using (var ms = new MemoryStream())
                using (var writer = new BinaryWriter(ms))
                {
                    writer.Write(Key.Length);
                    writer.Write(IV.Length);
                    writer.Write(Key);
                    writer.Write(IV);

                    var keyData = ProtectedData.Protect(ms.ToArray(), SALT, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(KEY_FILE, keyData);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to save encryption key", ex);
            }
        }
    }
} 