using System;
using System.IO;
using System.Security.Policy;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using Common;
using NAudio.Wave;
using OceanyaClient.Components;
using OceanyaClient.Utilities;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;

namespace OceanyaClient
{
    public partial class MainWindow : Window
    {
        Dictionary<ToggleButton, AOClient> clients = new Dictionary<ToggleButton, AOClient>();
        AOClient currentClient;
        private bool debug = false;

        List<ToggleButton> objectionModifiers;
        public MainWindow()
        {
            AudioPlayer.PlayEmbeddedSound("Resources/ApertureScienceJingleHD.mp3", 0.5f);

            InitializeComponent();
            WindowHelper.AddWindow(this);

            objectionModifiers = new List<ToggleButton> { HoldIt, Objection, TakeThat, Custom };
            // Set grid mode and size
            EmoteGrid.SetScrollMode(PageButtonGrid.ScrollMode.Vertical);
            EmoteGrid.SetPageSize(4, 1);
            OOCLogControl.IsEnabled = false;
            ICLogControl.IsEnabled = false;
            ICMessageSettingsControl.IsEnabled = false;

            OOCLogControl.OnSendOOCMessage += async (showName, message) =>
            {
                SaveFile.Data.OOCName = showName;
                SaveFile.Save();
                await currentClient.SendOOCMessage(showName, message);
            };

            ICMessageSettingsControl.OnSendICMessage += async (message) =>
            {
                // Split the message to get the client name and the actual message
                var splitMessage = message.Split(new[] { ':' }, 2);
                AOClient client = null;
                var sendMessage = message;
                if (splitMessage.Length == 2)
                {
                    var clientName = splitMessage[0].Trim();
                    var actualMessage = splitMessage[1].Trim();

                    // Find the client by name (case insensitive)
                    var targetClient = clients.Values.FirstOrDefault(bot => string.Equals(bot.clientName, clientName, StringComparison.OrdinalIgnoreCase));

                    if (targetClient != null)
                    {
                        sendMessage = actualMessage;
                        client = targetClient;
                        SelectClient(client);
                    }
                    else
                    {
                        client = currentClient;
                    }
                }
                else
                {
                    client = currentClient;
                }

                if (!string.IsNullOrWhiteSpace(sendMessage))
                {
                    sendMessage = sendMessage.Trim();
                }
                else if(sendMessage == "")
                {
                    sendMessage = " ";
                }

                client.shoutModifiers = ICMessage.ShoutModifiers.Nothing;

                if (HoldIt.IsChecked.Value)
                {
                    client.shoutModifiers = ICMessage.ShoutModifiers.HoldIt;
                }
                else if (Objection.IsChecked.Value)
                {
                    client.shoutModifiers = ICMessage.ShoutModifiers.Objection;
                }
                else if (TakeThat.IsChecked.Value)
                {
                    client.shoutModifiers = ICMessage.ShoutModifiers.TakeThat;
                }
                else if (Custom.IsChecked.Value)
                {
                    client.shoutModifiers = ICMessage.ShoutModifiers.Custom;
                }


                void OnICMessageReceivedHandler(ICMessage icMessage)
                {
                    if (icMessage.CharId == client.iniPuppetID && 
                    (icMessage.Message == "~"+sendMessage+"~" || icMessage.Message == sendMessage || icMessage.Message == sendMessage+"~"))
                    {
                        // Message was received by server.
                        ICMessageSettingsControl.Dispatcher.Invoke(() =>
                        {
                            ICMessageSettingsControl.txtICMessage.Text = "";

                            if (!ICMessageSettingsControl.stickyEffects)
                            {
                                ICMessageSettingsControl.ResetMessageEffects();
                            }
                        });

                        // Unsubscribe from the event
                        client.OnICMessageReceived -= OnICMessageReceivedHandler;
                    }
                }

                client.OnICMessageReceived -= OnICMessageReceivedHandler;
                // Subscribe to the event
                client.OnICMessageReceived += OnICMessageReceivedHandler;
                await client.SendICMessage(sendMessage);
            };

            ICMessageSettingsControl.OnResetMessageEffects += () =>
            {
                HoldIt.IsChecked = false;
                Objection.IsChecked = false;
                TakeThat.IsChecked = false;
                Custom.IsChecked = false;
            };

            OOCLogControl.txtOOCShowname.Text = SaveFile.Data.OOCName;
            chkPosOnIniSwap.IsChecked = SaveFile.Data.SwitchPosOnIniSwap;
            chkSticky.IsChecked = SaveFile.Data.StickyEffect;
            chkInvertLog.IsChecked = SaveFile.Data.InvertICLog;

            btnDebug.Visibility = debug ? Visibility.Visible : Visibility.Collapsed;
        }
        private void RenameClient(AOClient bot)
        {
            // Show an input dialog to the user
            string newClientName = InputDialog.Show("Enter new Client name:", "New Client Name", bot.clientName); ;

            if (!string.IsNullOrWhiteSpace(newClientName))
            {
                bot.clientName = newClientName;
                UpdateClientTooltip(bot);

                if (bot == currentClient)
                {
                    OOCLogControl.UpdateStreamLabel(bot);
                }
            }
        }
        private void UpdateClientTooltip(AOClient bot)
        {
            clients.Where(x => x.Value == bot).FirstOrDefault().Key.ToolTip = $"[{bot.playerID}] {bot.iniPuppetName} (\"{bot.clientName}\")";
        }
        private void AddClient(string clientName)
        {
            AddClientAsync(clientName);
        }
        private async Task AddClientAsync(string clientName)
        {
            IsEnabled = false;  
            await WaitForm.ShowFormAsync("Connecting client...", this);

            try
            {
                AOClient bot = new AOClient(Globals.IPs[Globals.Servers.ChillAndDices], Globals.ConnectionString);
                bot.clientName = clientName;

                bot.OnICMessageReceived += (ICMessage icMessage) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        bool isSentFromSelf = clients.Select(x => x.Value.iniPuppetID).Contains(icMessage.CharId);

                        ICLogControl.AddMessage(bot, icMessage.ShowName,
                            icMessage.Message,
                            isSentFromSelf, icMessage.TextColor);
                    });
                };

