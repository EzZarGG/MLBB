using System;
using System.Collections.Generic;

namespace EasySaveV3._0.Patterns.Observer
{
    public class BackupSubject
    {
        private readonly List<IBackupObserver> _observers;
        private readonly object _observersLock = new object();

        public BackupSubject()
        {
            _observers = new List<IBackupObserver>();
        }

        public void Attach(IBackupObserver observer)
        {
            lock (_observersLock)
            {
                if (!_observers.Contains(observer))
                {
                    _observers.Add(observer);
                }
            }
        }

        public void Detach(IBackupObserver observer)
        {
            lock (_observersLock)
            {
                _observers.Remove(observer);
            }
        }

        public void NotifyStateChanged(string backupName, string state, int progress)
        {
            lock (_observersLock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnBackupStateChanged(backupName, state, progress);
                    }
                    catch (Exception ex)
                    {
                        // Log l'erreur de notification
                        Console.WriteLine($"Erreur lors de la notification de changement d'état: {ex.Message}");
                    }
                }
            }
        }

        public void NotifyCompleted(string backupName, bool success)
        {
            lock (_observersLock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnBackupCompleted(backupName, success);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de la notification de complétion: {ex.Message}");
                    }
                }
            }
        }

        public void NotifyError(string backupName, string errorMessage)
        {
            lock (_observersLock)
            {
                foreach (var observer in _observers)
                {
                    try
                    {
                        observer.OnBackupError(backupName, errorMessage);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erreur lors de la notification d'erreur: {ex.Message}");
                    }
                }
            }
        }
    }
} 