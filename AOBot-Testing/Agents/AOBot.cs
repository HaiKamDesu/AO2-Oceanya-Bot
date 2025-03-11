using AOBot_Testing.Structures;
using System;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;

namespace AOBot_Testing.Agents
{
    public partial class AOBot(string serverAddress, string location)
    {
        private const int textCrawlSpeed = 35;
        private readonly Uri serverUri = new Uri(serverAddress);
        private ClientWebSocket? ws; // WebSocket instance
        public Stopwatch aliveTime = new Stopwatch();
        private CountdownTimer speakTimer;
        bool AbleToSpeak { get; set; } = true;

        private bool isHandshakeComplete = false;

        /// <summary>
        /// Key: Name of the character
        /// Value: Whether the character is available for selection
        /// </summary>
        Dictionary<string, bool> serverCharacterList = new Dictionary<string, bool>();
        public int selectedCharacterIndex = -1;
        public static string lastCharsCheck = string.Empty;

        string hdid;

        public int playerID;
        private string currentArea = location;
        public CharacterINI? currentINI;
        public Emote? currentEmote;

        public string clientName = "DefaultClientName";

        public string OOCShowname = "OceanyaBot";
        //Limit on this is 22
        public string ICShowname = "";
        public string curPos = "";
        public string curBG = "";

        public ICMessage.DeskMods deskMod = ICMessage.DeskMods.Chat;
        public ICMessage.EmoteModifiers emoteMod = ICMessage.EmoteModifiers.NoPreanimation;
        public ICMessage.ShoutModifiers shoutModifiers = ICMessage.ShoutModifiers.Nothing;
        public bool flip = false;
        public ICMessage.Effects effect = ICMessage.Effects.None;
        public ICMessage.TextColors textColor = ICMessage.TextColors.White;
        public bool Immediate = false;
        public bool Additive = false;
        public (int Horizontal, int Vertical) SelfOffset;

        private CharacterINI CurrentINI
        {
            get
            {
                return currentINI;
            }
            set
            {
                currentINI = value;

                if(currentINI != null)
                {
                    currentEmote = value.Emotions.Values.First();
                }
            }
        }

        public Action<string, string, string, string, int> OnMessageReceived;
        public Action<ICMessage> OnICMessageReceived;
        public Action<string, string, bool> OnOOCMessageReceived;
        public Action<CharacterINI> OnChangedCharacter;
        public Action<string> OnBGChange;
        public Action<string> OnSideChange;


        #region Send Message Methods
        public async Task SendICMessage(string showname, string message)
        {
            SetICShowname(showname);
            SendICMessage(message);
        }
        public async Task SendICMessage(string message)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                ICMessage msg = new ICMessage();
                msg.DeskMod = deskMod;
                msg.PreAnim = currentEmote.PreAnimation;
                msg.Character = CurrentINI.Name;
                msg.Emote = currentEmote.Animation;

                // for some reason, specifically for the color red, the server decides to not color the text. So this is a clientside fix.
                if (textColor == ICMessage.TextColors.Red)
                {
                    if(message.StartsWith("~"))
                    {
                        msg.Message = $"{message}~";
                    }
                    else
                    {
                        msg.Message = $"~{message}~";
                    }
                }
                else
                {
                    msg.Message = message;
                }


                msg.Side = curPos;
                msg.SfxName = currentEmote.sfxName;
                msg.EmoteModifier = emoteMod;
                msg.CharId = selectedCharacterIndex;
                msg.SfxDelay = currentEmote.sfxDelay;
                msg.ShoutModifier = shoutModifiers;
                msg.EvidenceID = "0";
                msg.Flip = flip;
                msg.Realization = effect == ICMessage.Effects.Realization;
                msg.TextColor = textColor;
                msg.ShowName = ICShowname;
                msg.OtherCharId = -1;
                msg.SelfOffset = SelfOffset;
                msg.NonInterruptingPreAnim = Immediate;
                msg.SfxLooping = false;
                msg.ScreenShake = false;
                msg.FramesShake = $"-^(b){currentEmote.Animation}^(a){currentEmote.Animation}^";
                msg.FramesRealization = $"-^(b){currentEmote.Animation}^(a){currentEmote.Animation}^";
                msg.FramesSfx = $"-^(b){currentEmote.Animation}^(a){currentEmote.Animation}^";
                msg.Additive = Additive;
                msg.Effect = effect;
                msg.Blips = "";



