using System;
using EasySaveV2._0.Patterns.Strategy;

namespace EasySaveV2._0.Patterns.Factory
{
    public class BackupStrategyFactory
    {
        public static IBackupStrategy CreateStrategy(string strategyType)
        {
            return strategyType.ToLower() switch
            {
                "full" => new FullBackupStrategy(),
                "differential" => new DifferentialBackupStrategy(),
                _ => throw new ArgumentException($"Type de stratégie non supporté: {strategyType}")
            };
        }

        public static IBackupStrategy CreateStrategy(BackupStrategyType strategyType)
        {
            return strategyType switch
            {
                BackupStrategyType.Full => new FullBackupStrategy(),
                BackupStrategyType.Differential => new DifferentialBackupStrategy(),
                _ => throw new ArgumentException($"Type de stratégie non supporté: {strategyType}")
            };
        }
    }

    public enum BackupStrategyType
    {
        Full,
        Differential
    }
} 