using System;
using System.Configuration;
using System.Text;
using System.Threading;
using CryptoLibrary;

namespace CryptoConsole
{
    class Program
    {
        // Static field to keep the Mutex for the entire application lifetime
        private static Mutex? _singleInstanceMutex;

        static int Main(string[] args)
        {
            const string mutexName = @"Global\CryptoConsole_MonoInstance";
            bool createdNew;

            try
            {
                // 1) Try to create and acquire the named mutex
                _singleInstanceMutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out createdNew);

                if (!createdNew)
                {
                    // If createdNew == false, it means another instance is already running
                    Console.Error.WriteLine("CryptoSoft is already running on this machine.");
                    return -1;
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Case where the user doesn't have permission to create a Global mutex
                Console.Error.WriteLine("Unable to verify single instance: " + uaEx.Message);
                return -1;
            }
            // (We don't call ReleaseMutex() here: the Mutex will remain acquired until the process ends)

            // 2) Validate arguments
            if (args.Length != 3 || (args[0] != "encrypt" && args[0] != "decrypt"))
            {
                Console.WriteLine("Usage: CryptoConsole encrypt|decrypt <source> <destination>");
                return -1;
            }

            // 3) Load key from App.config
            var keyString = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 8)
            {
                Console.Error.WriteLine("EncryptionKey is missing or too short (>8 chars).");
                return -2;
            }
            var key = Encoding.UTF8.GetBytes(keyString);

            // 4) Execute encryption or decryption
            bool enc = args[0] == "encrypt";
            int ms = enc
                ? CryptoManager.EncryptFile(args[1], args[2], key)
                : CryptoManager.DecryptFile(args[1], args[2], key);

            if (ms < 0)
            {
                Console.Error.WriteLine("Error during processing.");
                return -3;
            }

            Console.WriteLine($"{(enc ? "Encrypted" : "Decrypted")} in {ms} ms: {args[2]}");
            return 0;
        }
    }
}
