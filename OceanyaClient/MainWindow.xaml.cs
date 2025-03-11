using System;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using Common;
using OceanyaClient.Components;
using ToggleButton = System.Windows.Controls.Primitives.ToggleButton;

namespace OceanyaClient
{
    public partial class MainWindow : Window
    {
        Dictionary<ToggleButton, AOBot> clients = new Dictionary<ToggleButton, AOBot>();
        AOBot currentClient;


        List<ToggleButton> objectionModifiers;
        public MainWindow()
        {
            InitializeComponent();
            objectionModifiers = new List<ToggleButton> { HoldIt, Objection, TakeThat, Custom };
            // Set grid mode and size
            EmoteGrid.SetScrollMode(PageButtonGrid.ScrollMode.Vertical);
            EmoteGrid.SetPageSize(4, 1);
            OOCLogControl.IsEnabled = false;
            ICLogControl.IsEnabled = false;
            ICMessageSettingsControl.IsEnabled = false;

            OOCLogControl.OnSendOOCMessage += async (showName, message) =>
            {
                await currentClient.SendOOCMessage(showName, message);
            };

            ICMessageSettingsControl.OnSendICMessage += async (message) =>
            {
                // Split the message to get the client name and the actual message
                var splitMessage = message.Split(new[] { ':' }, 2);
                AOBot client = null;
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

                
                await client.SendICMessage(sendMessage.Trim());
            };

            OpenConfigurationWindow();

            //ICLogControl.AddMessage("TestingShowname", "a", true);
            //OOCLogControl.AddMessage("TestingShowname", "a", true);
            //for (int i = 0; i < 5; i++)
            //{
            //    ICLogControl.AddMessage("TestingShowname", "TestingMessage"+i);
            //    OOCLogControl.AddMessage("TestingShowname", "TestingMessage" + i);
            //}


        }

        private void OpenConfigurationWindow()
        {
            Window configWindow = new Window
            {
                Width = 450,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Configuration",
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };

            // Attorney_Online.exe path
            TextBlock exePathLabel = new TextBlock { Text = "Attorney_Online.exe Path:", Margin = new Thickness(0, 0, 0, 5) };
            TextBox exePathTextBox = new TextBox { MinWidth = 300 };
            Button browseButton = new Button { Content = "Browse", Width = 75, Margin = new Thickness(5, 0, 0, 0) };
            browseButton.Click += (s, e) =>
            {
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Executable files (*.exe)|*.exe",
                    Title = "Select Attorney_Online.exe"
                };
                if (openFileDialog.ShowDialog() == true)
                {
                    exePathTextBox.Text = openFileDialog.FileName;
                }
            };

            StackPanel exePathPanel = new StackPanel { Orientation = Orientation.Horizontal };
            exePathPanel.Children.Add(exePathTextBox);
            exePathPanel.Children.Add(browseButton);

            // Connection path
            TextBlock connectionPathLabel = new TextBlock { Text = "Connection Path (e.g., Basement/testing):", Margin = new Thickness(0, 10, 0, 5) };
            TextBox connectionPathTextBox = new TextBox { MinWidth = 300 };

            // Refresh character and background info checkbox
            CheckBox refreshInfoCheckBox = new CheckBox { Content = "Refresh character and background info", Margin = new Thickness(0, 10, 0, 0) };

            // Load saved configuration
            LoadConfiguration(exePathTextBox, connectionPathTextBox);

