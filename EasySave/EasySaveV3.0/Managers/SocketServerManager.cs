using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using EasySaveV3._0.Controllers;
using EasySaveV3._0.Models;
using EasySaveLogging;
using System.Diagnostics;

namespace EasySaveV3._0.Managers
{
    public class SocketServerManager
    {
        private readonly TcpListener _server;
        private readonly BackupController _backupController;
        private readonly Logger _logger;
        private bool _isRunning;
        private const int PORT = 12345;
        private const int CONNECTION_TIMEOUT_MS = 5000;
        private const int BUFFER_SIZE = 8192;

        private void Log(string message)
        {
            Trace.WriteLine($"[EasySave Server] {message}");
        }

        private void DebugLog(string message)
        {
            Debug.WriteLine($"[EasySave Server] {message}");
        }

        public SocketServerManager(BackupController backupController)
        {
            _backupController = backupController ?? throw new ArgumentNullException(nameof(backupController));
            _server = new TcpListener(IPAddress.Any, PORT);
            _logger = Logger.GetInstance();
            Log($"Socket server initialized on port {PORT}");
        }

        public async Task StartServer()
        {
            if (_isRunning)
            {
                DebugLog("Server is already running");
                return;
            }

            try
            {
                DebugLog($"Starting socket server on port {PORT}...");
                _server.Start();
                _isRunning = true;
                DebugLog($"Socket server started successfully on {_server.LocalEndpoint}");

                while (_isRunning)
                {
                    try
                    {
                        DebugLog("Waiting for client connection...");
                        var client = await _server.AcceptTcpClientAsync();
                        DebugLog($"New client connection accepted from {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
                        
                        // Client configuration
                        client.ReceiveTimeout = CONNECTION_TIMEOUT_MS;
                        client.SendTimeout = CONNECTION_TIMEOUT_MS;
                        client.NoDelay = true; // Disable Nagle's algorithm
                        client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                        
                        DebugLog($"Client configured - Timeouts: {CONNECTION_TIMEOUT_MS}ms, NoDelay: true, KeepAlive: true");
                        _ = HandleClientAsync(client);
                    }
                    catch (Exception ex) when (ex is ObjectDisposedException || ex is InvalidOperationException)
                    {
                        DebugLog($"Server stopped: {ex.Message}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        DebugLog($"Error accepting client: {ex.Message}");
                        if (ex.InnerException != null)
                        {
                            DebugLog($"Inner exception: {ex.InnerException.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to start socket server: {ex.Message}");
                if (ex.InnerException != null)
                {
                    DebugLog($"Inner exception: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public void StopServer()
        {
            if (!_isRunning)
            {
                DebugLog("Server is not running");
                return;
            }

            try
            {
                DebugLog("Stopping socket server...");
                _isRunning = false;
                _server.Stop();
                DebugLog("Socket server stopped successfully");
            }
            catch (Exception ex)
            {
                DebugLog($"Error stopping socket server: {ex.Message}");
                throw;
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var clientEndPoint = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            DebugLog($"Starting to handle client {clientEndPoint}");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    DebugLog($"Stream created for client {clientEndPoint}");
                    var buffer = new byte[BUFFER_SIZE];
                    
                    while (true)
                    {
                        try
                        {
                            if (!client.Connected)
                            {
                                DebugLog($"Client {clientEndPoint} disconnected (Connected = false)");
                                break;
                            }

                            if (!client.Client.Connected)
                            {
                                DebugLog($"Client {clientEndPoint} disconnected (Socket.Connected = false)");
                                break;
                            }

                            DebugLog($"Waiting for command from client {clientEndPoint}...");
                            var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                            DebugLog($"Read {bytesRead} bytes from client {clientEndPoint}");
                            
                            if (bytesRead == 0)
                            {
                                DebugLog($"Client {clientEndPoint} disconnected gracefully (0 bytes read)");
                                break;
                            }

                            var command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                            if (string.IsNullOrEmpty(command))
                            {
                                DebugLog($"Received empty command from client {clientEndPoint}");
                                continue;
                            }

                            DebugLog($"Received command from client {clientEndPoint}: {command}");
                            var response = await ProcessCommandAsync(command);
                            DebugLog($"Sending response to client {clientEndPoint}: {response}");

                            var responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                            DebugLog($"Writing {responseBytes.Length} bytes to client {clientEndPoint}");
                            await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                            await stream.FlushAsync();
                            DebugLog($"Response sent to client {clientEndPoint}");

                            // Check connection state after sending
                            if (!client.Connected)
                            {
                                DebugLog($"Client {clientEndPoint} disconnected after sending response");
                                break;
                            }
                        }
                        catch (IOException ex) when (ex.InnerException is SocketException socketEx)
                        {
                            DebugLog($"Socket error for client {clientEndPoint}: {socketEx.Message} (Error code: {socketEx.ErrorCode})");
                            break;
                        }
                        catch (IOException ex)
                        {
                            DebugLog($"IO error for client {clientEndPoint}: {ex.Message}");
                            break;
                        }
                        catch (Exception ex)
                        {
                            DebugLog($"Error processing command from client {clientEndPoint}: {ex.Message}");
                            try
                            {
                                var errorResponse = JsonConvert.SerializeObject(new { error = ex.Message });
                                var errorBytes = Encoding.UTF8.GetBytes(errorResponse + "\n");
                                DebugLog($"Sending error response to client {clientEndPoint}");
                                await stream.WriteAsync(errorBytes, 0, errorBytes.Length);
                                await stream.FlushAsync();
                                DebugLog($"Error response sent to client {clientEndPoint}");
                            }
                            catch (Exception sendEx)
                            {
                                DebugLog($"Could not send error response to client {clientEndPoint}: {sendEx.Message}");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLog($"Critical error handling client {clientEndPoint}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    DebugLog($"Inner exception: {ex.InnerException.Message}");
                }
            }
            finally
            {
                try
                {
                    if (client != null)
                    {
                        DebugLog($"Client {clientEndPoint} final state - Connected: {client.Connected}, Socket.Connected: {client.Client?.Connected}");
                    }
                }
                catch (Exception ex)
                {
                    DebugLog($"Error checking final client state: {ex.Message}");
                }
                DebugLog($"Finished handling client {clientEndPoint}");
            }
        }

        private async Task<string> ProcessCommandAsync(string commandJson)
        {
            try
            {
                Log($"Processing command: {commandJson}");
                var command = JsonConvert.DeserializeObject<RemoteCommand>(commandJson);
                if (command == null)
                {
                    Log("Invalid command format received");
                    return JsonConvert.SerializeObject(new { error = "Invalid command format" });
                }

                string response;
                switch (command.Command.ToUpper())
                {
                    case "GET_STATUS":
                        Log("Processing GET_STATUS command");
                        var backups = await Task.Run(() => _backupController.GetBackups());
                        var statuses = backups.Select(b => new BackupJobStatus
                        {
                            Name = b.Name,
                            Status = _backupController.GetBackupState(b.Name)?.Status ?? "Unknown",
                            Progress = _backupController.GetBackupState(b.Name)?.ProgressPercentage ?? 0,
                            Source = b.SourcePath,
                            Destination = b.TargetPath
                        }).ToList();
                        response = JsonConvert.SerializeObject(statuses);
                        Log($"GET_STATUS response: {response}");
                        return response;

                    case "PAUSE":
                        if (string.IsNullOrEmpty(command.JobName))
                        {
                            Log("PAUSE command received without job name");
                            return JsonConvert.SerializeObject(new { error = "Job name is required" });
                        }
                        Log($"Processing PAUSE command for job: {command.JobName}");
                        try
                        {
                            _backupController.PauseBackup(command.JobName);
                            return JsonConvert.SerializeObject(new { message = "Backup paused successfully" });
                        }
                        catch (Exception ex)
                        {
                            Log($"Error pausing backup: {ex.Message}");
                            return JsonConvert.SerializeObject(new { error = ex.Message });
                        }

                    case "RESUME":
                        if (string.IsNullOrEmpty(command.JobName))
                        {
                            Log("RESUME command received without job name");
                            return JsonConvert.SerializeObject(new { error = "Job name is required" });
                        }
                        Log($"Processing RESUME command for job: {command.JobName}");
                        try
                        {
                            _backupController.ResumeBackup(command.JobName);
                            return JsonConvert.SerializeObject(new { message = "Backup resumed successfully" });
                        }
                        catch (Exception ex)
                        {
                            Log($"Error resuming backup: {ex.Message}");
                            return JsonConvert.SerializeObject(new { error = ex.Message });
                        }

                    case "STOP":
                        if (string.IsNullOrEmpty(command.JobName))
                        {
                            Log("STOP command received without job name");
                            return JsonConvert.SerializeObject(new { error = "Job name is required" });
                        }
                        Log($"Processing STOP command for job: {command.JobName}");
                        try
                        {
                            _backupController.StopBackup(command.JobName);
                            return JsonConvert.SerializeObject(new { message = "Backup stopped successfully" });
                        }
                        catch (Exception ex)
                        {
                            Log($"Error stopping backup: {ex.Message}");
                            return JsonConvert.SerializeObject(new { error = ex.Message });
                        }

                    default:
                        Log($"Unknown command received: {command.Command}");
                        return JsonConvert.SerializeObject(new { error = "Unknown command" });
                }
            }
            catch (Exception ex)
            {
                Log($"Error processing command: {ex.Message}");
                return JsonConvert.SerializeObject(new { error = ex.Message });
            }
        }
    }

    public class RemoteCommand
    {
        public string Command { get; set; } = string.Empty;
        public string? JobName { get; set; }
    }

    public class BackupJobStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }
} 