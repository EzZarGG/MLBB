using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using EasySaveV2._0.Models;

namespace EasySaveV2._0.Patterns.Strategy
{
    public class XmlLogStrategy : ILogStrategy
    {
        private readonly string _logDirectory;
        private readonly object _logLock = new object();

        public XmlLogStrategy()
        {
            _logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            InitializeLogDirectory();
        }

        private void InitializeLogDirectory()
        {
            if (!Directory.Exists(_logDirectory))
            {
                Directory.CreateDirectory(_logDirectory);
            }
        }

        private string GetLogFilePath()
        {
            return Path.Combine(_logDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.xml");
        }

        public async Task WriteLogAsync(string message, LogLevel level, string source)
        {
            string logFilePath = GetLogFilePath();
            XDocument doc;

            try
            {
                if (File.Exists(logFilePath))
                {
                    doc = XDocument.Load(logFilePath);
                }
                else
                {
                    doc = new XDocument(new XElement("Logs"));
                }

                var logEntry = new XElement("Log",
                    new XElement("Timestamp", DateTime.Now),
                    new XElement("Level", level.ToString()),
                    new XElement("Source", source ?? "Application"),
                    new XElement("Message", message)
                );

                doc.Root.Add(logEntry);

                using (var writer = new StreamWriter(logFilePath))
                {
                    await writer.WriteAsync(doc.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de l'écriture du log XML: {ex.Message}");
            }
        }

        public async Task<string> ReadLogsAsync(DateTime? startDate = null, DateTime? endDate = null, LogLevel? level = null)
        {
            string logFilePath = GetLogFilePath();
            try
            {
                if (!File.Exists(logFilePath))
                    return "<Logs></Logs>";

                var doc = XDocument.Load(logFilePath);
                var logs = doc.Root.Elements("Log")
                    .Where(log =>
                    {
                        var timestamp = DateTime.Parse(log.Element("Timestamp").Value);
                        var logLevel = log.Element("Level").Value;

                        return (!startDate.HasValue || timestamp >= startDate.Value) &&
                               (!endDate.HasValue || timestamp <= endDate.Value) &&
                               (!level.HasValue || logLevel == level.Value.ToString());
                    });

                var filteredDoc = new XDocument(new XElement("Logs", logs));
                return filteredDoc.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la lecture des logs XML: {ex.Message}");
                return "<Logs></Logs>";
            }
        }

        public string GetFileExtension() => ".xml";
    }
} 