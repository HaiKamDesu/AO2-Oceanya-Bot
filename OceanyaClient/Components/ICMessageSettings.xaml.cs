using AOBot_Testing.Agents;
using AOBot_Testing.Structures;
using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using Path = System.IO.Path;
using System.ComponentModel;
using static OceanyaClient.Components.ImageComboBox;
using System.Xml.Linq;

namespace OceanyaClient.Components
{
    /// <summary>
    /// Interaction logic for ICMessageSettings.xaml
    /// </summary>
    public partial class ICMessageSettings : UserControl
    {
        public static int ICShownameMaxLength = 22;
        public static int ICMessageMaxLength = 256;
        Dictionary<Emote, ToggleButton> emotes = new();
        AOClient curClient;
        public bool stickyEffects;

        public Action<string> OnSendICMessage;

        public ICMessageSettings()
        {
            InitializeComponent();

            #region Emote Grid
            EmoteGrid.SetScrollMode(PageButtonGrid.ScrollMode.Horizontal);
            EmoteGrid.SetPageSize(2, 10);
            #endregion

            #region Char dropdown
            foreach (var ini in CharacterINI.FullList)
            {
                CharacterDropdown.Add(ini.Name, ini.CharIconPath);
            }
            CharacterDropdown.OnConfirm += CharacterDropdown_OnConfirm;
            #endregion

            EmoteDropdown.OnConfirm += EmoteDropdown_OnConfirm;
            EmoteDropdown.SetComboBoxReadOnly(true);

            PositionDropdown.OnConfirm += PositionDropdown_OnConfirm;

            foreach (var color in Enum.GetValues(typeof(ICMessage.TextColors)).Cast<ICMessage.TextColors>())
            {
                var colorsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "Colors");
                if (!Directory.Exists(colorsDirectory))
                {
                    Directory.CreateDirectory(colorsDirectory);
                }

                var filePath = Path.Combine(colorsDirectory, $"{color}.png");
                TextColorDropdown.Add(color.ToString(), filePath);
            }
            TextColorDropdown.OnConfirm += TextColorDropdown_OnConfirm;

            TextColorDropdown.SetComboBoxReadOnly(true);

            EffectDropdown.SetComboBoxReadOnly(true);
            foreach (var effect in Enum.GetValues(typeof(ICMessage.Effects)).Cast<ICMessage.Effects>())
            {
                var path = $"pack://application:,,,/Resources/Buttons/MessageEffects/{effect.ToString().ToLower()}.png";
                EffectDropdown.Add(effect.ToString(), effect == ICMessage.Effects.None ? "" : path);
            }
            EffectDropdown.OnConfirm += EffectDropdown_OnConfirm;

            sfxDropdown.SetImageFieldVisible(false);
            sfxDropdown.OnConfirm += SfxDropdown_OnConfirm;

            txtICShowname.MaxLength = ICShownameMaxLength;
            txtICMessage.MaxLength = ICMessageMaxLength;
        }

        private void SfxDropdown_OnConfirm(object? sender, string sfx)
        {
            txtICMessage.Focus();
            curClient.curSFX = sfx;
        }

        public void ReinitializeSettings()
        {
            // Reinitialize Character Dropdown
            CharacterDropdown.Clear();
            foreach (var ini in CharacterINI.FullList)
            {
                CharacterDropdown.Add(ini.Name, ini.CharIconPath);
            }

            // Reinitialize Emote Dropdown
            EmoteDropdown.Clear();

            // Reinitialize Text Color Dropdown
            TextColorDropdown.Clear();
            foreach (var color in Enum.GetValues(typeof(ICMessage.TextColors)).Cast<ICMessage.TextColors>())
            {
                var colorsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Resources", "Colors");
                if (!Directory.Exists(colorsDirectory))
                {
                    Directory.CreateDirectory(colorsDirectory);
                }

                var filePath = Path.Combine(colorsDirectory, $"{color}.png");
                TextColorDropdown.Add(color.ToString(), filePath);
            }

            // Reinitialize Effect Dropdown
            EffectDropdown.Clear();
            foreach (var effect in Enum.GetValues(typeof(ICMessage.Effects)).Cast<ICMessage.Effects>())
            {
                var path = $"pack://application:,,,/Resources/Buttons/MessageEffects/{effect.ToString().ToLower()}.png";
                EffectDropdown.Add(effect.ToString(), effect == ICMessage.Effects.None ? "" : path);
            }
        }

