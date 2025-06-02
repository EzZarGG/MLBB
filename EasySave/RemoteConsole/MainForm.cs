using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using System.Diagnostics;

namespace RemoteConsole
{
    public partial class MainForm : Form
    {
        private TcpClient? client;
        private NetworkStream? stream;
        private readonly string serverIp = "127.0.0.1";
        private readonly int serverPort = 12345;
        private bool isConnected = false;
        private System.Windows.Forms.Timer updateTimer;
        private const int CONNECTION_TIMEOUT_MS = 5000;
        private const int INITIAL_TIMEOUT_MS = 2000;
        private const int BUFFER_SIZE = 8192;
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int RETRY_DELAY_MS = 1000;
        private const int SERVER_CHECK_TIMEOUT_MS = 1000;  // Timeout for server verification
        private readonly object connectionLock = new object();
        private bool isUpdating = false;
        private int consecutiveErrors = 0;
        private const int MAX_CONSECUTIVE_ERRORS = 3;
        private Button? connectButton;

        public MainForm()
        {
            InitializeComponent();
            InitializeCustomComponents();
            updateTimer = new System.Windows.Forms.Timer();
            updateTimer.Interval = 5000; // Change update interval to 5 seconds
            updateTimer.Tick += UpdateTimer_Tick;
        }

        private void InitializeCustomComponents()
        {
            // Connection controls
            connectButton = new Button
            {
                Text = "Connect",
                Location = new Point(10, 10),
                Size = new Size(100, 30),
                Name = "connectButton"
            };
            connectButton.Click += ConnectButton_Click;
            this.Controls.Add(connectButton);

            // Job list view
            ListView jobListView = new ListView
            {
                View = View.Details,
                Location = new Point(10, 50),
                Size = new Size(760, 400),
                FullRowSelect = true,
                GridLines = true,
                Name = "jobListView"
            };
            jobListView.Columns.Add("Job Name", 200);
            jobListView.Columns.Add("Status", 100);
            jobListView.Columns.Add("Progress", 100);
            jobListView.Columns.Add("Source", 200);
            jobListView.Columns.Add("Destination", 200);
            jobListView.SelectedIndexChanged += (s, e) => UpdateControlButtonsState();
            this.Controls.Add(jobListView);

            // Control buttons
            Button pauseButton = new Button
            {
                Text = "Pause",
                Location = new Point(10, 460),
                Size = new Size(100, 30),
                Name = "pauseButton",
                Enabled = false
            };
            pauseButton.Click += PauseButton_Click;
            this.Controls.Add(pauseButton);

            Button resumeButton = new Button
            {
                Text = "Resume",
                Location = new Point(120, 460),
                Size = new Size(100, 30),
                Name = "resumeButton",
                Enabled = false
            };
            resumeButton.Click += ResumeButton_Click;
            this.Controls.Add(resumeButton);

            Button stopButton = new Button
            {
                Text = "Stop",
                Location = new Point(230, 460),
                Size = new Size(100, 30),
                Name = "stopButton",
                Enabled = false
            };
            stopButton.Click += StopButton_Click;
            this.Controls.Add(stopButton);

            // Status label
            Label statusLabel = new Label
            {
                Text = "Disconnected",
                Location = new Point(340, 460),
                Size = new Size(200, 30),
                Name = "statusLabel"
            };
            this.Controls.Add(statusLabel);
        }

        private void Log(string message)
        {
            Trace.WriteLine($"[RemoteConsole] {message}");
        }

        private async Task<bool> IsServerAvailable()
        {
            try
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.ReceiveTimeout = SERVER_CHECK_TIMEOUT_MS;
                    socket.SendTimeout = SERVER_CHECK_TIMEOUT_MS;

                    var connectTask = socket.ConnectAsync(serverIp, serverPort);
                    var timeoutTask = Task.Delay(SERVER_CHECK_TIMEOUT_MS);
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        Log($"Server check timed out after {SERVER_CHECK_TIMEOUT_MS}ms");
                        return false;
                    }

