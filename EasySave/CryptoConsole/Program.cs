using System;
using CryptoLibrary;
using System.Configuration;
using System.Text;

namespace CryptoConsole
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length != 3 || (args[0] != "encrypt" && args[0] != "decrypt"))
            {
                Console.WriteLine("Usage: CryptoConsole encrypt|decrypt <source> <destination>");
                return -1;
            }

            // 1) Chargement de la clé depuis App.config
            var keyString = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 8)
            {
                Console.Error.WriteLine("Clé EncryptionKey absente ou trop courte (>8 chars).");
                return -2;
            }
            var key = Encoding.UTF8.GetBytes(keyString);

            // 2) Appel du chiffrement/déchiffrement
            bool enc = args[0] == "encrypt";
            int ms = enc
                ? CryptoManager.EncryptFile(args[1], args[2], key)
                : CryptoManager.DecryptFile(args[1], args[2], key);

            if (ms < 0)
            {
                Console.Error.WriteLine("Erreur pendant le traitement.");
                return -3;
            }

            Console.WriteLine($"{(enc ? "Chiffré" : "Déchiffré")} en {ms} ms : {args[2]}");
            return 0;
        }
    }
}