        private void EffectDropdown_OnConfirm(object? sender, string newEffect)
        {
            if (Enum.TryParse(newEffect, out ICMessage.Effects parsedEffect))
            {
                curClient.effect = parsedEffect;
                txtICMessage.Focus();
            }
            else
            {
                // Handle the error if the color cannot be parsed
                CustomConsole.WriteLine($"Invalid color: {newEffect}");
            }
        }

        private void TextColorDropdown_OnConfirm(object? sender, string newColor)
        {
            if (curClient == null) return;

            if (Enum.TryParse(newColor, out ICMessage.TextColors parsedColor))
            {
                curClient.textColor = parsedColor;
                txtICMessage.Focus();
            }
            else
            {
                // Handle the error if the color cannot be parsed
                CustomConsole.WriteLine($"Invalid color: {newColor}");
            }
        }

        private void PositionDropdown_OnConfirm(object? sender, string newPos)
        {
            if (curClient == null) return;

            curClient.SetPos(newPos);
            txtICMessage.Focus();
        }

        private void EmoteDropdown_OnConfirm(object? sender, string emoteDisplayID)
        {
            //toggles the emote button that corresponds to the selected emote in the dropdown
            var emote = emotes.FirstOrDefault(x => x.Key.DisplayID == emoteDisplayID).Value;
            if (emote == null) return;
            emote.IsChecked = true;
            txtICMessage.Focus();
        }

        private void CharacterDropdown_OnConfirm(object? sender, string iniName)
        {
            var ini = CharacterINI.FullList.FirstOrDefault(x => x.Name == iniName);
            if(ini != null)
            {
                txtICMessage.Focus();

                if (curClient.currentINI == ini) return;

                curClient.SetCharacter(ini);
                SetINI(ini);
            }
            else
            {
                //handle error in customconsole.writeline method
                CustomConsole.WriteLine($"Character {iniName} not found.");

            }
        }

        public void SetClient(AOClient client)
        {
            if (this.curClient != null)
            {
                curClient.OnSideChange -= UpdatePos;
                curClient.OnBGChange -= EventHandler_ClientOnBgChange;
            }

            this.curClient = client;
            SetINI(client.currentINI);
            txtICShowname.Text = client.ICShowname;

            chkPreanim.IsChecked = client.emoteMod == ICMessage.EmoteModifiers.PlayPreanimation;
            chkFlip.IsChecked = client.flip;
            chkAdditive.IsChecked = client.Additive;
            chkImmediate.IsChecked = client.Immediate;

            CharacterDropdown.SelectedText = client.currentINI.Name;
            TextColorDropdown.SelectedText = client.textColor.ToString();
            EffectDropdown.SelectedText = client.effect.ToString();

            //pos
            UpdatePosDropdown(client);
            curClient.OnBGChange += EventHandler_ClientOnBgChange;
            curClient.OnSideChange += UpdatePos;
        }

        private void EventHandler_ClientOnBgChange(string newBG)
        {
            UpdatePosDropdown(this.curClient);
        }

