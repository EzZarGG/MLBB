using System;
using EasySaveV3._0.Patterns.Strategy;

namespace EasySaveV3._0.Patterns.Factory
{
    public class LogStrategyFactory
    {
        public static ILogStrategy CreateStrategy(string logType)
        {
            return logType.ToLower() switch
            {
                "json" => new JsonLogStrategy(),
                "xml" => new XmlLogStrategy(),
                _ => throw new ArgumentException($"Type de log non supporté: {logType}")
            };
        }

        public static ILogStrategy CreateStrategy(LogType logType)
        {
            return logType switch
            {
                LogType.Json => new JsonLogStrategy(),
                LogType.Xml => new XmlLogStrategy(),
                _ => throw new ArgumentException($"Type de log non supporté: {logType}")
            };
        }
    }

    public enum LogType
    {
        Json,
        Xml
    }
} 