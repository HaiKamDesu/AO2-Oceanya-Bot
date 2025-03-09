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

namespace OceanyaClient.Components
{
    /// <summary>
    /// Interaction logic for ICMessageSettings.xaml
    /// </summary>
    public partial class ICMessageSettings : UserControl
    {

        Dictionary<Emote, ToggleButton> emotes = new();
        AOBot curClient;

        public Action<string> OnSendICMessage;

        public ICMessageSettings()
        {
            InitializeComponent();

            EmoteGrid.SetScrollMode(PageButtonGrid.ScrollMode.Horizontal);
            EmoteGrid.SetPageSize(2, 10);

            foreach (var ini in CharacterINI.FullList)
            {
                CharacterDropdown.AddDropdownItem(ini.Name, ini.CharIconPath);
            }

            CharacterDropdown.OnConfirm += CharacterDropdown_OnConfirm;
        }

        private void CharacterDropdown_OnConfirm(object? sender, string iniName)
        {
            var ini = CharacterINI.FullList.FirstOrDefault(x => x.Name == iniName);
            if(ini != null)
            {
                curClient.SetCharacter(ini);
                SetINI(ini);
            }
            else
            {
                //handle error in customconsole.writeline method
                CustomConsole.WriteLine($"Character {iniName} not found.");

            }
        }

        public void SetClient(AOBot client)
        {
            this.curClient = client;
            SetINI(client.currentINI);
            txtICShowname.Text = client.ICShowname;

            chkPreanim.IsChecked = client.emoteMod == ICMessage.EmoteModifiers.PlayPreanimation;
            chkFlip.IsChecked = client.flip;
            chkAdditive.IsChecked = client.Additive;
            chkImmediate.IsChecked = client.Immediate;


            CharacterDropdown.SelectedText = client.currentINI.Name;
        }

        private void SetINI(CharacterINI ini)
        {
            txtICShowname_Placeholder.Text = ini.ShowName;

            EmoteGrid.ClearGrid();
            emotes.Clear();
            foreach (var emote in ini.Emotions.Values)
            {
                AddEmote(emote);
            }

            emotes.First(x => x.Key.Name == curClient
            .currentEmote.Name).Value.IsChecked = true;
        }
        private void AddEmote(Emote emote)
        {
            string buttonOff = emote.PathToImage_off;
            string buttonOn = emote.PathToImage_on;

            ToggleButton toggleBtn = new ToggleButton
            {
                Width = 40,
                Height = 40,
                ToolTip = emote.Name
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
                toggleBtn.Content = emote.Name;
            }

            EmoteGrid.AddElement(toggleBtn);
            emotes.Add(emote, toggleBtn);
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
            curClient.SetEmote(emote.Name);
        }
        private void EmoteToggleBtn_Unchecked(object sender, RoutedEventArgs e)
        {
            ToggleButton clickedButton = sender as ToggleButton;

            if (clickedButton.IsChecked == false)
            {
                chkPreanim.IsChecked = !chkPreanim.IsChecked;
                clickedButton.IsChecked = true;
            }
        }
        private void txtICMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true; // Prevents the beep sound from default Enter behavior
                string message = txtICMessage.Text;
                txtICMessage.Clear();
                OnSendICMessage?.Invoke(message);
            }
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
            }
        }

        private void chkFlip_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.flip = checkBox.IsChecked == true;
            }
        }

        private void chkAdditive_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.Additive = checkBox.IsChecked == true;
            }
        }

        private void chkImmediate_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                curClient.Immediate = checkBox.IsChecked == true;
            }
        }
    }
}