        private void UpdatePosDropdown(AOClient client)
        {
            PositionDropdown.Dispatcher.Invoke(() =>
            {
                PositionDropdown.Clear();
                var bg = AOBot_Testing.Structures.Background.FromBGPath(client.curBG);

                if(bg != null)
                {
                    var allPos = bg.GetPossiblePositions();
                    foreach (var pos in allPos)
                    {
                        PositionDropdown.Add(pos.Key, pos.Value);
                    }
                    if (allPos.ContainsKey(client.curPos))
                    {
                        PositionDropdown.SelectedText = client.curPos;
                    }
                    else if (allPos.ContainsKey(client.currentINI.Side))
                    {
                        PositionDropdown.SelectedText = client.currentINI.Side;
                    }
                    else
                    {
                        if(allPos.Count > 0)
                        {
                            PositionDropdown.SelectedText = allPos.First().Key;
                        }
                    }
                }
                else
                {
                    PositionDropdown.SelectedText = client.currentINI.Side;
                }
            });
        }
        private void UpdatePos(string newPos)
        {
            PositionDropdown.Dispatcher.Invoke(() =>
            {
                curClient.OnSideChange -= UpdatePos;
                PositionDropdown.SelectedText = newPos;
                curClient.OnSideChange += UpdatePos;
            });
        }
        private void SetINI(CharacterINI ini)
        {
            txtICShowname_Placeholder.Text = ini.ShowName;

            EmoteGrid.ClearGrid();
            emotes.Clear();
            EmoteDropdown.Clear();
            foreach (var emote in ini.Emotions.Values)
            {
                AddEmote(emote);
            }

            emotes.First(x => x.Key.DisplayID == curClient.currentEmote.DisplayID).Value.IsChecked = true;


            sfxDropdown.Clear();
            sfxDropdown.Add("Default", "");
            sfxDropdown.Add("Nothing", "");
            if (File.Exists(ini.SoundListPath))
            {
                var sfxLines = File.ReadAllLines(ini.SoundListPath);

                foreach (var sfx in sfxLines)
                {
                    sfxDropdown.Add(sfx, "");
                }
            }
            sfxDropdown.SelectedText = "Default";
        }
        private void AddEmote(Emote emote)
        {
            string buttonOff = emote.PathToImage_off;
            string buttonOn = emote.PathToImage_on;

            ToggleButton toggleBtn = new ToggleButton
            {
                Width = 40,
                Height = 40,
                ToolTip = emote.DisplayID,
                Focusable = false,
                IsTabStop = false
            };

            toggleBtn.Checked += EmoteToggleBtn_Checked;
            toggleBtn.Unchecked += EmoteToggleBtn_Unchecked;

            if (System.IO.File.Exists(buttonOff) && System.IO.File.Exists(buttonOn))
            {
                // Create the ControlTemplate dynamically
                ControlTemplate template = new ControlTemplate(typeof(ToggleButton));
                FrameworkElementFactory gridFactory = new FrameworkElementFactory(typeof(Grid));
                FrameworkElementFactory imageFactory = new FrameworkElementFactory(typeof(Image));
                imageFactory.Name = "ButtonImage";
                imageFactory.SetValue(Image.WidthProperty, 40.0);
                imageFactory.SetValue(Image.HeightProperty, 40.0);
                imageFactory.SetValue(Image.SourceProperty, new BitmapImage(new Uri(buttonOff, UriKind.Absolute)));

                gridFactory.AppendChild(imageFactory);
                template.VisualTree = gridFactory;

                // Add the trigger for toggled state (change image)
                Trigger trigger = new Trigger { Property = ToggleButton.IsCheckedProperty, Value = true };
                trigger.Setters.Add(new Setter
                {
                    Property = Image.SourceProperty,
                    TargetName = "ButtonImage",
                    Value = new BitmapImage(new Uri(buttonOn, UriKind.Absolute))
                });

                template.Triggers.Add(trigger);
                toggleBtn.Template = template;
            }
            else
            {
                // No image exists, use a default button with text
                toggleBtn.Content = emote.DisplayID;
            }

            EmoteDropdown.Add(emote.DisplayID, emote.PathToImage_off);

            toggleBtn.Focusable = false;
            toggleBtn.IsTabStop = false;
            EmoteGrid.AddElement(toggleBtn);
            emotes.Add(emote, toggleBtn);
        }

        public void ClearSettings()
        {
            ReinitializeSettings();
            CharacterDropdown.SelectedText = string.Empty;
            EffectDropdown.SelectedText = string.Empty;
            EmoteDropdown.SelectedText = string.Empty;
            PositionDropdown.SelectedText = string.Empty;
            TextColorDropdown.SelectedText = string.Empty;
            sfxDropdown.SelectedText = string.Empty;
            txtICShowname.Clear();
            txtICMessage.Clear();
            EmoteGrid.ClearGrid();
        }