                string command = ICMessage.GetCommand(msg);

                if(!AbleToSpeak)
                {
                    var remainingTime = speakTimer.GetRemainingTime();

                    if (Globals.DebugMode)
                    {
                        var formattedTime = $"{remainingTime.Hours}h {remainingTime.Minutes}m {remainingTime.Seconds}s {remainingTime.Milliseconds}ms";
                        CustomConsole.WriteLine($"Cannot speak yet, waiting for {formattedTime}...");
                    }
                    await Task.Delay((int)remainingTime.TotalMilliseconds);
                }

                await SendPacket(command);

                await Task.Delay(500); //Wait for command to process server-side
            }
            else
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot send message.");
            }
        }
        public async Task SendOOCMessage(string message)
        {
            SendOOCMessage(OOCShowname, message);
        }
        public async Task SendOOCMessage(string showname, string message)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                OOCShowname = showname;
                string oocMessage = $"CT#{showname}#{message}#%";
                await SendPacket(oocMessage);
                await Task.Delay(500);
            }
            else
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot send message.");
            }
        }
        #endregion

        #region Set Methods
        public async Task SetArea(string areaName)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                string[] areas = areaName.Split('/');
                foreach (var area in areas)
                {
                    string switchRoomCommand = $"MC#{area}#{playerID}#%";
                    await SendPacket(switchRoomCommand);
                    CustomConsole.WriteLine($"Switched to room: {area}");
                    // Allow some time between room switches  
                    await Task.Delay(500);
                }

                currentArea = areaName;
            }
            else
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot switch rooms.");
            }
        }
        public void SetCharacter(string characterName)
        {
            var newChar = new CharacterINI();
            try
            {
                newChar = CharacterINI.FullList.First(c => c.Name == characterName);
            }
            catch
            {
                newChar = CharacterINI.FullList.First(c => c.Name.ToLower() == characterName.ToLower());
            }

            SetCharacter(newChar);
        }
        public void SetCharacter(CharacterINI character)
        {
            CurrentINI = character;
            if (string.IsNullOrEmpty(curPos))
            {
                curPos = character.Side;
            }


            OnChangedCharacter?.Invoke(character);
        }
        public void SetICShowname(string newShowname)
        {
            ICShowname = newShowname;
        }
        public void SetEmote(string emoteDisplayID)
        {
            currentEmote = CurrentINI.Emotions.Values.First(e => e.DisplayID == emoteDisplayID);
        }
        #endregion

        public async Task HandleMessage(string message)
        {
            if (message.StartsWith("CharsCheck#"))
            {
                // Handle CharsCheck response
                lastCharsCheck = message;
                var parts = message.Substring(11).TrimEnd('#', '%').Split('#');
                int index = 0;
                foreach (var key in serverCharacterList.Keys.ToList())
                {
                    if (index < parts.Length)
                    {
                        serverCharacterList[key] = parts[index] == "0";
                        index++;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            else if (message.StartsWith("SC#"))
            {
                var characters = message.Substring(3).TrimEnd('#', '%').Split('#');
                foreach (var character in characters)
                {
                    //Start every character as available to select
                    serverCharacterList[character] = true;
                }
                CustomConsole.WriteLine("Server Character List updated.");
            }
            else if (message.StartsWith("MS#"))
            {
                ICMessage? icMessage = ICMessage.FromConsoleLine(message);
                if (icMessage != null)
                {
                    // Handle IC message
                    OnMessageReceived?.Invoke("IC", icMessage.Character, icMessage.ShowName, icMessage.Message, icMessage.CharId);
                    OnICMessageReceived?.Invoke(icMessage);

                    AbleToSpeak = false;
                    // Set the timer to a value and when it's done, set AbleToSpeak to true
                    speakTimer = new CountdownTimer(new TimeSpan(0,0,0,0,(textCrawlSpeed * 3) * icMessage.Message.Length));
                    speakTimer.TimerElapsed += () =>
                    {
                        AbleToSpeak = true;
                    };
                    speakTimer.Start();
                }
            }
            else if (message.StartsWith("CT#"))
            {
                var fields = message.Split("#");
                var showname = fields[1];
                var messageText = fields[2];
                var fromServer = fields[3].ToString() == "1";

                if (messageText.ToLower().Contains("people in this area: ") && messageText.ToLower().Contains("===") && messageText.Split("\n").Length > 3)
                {
                    List<Player> players = AO2Parser.ParseGetArea(messageText);
                }

                // Handle OOC message
                //you cant get the char id from an ooc message, so just send -1
                OnMessageReceived?.Invoke("OOC", "", showname, messageText, -1);
                OnOOCMessageReceived?.Invoke(showname, messageText, fromServer);
            }
            else if (message.StartsWith("SP#"))
            {
                var fields = message.Split("#");
                var newPos = fields[1];

                curPos = newPos;
                OnSideChange?.Invoke(newPos);
            }
            else if (message.StartsWith("BN#"))
            {
                var fields = message.Split("#");
                var newBG = fields[1];

                curBG = newBG;
                OnBGChange?.Invoke(newBG);
            }
        }

        #region Connection Related Methods
        public async Task Connect()
        {
            aliveTime.Reset();
            ws = new ClientWebSocket();
            // Add required headers (modify as needed)
            ws.Options.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            ws.Options.SetRequestHeader("Origin", "http://example.com");
            try
            {
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                aliveTime.Start();
                CustomConsole.WriteLine("===========================");
                CustomConsole.WriteLine("Connected to AO2's Server WebSocket!");
                CustomConsole.WriteLine("===========================");

                // Start the handshake process
                await PerformHandshake();
                CustomConsole.WriteLine("===========================");

                await SetArea(currentArea);
                CustomConsole.WriteLine("===========================");

                //Allow some time for server to update area info

                await Task.Delay(1000);

                await SelectFirstAvailableINIPuppet();
                CustomConsole.WriteLine("===========================");

                // Start listening for messages
                _ = Task.Run(() => ListenForMessages());
                _ = Task.Run(() => KeepAlive());

                // Allow some time for connection
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine($"Connection Error: {ex.Message}");
            }
        }
        private async Task PerformHandshake()
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot perform handshake.");
                return;
            }

            if (Globals.DebugMode) CustomConsole.WriteLine("Starting handshake...");

            // Step 1: Send Hard Drive ID (HDID) - Can be anything unique
            hdid = Guid.NewGuid().ToString(); // Generate a unique HDID
            await SendPacket($"HI#{hdid}#%");

            // Step 2: Receive Server Version and Player Number
            string response;
            while (true)
            {
                response = await ReceiveMessageAsync();
                if (response.StartsWith("ID#"))
                    break;
            }

            var parts = response.Split('#');
            if (parts.Length >= 3)
            {
                playerID = int.Parse(parts[1]);
                string serverVersion = parts[2];
                CustomConsole.WriteLine($"Assigned Player ID: {playerID} | Server Version: {serverVersion}");
            }

            // Step 3: Send Client Version Info (Server doesn't really care)
            await SendPacket($"ID#MyBotClient#1.0#%");

            // Step 4: Request Character List (Important for selection)
            await SendPacket("RC#%");

            // Step 5: Receive Character List
            while (true)
            {
                response = await ReceiveMessageAsync();
                if (response.StartsWith("SC#"))
                    break;
            }

            // Step 8: Send Ready Signal
            await SendPacket("RD#%");

            // Step 9: Wait for final confirmation
            while (true)
            {
                response = await ReceiveMessageAsync();
                if (response.StartsWith("DONE#"))
                    break;
            }

            isHandshakeComplete = true;
            CustomConsole.WriteLine("Handshake completed successfully!");
        }
        private async Task Reconnect()
        {
            int retryCount = 0;
            while (retryCount < 5)
            {
                try
                {
                    await Connect();
                    if (ws != null && ws.State == WebSocketState.Open)
                    {
                        CustomConsole.WriteLine("Reconnected to WebSocket!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    CustomConsole.WriteLine($"Reconnection attempt {retryCount + 1} failed: {ex.Message}");
                }
                retryCount++;
                await Task.Delay(2000); // Wait before retrying
            }
            CustomConsole.WriteLine("Failed to reconnect after multiple attempts.");
        }
        private async Task KeepAlive()
        {
            while (ws != null && ws.State == WebSocketState.Open)
            {
                await SendPacket($"CH#{playerID}#%");
                await Task.Delay(10000); // Enviar un ping cada 10 segundos
            }
        }
        public async Task Disconnect()
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                ws.Dispose();
                ws = null;
                CustomConsole.WriteLine("Disconnected from WebSocket.");
            }
            else
            {
                CustomConsole.WriteLine("WebSocket is not connected.");
            }
        }
        #endregion

        #region Helper methods

        public async Task SelectFirstAvailableINIPuppet()
        {
            if (ws == null || ws.State != WebSocketState.Open)
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot select INI Puppet.");
                return;
            }

            var parts = lastCharsCheck.Substring(11).TrimEnd('#', '%').Split('#');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "0")
                {
                    // Select the first available character
                    var characterName = serverCharacterList.ElementAt(i).Key;

                    var ini = CharacterINI.FullList.FirstOrDefault(c => c.Name == characterName);

                    //if the ini is null, it means you dont have it in your pc, meaning keep looking for an available one you DO have.
                    if (ini != null)
                    {
                        await SendPacket($"CC#0#{i}#{hdid}#%");

                        selectedCharacterIndex = i;
                        CurrentINI = ini;
                        ICShowname = CurrentINI?.ShowName ?? characterName;
                        CustomConsole.WriteLine($"Selected INI Puppet: \"{characterName}\" (Server Index: {i})");
                        return;
                    }
                }
            }

            CustomConsole.WriteLine("No available INI Puppets to select.");
        }

        public async Task SendPacket(string packet)
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                byte[] messageBytes = Encoding.UTF8.GetBytes(packet);
                await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                if (Globals.DebugMode) CustomConsole.WriteLine($"Sent: {packet}");
            }
            else
            {
                CustomConsole.WriteLine("WebSocket is not connected. Cannot send message.");
            }
        }

        private async Task<string> ReceiveMessageAsync()
        {
            if (ws != null && ws.State == WebSocketState.Open)
            {
                byte[] buffer = new byte[4096];
                WebSocketReceiveResult result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                if (result.Count > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    if (Globals.DebugMode) CustomConsole.WriteLine($"Received: {message}");

                    await HandleMessage(message);

                    return message;
                }
            }
            return string.Empty;
        }

        private async Task ListenForMessages()
        {
            CustomConsole.WriteLine("Listening for messages...");
            try
            {
                while (ws != null && ws.State == WebSocketState.Open)
                {
                    string message = await ReceiveMessageAsync();
                    if (!string.IsNullOrEmpty(message))
                    {
                        // Handle incoming messages here
                    }
                }
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine("===========================");
                CustomConsole.WriteLine("===========================");
                CustomConsole.WriteLine($"Error in message listener: {ex.Message}");
                CustomConsole.WriteLine("===========================");
                CustomConsole.WriteLine("===========================");
            }
            finally
            {
                if (ws == null || ws.State != WebSocketState.Open)
                {
                    aliveTime.Stop();
                    CustomConsole.WriteLine($"WebSocket connection lost after {aliveTime.Elapsed}. Attempting to reconnect...");
                    CustomConsole.WriteLine("===========================");
                    await Reconnect();
                    CustomConsole.WriteLine("===========================");
                }
            }
        }

        #endregion
    }
}