                    try
                    {
                        await connectTask;
                        Log("Server is available");
                        return true;
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
                    {
                        Log("Server is running but refusing connections");
                        return false;
                    }
                    catch
                    {
                        Log("Server check failed");
                        return false;
                    }
                    finally
                    {
                        try { socket.Shutdown(SocketShutdown.Both); } catch { }
                        try { socket.Close(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error checking server availability: {ex.Message}");
                return false;
            }
        }

        private async void ConnectButton_Click(object? sender, EventArgs e)
        {
            if (!isConnected)
            {
                try
                {
                    Log("Checking server availability...");
                    if (!await IsServerAvailable())
                    {
                        MessageBox.Show(
                            "The server is not available. Please check if:\n" +
                            "1. The server is running\n" +
                            "2. The server address and port are correct\n" +
                            "3. No firewall is blocking the connection\n" +
                            "4. The server is not overloaded",
                            "Server Unavailable",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );
                        return;
                    }

                    int retryCount = 0;
                    while (retryCount < MAX_RETRY_ATTEMPTS)
                    {
                        try
                        {
                            Log($"Connection attempt {retryCount + 1} of {MAX_RETRY_ATTEMPTS}");
                            
                            lock (connectionLock)
                            {
                                if (isConnected) return;

                                client = new TcpClient();
                                client.ReceiveTimeout = CONNECTION_TIMEOUT_MS;
                                client.SendTimeout = CONNECTION_TIMEOUT_MS;
                                Log("TcpClient created with timeouts set");
                            }

                            Log("Starting connection attempt...");
                            var connectTask = client.ConnectAsync(serverIp, serverPort);
                            var mainTimeoutTask = Task.Delay(CONNECTION_TIMEOUT_MS);
                            var mainCompletedTask = await Task.WhenAny(connectTask, mainTimeoutTask);
                            
                            if (mainCompletedTask == mainTimeoutTask)
                            {
                                Log($"Connection attempt timed out after {CONNECTION_TIMEOUT_MS}ms");
                                if (retryCount < MAX_RETRY_ATTEMPTS - 1)
                                {
                                    Log($"Retrying in {RETRY_DELAY_MS}ms...");
                                    await Task.Delay(RETRY_DELAY_MS);
                                    retryCount++;
                                    continue;
                                }
                                throw new SocketException((int)SocketError.TimedOut);
                            }

                            try
                            {
                                await connectTask;
                                Log("Connection established successfully");
                                break;
                            }
                            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.ConnectionRefused)
                            {
                                Log("Server refused connection");
                                MessageBox.Show(
                                    "The server refused the connection. This might indicate that:\n" +
                                    "1. The server is not properly configured\n" +
                                    "2. The server is in a paused state\n" +
                                    "3. The server is overloaded\n" +
                                    "Please try again in a few moments.",
                                    "Connection Refused",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                                SafeDisconnect();
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Connection attempt {retryCount + 1} failed: {ex.Message}");
                            if (retryCount < MAX_RETRY_ATTEMPTS - 1)
                            {
                                Log($"Retrying in {RETRY_DELAY_MS}ms...");
                                await Task.Delay(RETRY_DELAY_MS);
                                retryCount++;
                                continue;
                            }
                            MessageBox.Show(
                                $"Failed to connect after {MAX_RETRY_ATTEMPTS} attempts: {ex.Message}",
                                "Connection Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error
                            );
                            SafeDisconnect();
                            return;
                        }
                    }

                    // If we reach here, the connection was successful
                    try
                    {
                        if (!client.Connected)
                        {
                            throw new SocketException((int)SocketError.NotConnected);
                        }

                        stream = client.GetStream();
                        isConnected = true;
                        consecutiveErrors = 0;
                        if (connectButton != null)
                        {
                            connectButton.Text = "Disconnect";
                            connectButton.Enabled = true;
                        }
                        ((Label)Controls.Find("statusLabel", true)[0]).Text = "Connected";
                        EnableControlButtons(true);
                        Log("Connection state initialized");

                        updateTimer.Start();
                        Log("Update timer started");

                        try
                        {
                            Log("Sending initial status request...");
                            await Task.Delay(500);
                            await SendCommand("GET_STATUS");
                            var response = await ReceiveResponse();
                            if (!string.IsNullOrEmpty(response))
                            {
                                Log($"Received initial status response: {response}");
                                UpdateJobList(response);
                            }
                            else
                            {
                                Log("Received empty initial status response");
                            }
                        }
                        catch (Exception ex)
                        {
                            Log($"Initial status update failed: {ex.Message}");
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log($"Error during connection setup: {ex.Message}");
                        MessageBox.Show(
                            $"Error during connection setup: {ex.Message}",
                            "Connection Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error
                        );
                        SafeDisconnect();
                    }
                }
                catch (Exception ex)
                {
                    Log($"Critical error during connection: {ex.Message}");
                    MessageBox.Show(
                        $"Critical error during connection: {ex.Message}",
                        "Connection Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error
                    );
                    SafeDisconnect();
                }
            }
            else
            {
                SafeDisconnect();
            }
        }

        private void SafeDisconnect()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new Action(SafeDisconnect));
                return;
            }

            try
            {
                if (stream != null)
                {
                    try
                    {
                        stream.Close();
                    }
                    catch { /* Ignore close errors */ }
                    stream = null;
                }

                if (client != null)
                {
                    try
                    {
                        client.Close();
                    }
                    catch { /* Ignore close errors */ }
                    client = null;
                }

                isConnected = false;
                consecutiveErrors = 0;
                updateTimer.Stop();

                // Update UI controls
                if (connectButton != null)
                {
                    connectButton.Text = "Connect";
                    connectButton.Enabled = true;
                }
                
                var statusLabel = Controls.Find("statusLabel", true).FirstOrDefault() as Label;
                if (statusLabel != null)
                {
                    statusLabel.Text = "Disconnected";
                }
                
                EnableControlButtons(false);
            }
            catch (Exception ex)
            {
                Log($"SafeDisconnect error: {ex.Message}");
            }
        }

        private void EnableControlButtons(bool enable)
        {
            var pauseButton = Controls.Find("pauseButton", true).FirstOrDefault() as Button;
            var resumeButton = Controls.Find("resumeButton", true).FirstOrDefault() as Button;
            var stopButton = Controls.Find("stopButton", true).FirstOrDefault() as Button;

            if (pauseButton != null && resumeButton != null && stopButton != null)
            {
                pauseButton.Enabled = enable;
                resumeButton.Enabled = enable;
                stopButton.Enabled = enable;
            }
        }

        private void UpdateTimer_Tick(object? sender, EventArgs e)
        {
            if (!isConnected || stream == null || isUpdating) return;

            try
            {
                isUpdating = true;
                Task.Run(async () =>
                {
                    try
                    {
                        // Add a small delay between status updates
                        await Task.Delay(100);
                        
                        await SendCommand("GET_STATUS");
                        var response = await ReceiveResponse();
                        
                        if (!string.IsNullOrEmpty(response))
                        {
                            // Use Invoke to update UI from background thread
                            this.Invoke((MethodInvoker)delegate
                            {
                                try
                                {
                                    UpdateJobList(response);
                                    consecutiveErrors = 0; // Reset error counter on successful update
                                }
                                catch (Exception ex)
                                {
                                    // Log UI update error but don't disconnect
                                    Log($"UI update error: {ex.Message}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        consecutiveErrors++;
                        
                        // Use BeginInvoke to safely handle UI updates from background thread
                        this.BeginInvoke((MethodInvoker)delegate
                        {
                            try
                            {
                                // Only show error message if we've had multiple consecutive failures
                                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                                {
                                    MessageBox.Show($"Connection lost: {ex.Message}", "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    SafeDisconnect();
                                }
                                else
                                {
                                    // For transient errors, just log them
                                    Log($"Status update error (attempt {consecutiveErrors}): {ex.Message}");
                                }
                            }
                            catch (Exception uiEx)
                            {
                                Log($"UI Error: {uiEx.Message}");
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                consecutiveErrors++;
                if (consecutiveErrors >= MAX_CONSECUTIVE_ERRORS)
                {
                    MessageBox.Show($"Update failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    SafeDisconnect();
                }
                else
                {
                    Log($"Timer error (attempt {consecutiveErrors}): {ex.Message}");
                }
            }
            finally
            {
                isUpdating = false;
            }
        }

        private async Task SendCommand(string command, string? jobName = null)
        {
            if (stream == null || !isConnected)
            {
                Log("Cannot send command: stream is null or not connected");
                return;
            }

            try
            {
                var commandObj = new
                {
                    Command = command,
                    JobName = jobName
                };

                var json = JsonConvert.SerializeObject(commandObj);
                var data = Encoding.UTF8.GetBytes(json + "\n");
                Log($"Sending command: {json}");
                
                await Task.Run(() =>
                {
                    lock (connectionLock)
                    {
                        if (stream != null && isConnected)
                        {
                            stream.Write(data, 0, data.Length);
                            stream.Flush();
                            Log("Command sent successfully");
                        }
                        else
                        {
                            throw new IOException("Connection is not available");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error sending command: {ex.Message}");
                if (isConnected)
                {
                    throw new IOException($"Failed to send command: {ex.Message}", ex);
                }
            }
        }

        private async Task<string> ReceiveResponse()
        {
            if (stream == null || !isConnected)
            {
                Log("Cannot receive response: stream is null or not connected");
                return string.Empty;
            }

            try
            {
                var buffer = new byte[BUFFER_SIZE];
                int bytesRead;

                Log("Waiting for response...");
                bytesRead = await Task.Run(() =>
                {
                    lock (connectionLock)
                    {
                        if (stream == null || !isConnected)
                        {
                            throw new IOException("Connection is not available");
                        }
                        return stream.Read(buffer, 0, buffer.Length);
                    }
                });

                if (bytesRead == 0)
                {
                    Log("Connection closed by server (0 bytes read)");
                    throw new IOException("Connection closed by server");
                }

                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Log($"Received response: {response}");

                if (string.IsNullOrEmpty(response))
                {
                    Log("Received empty response from server");
                    throw new IOException("Received empty response from server");
                }

                return response;
            }
            catch (Exception ex)
            {
                Log($"Error receiving response: {ex.Message}");
                if (isConnected)
                {
                    throw new IOException($"Failed to receive response: {ex.Message}", ex);
                }
                return string.Empty;
            }
        }

        private void UpdateJobList(string jsonResponse)
        {
            try
            {
                var jobs = JsonConvert.DeserializeObject<List<JobStatus>>(jsonResponse);
                var listView = Controls.Find("jobListView", true).FirstOrDefault() as ListView;
                if (listView == null)
                {
                    Log("Job list view not found");
                    return;
                }

                listView.BeginUpdate();
                listView.Items.Clear();

                if (jobs != null)
                {
                    foreach (var job in jobs)
                    {
                        var item = new ListViewItem(new[]
                        {
                            job.Name,
                            job.Status,
                            $"{job.Progress}%",
                            job.Source,
                            job.Destination
                        });
                        listView.Items.Add(item);
                    }
                }

                listView.EndUpdate();

                // Update control buttons state
                UpdateControlButtonsState();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to update job list: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateControlButtonsState()
        {
            var listView = Controls.Find("jobListView", true).FirstOrDefault() as ListView;
            if (listView == null || listView.SelectedItems.Count == 0)
            {
                EnableControlButtons(false);
                return;
            }

            var selectedItem = listView.SelectedItems[0];
            var status = selectedItem.SubItems[1].Text;

            var pauseButton = Controls.Find("pauseButton", true).FirstOrDefault() as Button;
            var resumeButton = Controls.Find("resumeButton", true).FirstOrDefault() as Button;
            var stopButton = Controls.Find("stopButton", true).FirstOrDefault() as Button;

            if (pauseButton != null && resumeButton != null && stopButton != null)
            {
                pauseButton.Enabled = status.Equals("Active", StringComparison.OrdinalIgnoreCase);
                resumeButton.Enabled = status.Equals("Paused", StringComparison.OrdinalIgnoreCase);
                stopButton.Enabled = status.Equals("Active", StringComparison.OrdinalIgnoreCase) || 
                                   status.Equals("Paused", StringComparison.OrdinalIgnoreCase);
            }
        }

        private async void PauseButton_Click(object? sender, EventArgs e)
        {
            var listView = (ListView)Controls.Find("jobListView", true)[0];
            if (listView.SelectedItems.Count > 0)
            {
                var jobName = listView.SelectedItems[0].Text;
                try
                {
                    await SendCommand("PAUSE", jobName);
                    var response = await ReceiveResponse();
                    if (!string.IsNullOrEmpty(response))
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(response);
                        if (result?.error != null)
                        {
                            MessageBox.Show(result.error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            // Refresh the job list to update the status
                            await SendCommand("GET_STATUS");
                            var statusResponse = await ReceiveResponse();
                            if (!string.IsNullOrEmpty(statusResponse))
                            {
                                UpdateJobList(statusResponse);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to pause backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void ResumeButton_Click(object? sender, EventArgs e)
        {
            var listView = (ListView)Controls.Find("jobListView", true)[0];
            if (listView.SelectedItems.Count > 0)
            {
                var jobName = listView.SelectedItems[0].Text;
                try
                {
                    await SendCommand("RESUME", jobName);
                    var response = await ReceiveResponse();
                    if (!string.IsNullOrEmpty(response))
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(response);
                        if (result?.error != null)
                        {
                            MessageBox.Show(result.error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            // Refresh the job list to update the status
                            await SendCommand("GET_STATUS");
                            var statusResponse = await ReceiveResponse();
                            if (!string.IsNullOrEmpty(statusResponse))
                            {
                                UpdateJobList(statusResponse);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to resume backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void StopButton_Click(object? sender, EventArgs e)
        {
            var listView = (ListView)Controls.Find("jobListView", true)[0];
            if (listView.SelectedItems.Count > 0)
            {
                var jobName = listView.SelectedItems[0].Text;
                try
                {
                    await SendCommand("STOP", jobName);
                    var response = await ReceiveResponse();
                    if (!string.IsNullOrEmpty(response))
                    {
                        var result = JsonConvert.DeserializeObject<dynamic>(response);
                        if (result?.error != null)
                        {
                            MessageBox.Show(result.error.ToString(), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        else
                        {
                            // Refresh the job list to update the status
                            await SendCommand("GET_STATUS");
                            var statusResponse = await ReceiveResponse();
                            if (!string.IsNullOrEmpty(statusResponse))
                            {
                                UpdateJobList(statusResponse);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to stop backup: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SafeDisconnect();
            base.OnFormClosing(e);
        }
    }

    public class JobStatus
    {
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
    }
} 