            // OK button
            Button okButton = new Button { Content = "OK", Width = 75, Margin = new Thickness(0, 10, 0, 0), IsDefault = true };
            okButton.Click += (s, e) =>
            {
                // Save the configuration
                string exePath = exePathTextBox.Text;
                string connectionPath = connectionPathTextBox.Text;

                // Validate inputs
                if (string.IsNullOrWhiteSpace(exePath) || string.IsNullOrWhiteSpace(connectionPath))
                {
                    MessageBox.Show("Please provide both the Attorney_Online.exe path and the connection path.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Save to settings or use as needed
                Globals.BaseFolder = Path.Combine(Path.GetDirectoryName(exePath), "base");
                Globals.ConnectionString = connectionPath;

                // Save configuration to file
                SaveConfiguration(exePath, connectionPath);

                // Refresh character and background info if checkbox is checked
                if (refreshInfoCheckBox.IsChecked == true)
                {
                    WaitForm.ShowForm("Refreshing character and background info...", configWindow);
                    CharacterINI.RefreshCharacterList();
                    WaitForm.CloseForm();
                }

                configWindow.Close();
            };

            panel.Children.Add(exePathLabel);
            panel.Children.Add(exePathPanel);
            panel.Children.Add(connectionPathLabel);
            panel.Children.Add(connectionPathTextBox);
            panel.Children.Add(refreshInfoCheckBox);
            panel.Children.Add(okButton);

            configWindow.Content = panel;
            configWindow.ShowDialog();
        }

        private void SaveConfiguration(string exePath, string connectionPath)
        {
            var config = new { ExePath = exePath, ConnectionPath = connectionPath };
            string json = JsonSerializer.Serialize(config);
            string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OceanyaClient", "config.json");

            Directory.CreateDirectory(Path.GetDirectoryName(configFilePath));
            File.WriteAllText(configFilePath, json);
        }

        private void LoadConfiguration(TextBox exePathTextBox, TextBox connectionPathTextBox)
        {
            string configFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OceanyaClient", "config.json");

            if (File.Exists(configFilePath))
            {
                string json = File.ReadAllText(configFilePath);
                var config = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                if (config != null)
                {
                    exePathTextBox.Text = config["ExePath"];
                    connectionPathTextBox.Text = config["ConnectionPath"];
                }
            }
        }

        private void AddClient(string clientName)
        {
            AddClientAsync(clientName);
        }
        private async Task AddClientAsync(string clientName)
        {
            IsEnabled = false;
            WaitForm.ShowForm("Connecting client...", this);

            try
            {
                AOBot bot = new AOBot(Globals.IPs[Globals.Servers.ChillAndDices], Globals.ConnectionString);
                bot.clientName = clientName;
                if (clients.Count == 0)
                {
                    OOCLogControl.IsEnabled = true;
                    ICLogControl.IsEnabled = true;
                    ICMessageSettingsControl.IsEnabled = true;

                    bot.OnICMessageReceived += (ICMessage icMessage) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            ICLogControl.AddMessage(icMessage.ShowName,
                                icMessage.Message,
                                icMessage.CharId == bot.selectedCharacterIndex);
                        });
                    };

                    bot.OnOOCMessageReceived += (string showName, string message, bool isFromServer) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            OOCLogControl.AddMessage(showName, message, isFromServer);
                        });
                    };
                }

                await bot.Connect();

                bot.SetICShowname(clientName);
                bot.OOCShowname = clientName;

                ToggleButton toggleBtn = new ToggleButton
                {
                    Width = 40,
                    Height = 40,
                    ToolTip = $"\"{clientName}\" (AO ID: {bot.playerID})"
                };

                toggleBtn.Checked += ClientToggleButton_Checked;
                toggleBtn.Unchecked += ClientToggleButton_Unchecked;

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

                bot.SetCharacter(bot.currentINI);

                clients.Add(toggleBtn, bot);

                EmoteGrid.AddElement(toggleBtn);

                toggleBtn.IsChecked = true;

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error connecting client: {ex.Message}", "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                WaitForm.CloseForm();
            }

            IsEnabled = true;
        }

        private void SelectClient(AOBot client)
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
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            #region Create the bot and connect to the server
            AOBot bot = new AOBot(Globals.IPs[Globals.Servers.ChillAndDices], "Basement/testing");
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

        private static async Task<bool> ValidateJsonResponse(AOBot bot, string response)
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

        private  void btnAddClient_Click(object sender, RoutedEventArgs e)
        {
            // Show an input dialog to the user
            string newClientName = ShowInputDialog("Enter client name:");

            if (!string.IsNullOrWhiteSpace(newClientName))
            {
                AddClient(newClientName);
            }
        }

        // Helper method to show an input dialog
        private string ShowInputDialog(string prompt)
        {
            Window inputDialog = new Window
            {
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Title = "Client Name",
                ResizeMode = ResizeMode.NoResize
            };

            StackPanel panel = new StackPanel { Margin = new Thickness(10) };
            TextBlock textBlock = new TextBlock { Text = prompt, Margin = new Thickness(0, 0, 0, 10) };
            TextBox textBox = new TextBox { MinWidth = 200 };
            Button okButton = new Button { Content = "OK", Width = 60, Margin = new Thickness(10, 5, 0, 0), IsDefault = true };

            okButton.Click += (s, e) => { inputDialog.DialogResult = true; inputDialog.Close(); };

            panel.Children.Add(textBlock);
            panel.Children.Add(textBox);
            panel.Children.Add(okButton);
            inputDialog.Content = panel;

            return inputDialog.ShowDialog() == true ? textBox.Text : string.Empty;
        }


        private async void btnRemoveClient_Click(object sender, RoutedEventArgs e)
        {
            var clientToRemove = currentClient;
            if (clientToRemove == null) return;

            var button = clients.FirstOrDefault(x => x.Value == clientToRemove).Key;

            await clientToRemove.Disconnect();

            clients.Remove(button);
            EmoteGrid.DeleteElement(button);

            var newClient = clients.Values.FirstOrDefault();
            if (newClient != null)
            {
                SelectClient(newClient);
            }
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
    }
}
