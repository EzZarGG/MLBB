using System;
using System.Configuration;
using System.Text;
using System.Threading;
using CryptoLibrary;

namespace CryptoConsole
{
    class Program
    {
        // Champ statique pour conserver le Mutex pendant toute la durée de l’application
        private static Mutex? _singleInstanceMutex;

        static int Main(string[] args)
        {
            const string mutexName = @"Global\CryptoConsole_MonoInstance";
            bool createdNew;

            try
            {
                // 1) Tenter de créer et d’acquérir le mutex nommé
                _singleInstanceMutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out createdNew);

                if (!createdNew)
                {
                    // Si createdNew == false, cela signifie qu’une autre instance tourne déjà
                    Console.Error.WriteLine("CryptoSoft est déjà en cours d’exécution sur cette machine.");
                    return -1;
                }
            }
            catch (UnauthorizedAccessException uaEx)
            {
                // Cas où l’utilisateur n’a pas le droit de créer un mutex Global
                Console.Error.WriteLine("Impossible de vérifier l’instance unique : " + uaEx.Message);
                return -1;
            }
            // (Nous n’appelons pas ReleaseMutex() ici : le Mutex restera acquis jusqu’à la fin du process)

            // 2) Validation des arguments
            if (args.Length != 3 || (args[0] != "encrypt" && args[0] != "decrypt"))
            {
                Console.WriteLine("Usage: CryptoConsole encrypt|decrypt <source> <destination>");
                return -1;
            }

            // 3) Chargement de la clé depuis App.config
            var keyString = ConfigurationManager.AppSettings["EncryptionKey"];
            if (string.IsNullOrWhiteSpace(keyString) || keyString.Length < 8)
            {
                Console.Error.WriteLine("Clé EncryptionKey absente ou trop courte (>8 chars).");
                return -2;
            }
            var key = Encoding.UTF8.GetBytes(keyString);

            // 4) Exécution du chiffrement ou déchiffrement
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
