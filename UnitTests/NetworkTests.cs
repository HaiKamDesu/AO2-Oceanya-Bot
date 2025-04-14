using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestFixture]
    [Category("NoNetworkCall")]
    public class NetworkTests
    {
        // These tests use a mock server so no real network connections are made
        // This is a mock server implementation for testing network functionality
        private class MockAO2Server : IDisposable
        {
            private readonly HttpListener _listener;
            private readonly CancellationTokenSource _cts;
            private readonly List<WebSocket> _clients = new List<WebSocket>();
            private readonly Dictionary<string, Action<WebSocket, string>> _packetHandlers = new Dictionary<string, Action<WebSocket, string>>();
            private readonly Queue<string> _receivedMessages = new Queue<string>();
            
            public bool IsRunning { get; private set; }
            public IReadOnlyList<WebSocket> Clients => _clients.AsReadOnly();

            public MockAO2Server(string uri)
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add(uri);
                _cts = new CancellationTokenSource();
                
                // Setup default packet handlers
                _packetHandlers["HI"] = HandleHI;
                _packetHandlers["ID"] = HandleID;
                _packetHandlers["RC"] = HandleRC;
                _packetHandlers["RD"] = HandleRD;
                _packetHandlers["CC"] = HandleCC;
                _packetHandlers["MC"] = HandleMC;
                _packetHandlers["CH"] = HandleCH;
                _packetHandlers["MS"] = HandleMS;
                _packetHandlers["CT"] = HandleCT;
            }

            public void Start()
            {
                _listener.Start();
                IsRunning = true;
                Task.Run(ListenForClientsAsync);
            }

            public void Stop()
            {
                _cts.Cancel();
                
                foreach (var client in _clients)
                {
                    try
                    {
                        if (client.State == WebSocketState.Open)
                        {
                            client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server shutting down", CancellationToken.None).Wait();
                        }
                    }
                    catch { }
                }
                
                _clients.Clear();
                _listener.Stop();
                IsRunning = false;
            }

            public string GetNextReceivedMessage()
            {
                if (_receivedMessages.Count > 0)
                {
                    return _receivedMessages.Dequeue();
                }
                return null;
            }

            public void SendToAllClients(string message)
            {
                foreach (var client in _clients.ToList())
                {
                    if (client.State == WebSocketState.Open)
                    {
                        var buffer = Encoding.UTF8.GetBytes(message);
                        client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                    }
                }
            }

            private async Task ListenForClientsAsync()
            {
                try
                {
                    while (!_cts.IsCancellationRequested)
                    {
                        var context = await _listener.GetContextAsync();
                        
                        if (context.Request.IsWebSocketRequest)
                        {
                            var webSocketContext = await context.AcceptWebSocketAsync(null);
                            var webSocket = webSocketContext.WebSocket;
                            
                            _clients.Add(webSocket);
                            
                            _ = HandleClientAsync(webSocket);
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            context.Response.Close();
                        }
                    }
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                }
                catch (Exception ex)
                {
                    CustomConsole.Error("Error in WebSocket server", ex);
                }
            }

            private async Task HandleClientAsync(WebSocket webSocket)
            {
                var buffer = new byte[4096];
                
                try
                {
                    while (webSocket.State == WebSocketState.Open && !_cts.IsCancellationRequested)
                    {
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        
                        if (result.MessageType == WebSocketMessageType.Text)
                        {
                            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            _receivedMessages.Enqueue(message);
                            
                            ProcessPacket(webSocket, message);
                        }
                        else if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                            _clients.Remove(webSocket);
                            break;
                        }
                    }
                }
                catch
                {
                    // Client disconnected
                    if (_clients.Contains(webSocket))
                    {
                        _clients.Remove(webSocket);
                    }
                }
            }

            private void ProcessPacket(WebSocket client, string message)
            {
                if (!message.EndsWith("#%") || !message.Contains("#"))
                {
                    return;
                }
                
                string header = message.Split('#')[0];
                
                if (_packetHandlers.TryGetValue(header, out var handler))
                {
                    handler(client, message);
                }
            }

            #region Packet Handlers
            private void HandleHI(WebSocket client, string message)
            {
                // Send ID packet with player ID and server version
                SendPacket(client, "ID#1#2.11.0#%");
            }

            private void HandleID(WebSocket client, string message)
            {
                // Client sends its version, send character list
                SendPacket(client, "SC#Phoenix#Franziska#Miles#Apollo#Athena#%");
                
                // Send DONE to signal end of character list
                SendPacket(client, "DONE#%");
            }

            private void HandleRC(WebSocket client, string message)
            {
                // Send character availability (0 = available, 1 = taken)
                SendPacket(client, "CharsCheck#0#0#0#0#0#%");
            }

            private void HandleRD(WebSocket client, string message)
            {
                // Client is ready, send areas list
                SendPacket(client, "AREAS#Lobby#Basement#Courtroom#%");
            }

            private void HandleCC(WebSocket client, string message)
            {
                // Character change, respond with updated character list
                SendPacket(client, "CharsCheck#0#0#0#0#0#%");
            }

            private void HandleMC(WebSocket client, string message)
            {
                // Area change
                var parts = message.Split('#');
                if (parts.Length >= 3)
                {
                    var area = parts[1];
                    // Send background
                    SendPacket(client, $"BN#{area}_bg#%");
                    
                    // Send player list
                    SendPacket(client, "CT#" + Globals.ReplaceSymbolsForText("SERVER") + "#" +
                                      Globals.ReplaceSymbolsForText($"People in this area: \n=================\n[1] Phoenix\n=================") + "#1#%");
                }
            }

            private void HandleCH(WebSocket client, string message)
            {
                // Ping/keepalive, just acknowledge
            }

            private void HandleMS(WebSocket client, string message)
            {
                // Echo the IC message back (simulating seeing your own message)
                SendPacket(client, message);
            }

            private void HandleCT(WebSocket client, string message)
            {
                // Echo the OOC message back (simulating seeing your own message)
                SendPacket(client, message);
            }
            #endregion

            private void SendPacket(WebSocket client, string packet)
            {
                if (client.State == WebSocketState.Open)
                {
                    var buffer = Encoding.UTF8.GetBytes(packet);
                    client.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None).Wait();
                }
            }

            public void Dispose()
            {
                Stop();
                _cts.Dispose();
            }
        }

        private MockAO2Server _mockServer = null!;
        private const string MockServerUrl = "http://localhost:8080/";
        private const string MockWebSocketUrl = "ws://localhost:8080/";
        
        [OneTimeSetUp]
        public void SetupServer()
        {
            _mockServer = new MockAO2Server(MockServerUrl);
            _mockServer.Start();
        }
        
        [OneTimeTearDown]
        public void TearDownServer()
        {
            _mockServer?.Dispose();
        }
        
        [Test]
        public async Task Test_Connection()
        {
            // Create client with mock server address
            var client = new AOClient(MockWebSocketUrl, "Basement");
            
            try
            {
                // Connect and wait for completion
                await client.Connect();
                
                // Check connection state
                Assert.That(client.aliveTime.IsRunning, Is.True, "Client should be tracking alive time");
                
                // Wait a bit for handshake to complete
                await Task.Delay(500);
                
                // Verify client properties are set correctly
                Assert.That(client.playerID, Is.EqualTo(1), "Player ID should be set");
            }
            finally
            {
                // Cleanup
                await client.Disconnect();
            }
        }
        
        [Test]
        public async Task Test_CharacterSelection()
        {
            // Create client with mock server address
            var client = new AOClient(MockWebSocketUrl, "Basement");
            
            try
            {
                // Connect and wait for completion
                await client.Connect();
                await Task.Delay(500);
                
                // Choose a character
                client.SetCharacter("Phoenix");
                
                // Verify character is set
                Assert.That(client.currentINI, Is.Not.Null, "Character INI should be set");
                Assert.That(client.currentINI.Name, Is.EqualTo("Phoenix"), "Character name should match");
                
                // Change character
                client.SetCharacter("Franziska");
                
                // Verify character changed
                Assert.That(client.currentINI.Name, Is.EqualTo("Franziska"), "Character name should update after change");
            }
            finally
            {
                // Cleanup
                await client.Disconnect();
            }
        }
        
        [Test]
        public async Task Test_MessageSending()
        {
            // Create client with mock server address
            var client = new AOClient(MockWebSocketUrl, "Basement");
            
            try
            {
                // Connect client
                await client.Connect();
                await Task.Delay(500);
                
                // Set up a flag to check message receipt
                bool messageReceived = false;
                string receivedMessage = null;
                
                // Set up message handler
                client.OnMessageReceived += (chatLogType, characterName, showName, message, charId) => 
                {
                    if (characterName == client.currentINI?.Name && message == "Test message")
                    {
                        messageReceived = true;
                        receivedMessage = message;
                    }
                };
                
                // Send IC message
                await client.SendICMessage("Test message");
                
                // Wait a bit for message to be processed
                await Task.Delay(500);
                
                // Verify message was sent and received back (echo from mock server)
                Assert.That(messageReceived, Is.True, "Message should be received");
                Assert.That(receivedMessage, Is.EqualTo("Test message"), "Received message content should match");
                
                // Test OOC message
                messageReceived = false;
                receivedMessage = null;
                
                // Set up OOC message handler
                client.OnOOCMessageReceived += (showName, message, fromServer) => 
                {
                    if (showName == client.OOCShowname && message == "Test OOC message")
                    {
                        messageReceived = true;
                        receivedMessage = message;
                    }
                };
                
                // Send OOC message
                await client.SendOOCMessage("Test OOC message");
                
                // Wait a bit for message to be processed
                await Task.Delay(500);
                
                // Verify OOC message was sent and received back
                Assert.That(messageReceived, Is.True, "OOC Message should be received");
                Assert.That(receivedMessage, Is.EqualTo("Test OOC message"), "Received OOC message content should match");
            }
            finally
            {
                // Cleanup
                await client.Disconnect();
            }
        }
        
        [Test]
        public async Task Test_AreaChange()
        {
            // Create client with mock server address
            var client = new AOClient(MockWebSocketUrl, "Basement");
            
            try
            {
                // Connect client
                await client.Connect();
                await Task.Delay(500);
                
                // Set up a flag to check area change
                bool bgChanged = false;
                string newBg = null;
                
                // Set up message handler
                client.OnBGChange += (bg) => 
                {
                    bgChanged = true;
                    newBg = bg;
                };
                
                // Change area
                await client.SetArea("Courtroom");
                
                // Wait a bit for area change to be processed
                await Task.Delay(500);
                
                // Verify area was changed
                Assert.That(bgChanged, Is.True, "Background should have changed");
                Assert.That(newBg, Is.EqualTo("Courtroom_bg"), "Background should match new area");
            }
            finally
            {
                // Cleanup
                await client.Disconnect();
            }
        }
        
        [Test]
        public async Task Test_HandleInvalidMessages()
        {
            // Create client with mock server address
            var client = new AOClient(MockWebSocketUrl, "Basement");
            
            try
            {
                // Connect client
                await client.Connect();
                await Task.Delay(500);
                
                // Test handling incomplete MS message
                string incompleteMessage = "MS#1#pre#char#emote#message#wit#sfx#%";
                await client.HandleMessage(incompleteMessage);
                
                // Test handling message with invalid header
                string invalidHeader = "XX#invalid#message#%";
                await client.HandleMessage(invalidHeader);
                
                // No exception should be thrown
                Assert.Pass("Client handled invalid messages without crashing");
            }
            finally
            {
                // Cleanup
                await client.Disconnect();
            }
        }
        
        [Test]
        public async Task Test_Reconnection()
        {
            // Skip this test for now since it's hard to properly simulate 
            // disconnect and reconnect with the mock server
            Assert.Ignore("Reconnection test requires more complex server simulation");
        }
    }
}