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
            clients.Where(x => x.Value == bot).FirstOrDefault().Key.ToolTip = $"\"{bot.clientName}\" (AO ID: {bot.playerID})";
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
                if (clients.Count == 0)
                {
                    OOCLogControl.IsEnabled = true;
                    ICLogControl.IsEnabled = true;
                    ICMessageSettingsControl.IsEnabled = true;
                }

                bot.OnICMessageReceived += (ICMessage icMessage) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ICLogControl.AddMessage(bot, icMessage.ShowName,
                            icMessage.Message,
                            icMessage.CharId == bot.selectedCharacterIndex, icMessage.TextColor);
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
                toggleBtn.ContextMenu = contextMenu;
                #endregion
                // Subscribe to OnChangedCharacter event
                bot.OnChangedCharacter += (CharacterINI newCharacter) =>
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
                bot.SetCharacter(bot.currentINI);

                toggleBtn.Focusable = false;
                toggleBtn.IsTabStop = false;

                clients.Add(toggleBtn, bot);

                EmoteGrid.AddElement(toggleBtn);

                toggleBtn.IsChecked = true;
                UpdateClientTooltip(bot);
            }
            catch (Exception ex)
            {
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
                        if (showName == bot.ICShowname && characterName == bot.currentINI.Name && iniPuppetID == bot.selectedCharacterIndex)
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
                    if (Globals.DebugMode) CustomConsole.WriteLine($"Prompting AI..." + (attempt > 0 ? " (Attempt {attempt})" : ""));
                    string response = await gptClient.GetResponseAsync(chatLog.GetFormattedChatHistory());
                    if (Globals.DebugMode) CustomConsole.WriteLine("Received AI response: " + response);

                    success = await ValidateJsonResponse(bot, response);
                }

                if (!success)
                {
                    CustomConsole.WriteLine("ERROR: AI failed to return a valid response after multiple attempts.");
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
                    CustomConsole.WriteLine("ERROR: AI response is not valid JSON. Retrying...");
                    return success;
                }

                // Validate required fields
                if (!responseJson.ContainsKey("message") || !responseJson.ContainsKey("chatlog") ||
                    !responseJson.ContainsKey("showname") || !responseJson.ContainsKey("current_character") ||
                    !responseJson.ContainsKey("modifiers"))
                {
                    CustomConsole.WriteLine("ERROR: AI response is missing required fields. Retrying...");
                    return success;
                }

                string botMessage = responseJson["message"].ToString();
                string chatlogType = responseJson["chatlog"].ToString();
                string newShowname = responseJson["showname"].ToString();
                string newCharacter = responseJson["current_character"].ToString();

                // Ensure chatlogType is either "IC" or "OOC"
                if (chatlogType != "IC" && chatlogType != "OOC")
                {
                    CustomConsole.WriteLine($"ERROR: Invalid chatlog type '{chatlogType}', retrying...");
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
                            CustomConsole.WriteLine("ERROR: Invalid deskMod value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("emoteMod") && modifiers["emoteMod"].TryGetInt32(out int emoteModValue))
                            bot.emoteMod = (ICMessage.EmoteModifiers)emoteModValue;
                        else
                        {
                            CustomConsole.WriteLine("ERROR: Invalid emoteMod value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("shoutModifiers") && modifiers["shoutModifiers"].TryGetInt32(out int shoutModValue))
                            bot.shoutModifiers = (ICMessage.ShoutModifiers)shoutModValue;
                        else
                        {
                            CustomConsole.WriteLine("ERROR: Invalid shoutModifiers value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("flip") && modifiers["flip"].TryGetInt32(out int flipValue))
                            bot.flip = flipValue == 1;
                        else
                        {
                            CustomConsole.WriteLine("ERROR: Invalid flip value. Retrying...");
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
                            CustomConsole.WriteLine("ERROR: Invalid textColor value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("immediate") && modifiers["immediate"].TryGetInt32(out int immediateValue))
                            bot.Immediate = immediateValue == 1;
                        else
                        {
                            CustomConsole.WriteLine("ERROR: Invalid immediate value. Retrying...");
                            return success;
                        }

                        if (modifiers.ContainsKey("additive") && modifiers["additive"].TryGetInt32(out int additiveValue))
                            bot.Additive = additiveValue == 1;
                        else
                        {
                            CustomConsole.WriteLine("ERROR: Invalid additive value. Retrying...");
                            return success;
                        }
                    }
                    else
                    {
                        CustomConsole.WriteLine("ERROR: AI response modifiers section is invalid. Retrying...");
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
                    CustomConsole.WriteLine("ERROR: AI response message is empty. Retrying...");
                    return success;
                }

                success = true; // If we reach here, response was valid and handled successfully
            }
            catch (Exception ex)
            {
                CustomConsole.WriteLine($"ERROR: Exception while processing AI response - {ex.Message}. Retrying...");
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
            Globals.BaseFolders = Globals.GetBaseFolders(Globals.PathToConfigINI);
            await WaitForm.ShowFormAsync("Refreshing character and background info...", this);
            CharacterINI.RefreshCharacterList
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
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OOCLogControl.ScrollToBottom();
            for (int i = 0; i < 50; i++)
            {
                ICLogControl.AddMessage(currentClient, "test", "test");
            }
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
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
    }
}
