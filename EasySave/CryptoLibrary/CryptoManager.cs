using System;
using System.Diagnostics;
using System.IO;

namespace CryptoLibrary
{
    public static class CryptoManager
    {
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

        public static int DecryptFile(string sourcePath, string destPath, byte[] key)
            => EncryptFile(sourcePath, destPath, key);
    }
}
