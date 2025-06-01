using System;
using System.Diagnostics;
using System.IO;

namespace CryptoLibrary
{
    /// <summary>
    /// Provides file encryption and decryption functionality using XOR encryption.
    /// Uses a simple XOR operation with a key for both encryption and decryption.
    /// </summary>
    public static class CryptoManager
    {
        /// <summary>
        /// Encrypts a file using XOR encryption with the provided key.
        /// </summary>
        /// <param name="sourcePath">Path to the source file to encrypt</param>
        /// <param name="destPath">Path where the encrypted file will be saved</param>
        /// <param name="key">Encryption key (must be at least 8 bytes)</param>
        /// <returns>Time taken for encryption in milliseconds, or -1 if encryption failed</returns>
        public static int EncryptFile(string sourcePath, string destPath, byte[] key)
        {
            const int BufferSize = 4096;
            byte[] buffer = new byte[BufferSize];
            int keyIndex = 0;
            var sw = Stopwatch.StartNew();

            try
            {
                using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
                using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write);
                int bytesRead;
                while ((bytesRead = src.Read(buffer, 0, BufferSize)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        buffer[i] ^= key[keyIndex];
                        keyIndex = (keyIndex + 1) % key.Length;
                    }
                    dst.Write(buffer, 0, bytesRead);
                }
            }
            catch
            {
                return -1;
            }

            sw.Stop();
            return (int)sw.ElapsedMilliseconds;
        }

        /// <summary>
        /// Decrypts a file using XOR encryption with the provided key.
        /// Since XOR encryption is symmetric, decryption uses the same process as encryption.
        /// </summary>
        /// <param name="sourcePath">Path to the encrypted file</param>
        /// <param name="destPath">Path where the decrypted file will be saved</param>
        /// <param name="key">Decryption key (must be the same as the encryption key)</param>
        /// <returns>Time taken for decryption in milliseconds, or -1 if decryption failed</returns>
        public static int DecryptFile(string sourcePath, string destPath, byte[] key)
            => EncryptFile(sourcePath, destPath, key);
    }
}