        private void EmoteToggleBtn_Checked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            foreach (var button in emotes.Values)
            {
                if (button != clickedButton)
                {
                    button.Unchecked -= EmoteToggleBtn_Unchecked;
                    button.IsChecked = false;
                    button.Unchecked += EmoteToggleBtn_Unchecked;
                }
            }

            var emote = emotes.FirstOrDefault(x => x.Value == clickedButton).Key;
            EmoteDropdown.SelectedText = emote.DisplayID;
            curClient.SetEmote(emote.DisplayID);
            chkPreanim.IsChecked = true;
            txtICMessage.Focus();
        }
        private void EmoteToggleBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            if (clickedButton.IsChecked == false)
            {
                chkPreanim.IsChecked = !chkPreanim.IsChecked;

                clickedButton.Checked -= EmoteToggleBtn_Checked;
                clickedButton.IsChecked = true;
                clickedButton.Checked += EmoteToggleBtn_Checked;
            }
        }
        private void txtICMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Prevents the beep sound from default Enter behavior
                string message = txtICMessage.Text;
                OnSendICMessage?.Invoke(message);
            }
        }

        public Action OnResetMessageEffects;
        public void ResetMessageEffects()
        {
            btnRealization.IsChecked = false;
            btnScreenshake.IsChecked = false;
            EffectDropdown.SelectedText = ICMessage.Effects.None.ToString();
            chkPreanim.IsChecked = false;
            sfxDropdown.SelectedText = "";

            OnResetMessageEffects?.Invoke();
        }

        private void txtICShowname_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtICShowname_Placeholder.Visibility = string.IsNullOrWhiteSpace(txtICShowname.Text) ? Visibility.Visible : Visibility.Collapsed;

            curClient?.SetICShowname(txtICShowname.Text.Trim());
        }

        private void txtICMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtICMessage_Placeholder.Visibility = string.IsNullOrWhiteSpace(txtICMessage.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void chkPreanim_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                // Assuming 'currentClient' is an instance of AOBot
                curClient.emoteMod = checkBox.IsChecked == true ? ICMessage.EmoteModifiers.PlayPreanimation : ICMessage.EmoteModifiers.NoPreanimation;
                txtICMessage.Focus();
            }
        }

        private void chkFlip_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.flip = checkBox.IsChecked == true;
                txtICMessage.Focus();
            }
        }

        private void chkAdditive_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.Additive = checkBox.IsChecked == true;
                txtICMessage.Focus();
            }
        }

        private void chkImmediate_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.Immediate = checkBox.IsChecked == true;
                txtICMessage.Focus();
            }
        }

        private void btnRealization_Checked(object sender, RoutedEventArgs e)
        {
            // Handle the checked state
            if (sender is ToggleButton toggleButton)
            {
                EffectDropdown.SelectedText = ICMessage.Effects.Realization.ToString();
                txtICMessage.Focus();
            }
        }

        private void btnRealization_Unchecked(object sender, RoutedEventArgs e)
        {
            // Handle the unchecked state
            if (sender is ToggleButton toggleButton)
            {
                if(EffectDropdown.SelectedText == ICMessage.Effects.Realization.ToString())
                    EffectDropdown.SelectedText = ICMessage.Effects.None.ToString();

                txtICMessage.Focus();
            }
        }

        private void btnScreenshake_Checked(object sender, RoutedEventArgs e)
        {
            // Handle the checked state
            if (sender is ToggleButton toggleButton)
            {
                curClient.screenshake = true;
                txtICMessage.Focus();
            }
        }

        private void btnScreenshake_Unchecked(object sender, RoutedEventArgs e)
        {
            // Handle the unchecked state
            if (sender is ToggleButton toggleButton)
            {
                curClient.screenshake = false;
                txtICMessage.Focus();
            }
        }
    }
}