                bot.OnOOCMessageReceived += (string showName, string message, bool isFromServer) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        OOCLogControl.AddMessage(bot, showName, message, isFromServer);
                    });
                };

                await bot.Connect();

                bot.SetICShowname(clientName);
                bot.OOCShowname = clientName;
                bot.switchPosWhenChangingINI = chkPosOnIniSwap.IsChecked.Value;

                ToggleButton toggleBtn = new ToggleButton
                {
                    Width = 40,
                    Height = 40
                };
                
                toggleBtn.Checked += ClientToggleButton_Checked;
                toggleBtn.Unchecked += ClientToggleButton_Unchecked;

                #region Create Context Menu
                ContextMenu contextMenu = new ContextMenu();
                MenuItem renameMenuItem = new MenuItem { Header = "Rename Client" };
                renameMenuItem.Click += (sender, args) => RenameClient(bot);
                contextMenu.Items.Add(renameMenuItem);

                MenuItem iniPuppetChange = new MenuItem { Header = "Select INIPuppet (Automatic)" };
                iniPuppetChange.Click += async (sender, args) => await bot.SelectFirstAvailableINIPuppet(false);
                contextMenu.Items.Add(iniPuppetChange);

                MenuItem manualIniPuppetChange = new MenuItem { Header = "Select INIPuppet (Manual)" };
                manualIniPuppetChange.Click += async (sender, args) =>
                {
                    // Show an input dialog to the user
                    string newClientName = ShowInputDialog("Enter INIPuppet name:");

                    if (!string.IsNullOrWhiteSpace(newClientName))
                    {
                        try
                        {
                            await bot.SelectIniPuppet(newClientName, false);
                        }
                        catch(Exception e)
                        {
                            OceanyaMessageBox.Show(e.Message, "INIPuppet Selection Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                };
                contextMenu.Items.Add(manualIniPuppetChange);

                MenuItem reconnectMenuItem = new MenuItem { Header = "Reconnect" };
                reconnectMenuItem.Click += async (sender, args) => await bot.DisconnectWebsocket();
                contextMenu.Items.Add(reconnectMenuItem);


                toggleBtn.ContextMenu = contextMenu;
                #endregion
                // Subscribe to OnChangedCharacter event
                bot.OnChangedCharacter += (CharacterFolder newCharacter) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Path to the normal and selected images
                        string normalImagePath = newCharacter.CharIconPath;
                        string selectedImagePath = normalImagePath; // Since you want the same image darkened

                        if (System.IO.File.Exists(normalImagePath))
                        {
                            // Create the ControlTemplate dynamically
                            ControlTemplate template = new ControlTemplate(typeof(ToggleButton));
                            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
                            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(Image));
                            imageFactory.Name = "ButtonImage";
                            imageFactory.SetValue(Image.WidthProperty, 40.0);
                            imageFactory.SetValue(Image.HeightProperty, 40.0);
                            imageFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri(normalImagePath, UriKind.Absolute)));

                            gridFactory.AppendChild(imageFactory);
                            template.VisualTree = gridFactory;

                            // Add the trigger for toggled state (darken image)
                            Trigger trigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                            trigger.Setters.Add(new Setter
                            {
                                Property = Image.SourceProperty,
                                TargetName = "ButtonImage",
                                Value = new BitmapImage(new Uri(selectedImagePath, UriKind.Absolute))
                            });
                            trigger.Setters.Add(new Setter
                            {
                                Property = Image.OpacityProperty,
                                TargetName = "ButtonImage",
                                Value = 0.6 // Darkening effect
                            });

                            template.Triggers.Add(trigger);
                            toggleBtn.Template = template;
                        }
                        else
                        {
                            // No image exists, create a gray image programmatically with text
                            ControlTemplate template = new ControlTemplate(typeof(ToggleButton));
                            FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
                            FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(Image));
                            imageFactory.Name = "ButtonImage";
                            imageFactory.SetValue(Image.WidthProperty, 40.0);
                            imageFactory.SetValue(Image.HeightProperty, 40.0);

                            // Create a gray bitmap
                            int width = 40;
                            int height = 40;
                            WriteableBitmap grayBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                            byte[] pixels = new byte[width * height * 4];
                            for (int i = 0; i < pixels.Length; i += 4)
                            {
                                pixels[i] = 225;     // Blue
                                pixels[i + 1] = 225; // Green
                                pixels[i + 2] = 225; // Red
                                pixels[i + 3] = 255; // Alpha
                            }
                            grayBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, width * 4, 0);

                            imageFactory.SetValue(Image.SourceProperty, grayBitmap);

                            FrameworkElementFactory textFactory = new FrameworkElementFactory(typeof(TextBlock));
                            textFactory.SetValue(TextBlock.TextProperty, newCharacter.Name);
                            textFactory.SetValue(TextBlock.ForegroundProperty, Brushes.Black);
                            textFactory.SetValue(TextBlock.FontFamilyProperty, new FontFamily("Arial"));
                            textFactory.SetValue(TextBlock.FontSizeProperty, 10.0);
                            textFactory.SetValue(TextBlock.TextWrappingProperty, TextWrapping.Wrap);
                            textFactory.SetValue(TextBlock.TextAlignmentProperty, TextAlignment.Center);
                            textFactory.SetValue(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                            textFactory.SetValue(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center);

                            gridFactory.AppendChild(imageFactory);
                            gridFactory.AppendChild(textFactory);
                            template.VisualTree = gridFactory;

                            // Add the trigger for toggled state (darken image)
                            Trigger trigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                            trigger.Setters.Add(new Setter
                            {
                                Property = Image.OpacityProperty,
                                TargetName = "ButtonImage",
                                Value = 0.6 // Darkening effect
                            });

                            template.Triggers.Add(trigger);
                            toggleBtn.Template = template;
                        }
                    });
                };
                bot.OnWebsocketDisconnect += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ICLogControl.AddMessage(bot, "Oceanya Client", "Websocket Disconnected.", true, ICMessage.TextColors.Red);
                        OOCLogControl.AddMessage(bot, "Oceanya Client", "Websocket Disconnected.", true);
                    });
                };
                bot.OnReconnectionAttempt += (int attemptCount) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        string message = $"Reconnecting...{(attemptCount != 1 ? $" (Attempt {attemptCount})":"")}";
                        ICLogControl.AddMessage(bot, "Oceanya Client", message, true, ICMessage.TextColors.Yellow);
                        OOCLogControl.AddMessage(bot, "Oceanya Client", message, true);
                    });
                };
                bot.OnReconnectionAttemptFailed += (int attemptCount) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        string message = $"Attempt {attemptCount} failed.";
                        ICLogControl.AddMessage(bot, "Oceanya Client", message, true, ICMessage.TextColors.Yellow);
                        OOCLogControl.AddMessage(bot, "Oceanya Client", message, true);
                    });
                };
                bot.OnReconnect += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        UpdateClientTooltip(bot);
                        ICLogControl.AddMessage(bot, "Oceanya Client", "Reconnected to server.", true, ICMessage.TextColors.Green);
                        OOCLogControl.AddMessage(bot, "Oceanya Client", "Reconnected to server.", true);
                    });
                };
                bot.OnDisconnect += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        var button = clients.FirstOrDefault(x => x.Value == bot).Key;

                        clients.Remove(button);
                        EmoteGrid.DeleteElement(button);

                        OceanyaMessageBox.Show($"Client {bot.clientName} has disconnected.", "Client Disconnected", MessageBoxButton.OK, MessageBoxImage.Information);

                        if(clients.Count == 0)
                        {
                            //Clear the form entirely.
                            ICMessageSettingsControl.ClearSettings();
                            OOCLogControl.ClearAllLogs();
                            ICLogControl.ClearAllLogs();
                            OOCLogControl.IsEnabled = false;
                            ICLogControl.IsEnabled = false;
                            ICMessageSettingsControl.IsEnabled = false;
                            OOCLogControl.UpdateStreamLabel(null);
                        }
                        else
                        {
                            var newClient = clients.Values.FirstOrDefault();
                            if (newClient != null)
                            {
                                SelectClient(newClient);
                            }
                        }
                            
                    });
                };
                bot.OnINIPuppetChange += () =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if(bot == currentClient)
                        {
                            OOCLogControl.UpdateStreamLabel(bot);
                        }
                        UpdateClientTooltip(bot);
                    });
                };
                bot.SetCharacter(bot.currentINI);

                toggleBtn.Focusable = false;
                toggleBtn.IsTabStop = false;

                clients.Add(toggleBtn, bot);

                EmoteGrid.AddElement(toggleBtn);

                toggleBtn.IsChecked = true;
                UpdateClientTooltip(bot);

                if (clients.Count == 1)
                {
                    OOCLogControl.IsEnabled = true;
                    ICLogControl.IsEnabled = true;
                    ICMessageSettingsControl.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                WaitForm.CloseForm();
                OceanyaMessageBox.Show($"Error connecting client: {ex.Message}", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                WaitForm.CloseForm();
            }

            IsEnabled = true;
        }

        private void SelectClient(AOClient client)
        {
            foreach (var button in clients.Keys)
            {
                if (clients[button] == client)
                {
                    button.IsChecked = true;
                    EmoteGrid.SetPageToElement(button);
                    break;
                }
            }

            currentClient = client;
            ICMessageSettingsControl.SetClient(currentClient);
            OOCLogControl.SetCurrentClient(currentClient);
            ICLogControl.SetCurrentClient(currentClient);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            #region Create the bot and connect to the server
            AOClient bot = new AOClient(Globals.IPs[Globals.Servers.ChillAndDices], "Basement/testing");
            await bot.Connect();
            #endregion

            #region Start the GPT Client
            string? apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new Exception("OpenAI API key is not set in the environment variables.");
            }
            GPTClient gptClient = new GPTClient(apiKey);
            gptClient.SetSystemInstructions(new List<string> { Globals.AI_SYSTEM_PROMPT });
            gptClient.systemVariables = new Dictionary<string, string>()
            {
                { "[[[current_character]]]", bot.currentINI.Name },
                { "[[[current_emote]]]", bot.currentEmote.DisplayID }
            };
            #endregion

            ChatLogManager chatLog = new ChatLogManager(MaxChatHistory: 20);

            bot.OnMessageReceived += async (string chatLogType, string characterName, string showName, string message, int iniPuppetID) =>
            {
                chatLog.AddMessage(chatLogType, characterName, showName, message);

                if (!Globals.UseOpenAIAPI) return;

                switch (chatLogType)
                {
                    case "IC":
                        if (showName == bot.ICShowname && characterName == bot.currentINI.Name && iniPuppetID == bot.iniPuppetID)
                        {
                            return;
                        }
                        break;
                    case "OOC":
                        if (showName == bot.ICShowname)
                        {
                            return;
                        }
                        break;
                }


                int maxRetries = 3; // Prevent infinite loops
                int attempt = 0;
                bool success = false;

                while (attempt < maxRetries && !success)
                {
                    attempt++;
                    if (Globals.DebugMode) CustomConsole.Debug($"Prompting AI..." + (attempt > 0 ? " (Attempt {attempt})" : ""));
                    string response = await gptClient.GetResponseAsync(chatLog.GetFormattedChatHistory());
                    if (Globals.DebugMode) CustomConsole.Debug("Received AI response: " + response);

                    success = await ValidateJsonResponse(bot, response);
                }

                if (!success)
                {
                    CustomConsole.Error("ERROR: AI failed to return a valid response after multiple attempts.");
                }
            };

            await Task.Delay(-1);
        }

        private static async Task<bool> ValidateJsonResponse(AOClient bot, string response)
        {
            bool success = false;

            // Ensure response is not empty and not SYSTEM_WAIT()
            if (string.IsNullOrWhiteSpace(response) || response == "SYSTEM_WAIT()")
                return success;

            try
            {
                var responseJson = JsonSerializer.Deserialize<Dictionary<string, object>>(response);
                if (responseJson == null)
                {
                    CustomConsole.Error("ERROR: AI response is not valid JSON. Retrying...");
                    return success;
                }

                // Validate required fields
                if (!responseJson.ContainsKey("message") || !responseJson.ContainsKey("chatlog") ||
                    !responseJson.ContainsKey("showname") || !responseJson.ContainsKey("current_character") ||
                    !responseJson.ContainsKey("modifiers"))
                {
                    CustomConsole.Error("ERROR: AI response is missing required fields. Retrying...");
                    return success;
                }

                string botMessage = responseJson["message"].ToString();
                string chatlogType = responseJson["chatlog"].ToString();
                string newShowname = responseJson["showname"].ToString();
                string newCharacter = responseJson["current_character"].ToString();

                // Ensure chatlogType is either "IC" or "OOC"
                if (chatlogType != "IC" && chatlogType != "OOC")
                {
                    CustomConsole.Error($"ERROR: Invalid chatlog type '{chatlogType}', retrying...");
                    return success;
                }

                // Apply showname change if valid
                if (!string.IsNullOrEmpty(newShowname))
                {
                    bot.SetICShowname(newShowname);
                }

                // Apply character switch if valid
                if (!string.IsNullOrEmpty(newCharacter))
                {
                    bot.SetCharacter(newCharacter);
                }

                // Apply modifiers with strict validation
                if (responseJson["modifiers"] is JsonElement modifiersElement)
                {
                    var modifiers = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(modifiersElement.GetRawText());

                    if (modifiers != null)
                    {
                        if (modifiers.ContainsKey("deskMod") && modifiers["deskMod"].TryGetInt32(out int deskModValue))
                            bot.deskMod = (ICMessage.DeskMods)deskModValue;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid deskMod value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("emoteMod") && modifiers["emoteMod"].TryGetInt32(out int emoteModValue))
                            bot.emoteMod = (ICMessage.EmoteModifiers)emoteModValue;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid emoteMod value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("shoutModifiers") && modifiers["shoutModifiers"].TryGetInt32(out int shoutModValue))
                            bot.shoutModifiers = (ICMessage.ShoutModifiers)shoutModValue;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid shoutModifiers value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("flip") && modifiers["flip"].TryGetInt32(out int flipValue))
                            bot.flip = flipValue == 1;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid flip value. Retrying...");
                            return success;
                        }

                        //if (modifiers.ContainsKey("realization") && modifiers["realization"].TryGetInt32(out int realizationValue))
                        //    bot.effect = realizationValue == 1;
                        //else
                        //{
                        //    CustomConsole.WriteLine("ERROR: Invalid realization value. Retrying...");
                        //    return success;
                        //}

                        if (modifiers.ContainsKey("textColor") && modifiers["textColor"].TryGetInt32(out int textColorValue))
                            bot.textColor = (ICMessage.TextColors)textColorValue;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid textColor value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("immediate") && modifiers["immediate"].TryGetInt32(out int immediateValue))
                            bot.Immediate = immediateValue == 1;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid immediate value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("additive") && modifiers["additive"].TryGetInt32(out int additiveValue))
                            bot.Additive = additiveValue == 1;
                        else
                        {
                            CustomConsole.Error("ERROR: Invalid additive value. Retrying...");
                            return success;
                        }
                    }
                    else
                    {
                        CustomConsole.Error("ERROR: AI response modifiers section is invalid. Retrying...");
                        return success;
                    }
                }

                // Send response based on chatlog type
                if (!string.IsNullOrEmpty(botMessage))
                {
                    switch (chatlogType)
                    {
                        case "IC":
                            await bot.SendICMessage(botMessage);
                            break;
                        case "OOC":
                            await bot.SendOOCMessage(bot.ICShowname, botMessage);
                            break;
                    }
                }
                else
                {
                    CustomConsole.Error("ERROR: AI response message is empty. Retrying...");
                    return success;
                }

                success = true; // If we reach here, response was valid and handled successfully
            }
            catch (Exception ex)
            {
                CustomConsole.Error($"ERROR: Exception while processing AI response - {ex.Message}. Retrying...");
            }

            return success;
        }

        private void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            // Show an input dialog to the user
            string newClientName = ShowInputDialog("Enter client name:");

            if (!string.IsNullOrWhiteSpace(newClientName))
            {
                AddClient(newClientName);
            }
        }

        private string ShowInputDialog(string prompt)
        {
            return InputDialog.Show(prompt, "Client Name");
        }


        private async void btnRemoveClient_Click(object sender, RoutedEventArgs e)
        {
            var clientToRemove = currentClient;
            if (clientToRemove == null) return;

            await clientToRemove.Disconnect();
        }

        private void ClientToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            foreach (var button in clients.Keys)
            {
                if (button != clickedButton)
                {
                    button.Unchecked -= ClientToggleButton_Unchecked;
                    button.IsChecked = false;
                    button.Unchecked += ClientToggleButton_Unchecked;
                }
            }

            SelectClient(clients[clickedButton]);
        }
        private void ClientToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            if (clickedButton.IsChecked == false)
            {
                clickedButton.IsChecked = true;
            }
        }
        private void ToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            foreach (var button in objectionModifiers)
            {
                if (button != clickedButton)
                {
                    button.IsChecked = false; // Uncheck other buttons
                }
            }
        }

        private void chkStickyEffects_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ICMessageSettingsControl.stickyEffects = checkBox.IsChecked.Value;

                SaveFile.Data.StickyEffect = checkBox.IsChecked.Value;
                SaveFile.Save();
            }
        }
        private void chkPosOnIniSwap_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                foreach (var client in clients.Values)
                {
                    client.switchPosWhenChangingINI = checkBox.IsChecked.Value;
                }

                SaveFile.Data.SwitchPosOnIniSwap = checkBox.IsChecked.Value;
                SaveFile.Save();
            }
        }
        

        private async void btnRefreshCharacters_Click(object sender, RoutedEventArgs e)
        {
            var result = OceanyaMessageBox.Show("Are you sure you want to refresh your client assets? (This process may take a while)", "Refresh all Assets", MessageBoxButton.YesNo, MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;
            
            await WaitForm.ShowFormAsync("Refreshing character and background info...", this);

            Globals.UpdateConfigINI(Globals.PathToConfigINI);
            CharacterFolder.RefreshCharacterList
                (
                    onParsedCharacter:
                    (ini) =>
                    {
                        WaitForm.SetSubtitle("Parsed Character: " + ini.Name);
                    },
                    onChangedMountPath:
                    (path) =>
                    {
                        WaitForm.SetSubtitle("Changed mount path: " + path);
                    }
                );
            WaitForm.CloseForm();
            ICMessageSettingsControl.ReinitializeSettings();
            if (currentClient == null)
            {
                return;
            }
            SelectClient(currentClient);
        }

        bool temp = false;
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in clients.Values)
            {
                await item.DisconnectWebsocket();
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in clients.Values)
            {
                await item.Disconnect();
            }

            var config = new InitialConfigurationWindow();
            config.Activate();
            config.Show();

            this.Close();
        }

        private void chkInvertLog_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                ICLogControl.SetInvertOnClientLogs(checkBox.IsChecked.Value);
                SaveFile.Data.InvertICLog = checkBox.IsChecked.Value;
                SaveFile.Save();
            }
        }

        private void THEDINGBUTTON_Click(object sender, RoutedEventArgs e)
        {
            AudioPlayer.PlayEmbeddedSound("Resources/BellDing.mp3", 0.25f);
        }

        private bool _altGrActive = false;

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            // When RightAlt is pressed, mark AltGr as active.
            if (e.Key == Key.RightAlt)
            {
                _altGrActive = true;
            }

            // Determine if AltGr is active either because our flag is set
            // or because the RightAlt key is physically down.
            bool isAltGrActive = _altGrActive || ((Keyboard.GetKeyStates(Key.RightAlt) & KeyStates.Down) == KeyStates.Down);

            // If the Control modifier is pressed but AltGr is active, skip processing.
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && !isAltGrActive)
            {
                int index = -1;

                // Check if the pressed key is a digit on the main keyboard.
                if (e.Key >= Key.D1 && e.Key <= Key.D9)
                {
                    index = e.Key - Key.D1;
                }
                else if (e.Key == Key.D0)
                {
                    index = 9;
                }
                // Or a digit on the numpad.
                else if (e.Key >= Key.NumPad1 && e.Key <= Key.NumPad9)
                {
                    index = e.Key - Key.NumPad1;
                }
                else if (e.Key == Key.NumPad0)
                {
                    index = 9;
                }

                // If a valid digit was pressed and a corresponding client exists, process the selection.
                if (index >= 0 && index < clients.Count)
                {
                    AOClient client = clients.Values.ElementAt(index);
                    SelectClient(client);
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.Tab)
            {
                if (Keyboard.FocusedElement is TextBox focusedTextBox)
                {
                    switch (focusedTextBox.Name)
                    {
                        case "txtICMessage":
                            OOCLogControl.txtOOCMessage.Focus();
                            e.Handled = true;
                            break;
                        case "txtOOCMessage":
                            ICMessageSettingsControl.txtICMessage.Focus();
                            e.Handled = true; // Prevent default tab behavior
                            break;
                        case "txtICShowname":
                            OOCLogControl.txtOOCShowname.Focus();
                            e.Handled = true; // Prevent default tab behavior
                            break;
                        case "txtOOCShowname":
                            ICMessageSettingsControl.txtICShowname.Focus();
                            e.Handled = true; // Prevent default tab behavior
                            break;
                    }
                }
            }

            base.OnPreviewKeyDown(e);
        }

        protected override void OnPreviewKeyUp(KeyEventArgs e)
        {
            // When the RightAlt key is released, clear the AltGr flag.
            if (e.Key == Key.RightAlt)
            {
                _altGrActive = false;
            }
            base.OnPreviewKeyUp(e);
        }
    }
}
