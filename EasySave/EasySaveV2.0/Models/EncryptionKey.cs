using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace EasySaveV2._0.Models
{
    /// <summary>
    /// Manages encryption keys for file encryption operations.
    /// Handles generation, storage, and retrieval of AES encryption keys and initialization vectors.
    /// Keys are stored securely using Windows Data Protection API.
    /// </summary>
    public class EncryptionKey
    {
        /// <summary>
        /// Path to the file where the encryption key is stored.
        /// Located in the application's base directory.
        /// </summary>
        private static readonly string KEY_FILE = Path.Combine(AppContext.BaseDirectory, "encryption.key");

        /// <summary>
        /// Salt used for key protection.
        /// Provides additional security when encrypting the key file.
        /// </summary>
        private static readonly byte[] SALT = new byte[] { 0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c };

        /// <summary>
        /// The AES encryption key used for file encryption.
        /// </summary>
        public byte[] Key { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// The initialization vector (IV) used for AES encryption.
        /// Required for CBC mode encryption.
        /// </summary>
        public byte[] IV { get; private set; } = Array.Empty<byte>();

        /// <summary>
        /// Initializes a new instance of EncryptionKey.
        /// Loads existing key if available, otherwise generates a new one.
        /// </summary>
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

        /// <summary>
        /// Generates a new AES key and initialization vector.
        /// Automatically saves the generated key to disk.
        /// </summary>
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

        /// <summary>
        /// Loads the encryption key and IV from the key file.
        /// If loading fails, generates a new key.
        /// </summary>
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

        /// <summary>
        /// Saves the current encryption key and IV to the key file.
        /// The key data is protected using Windows Data Protection API.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when saving the key fails.</exception>
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