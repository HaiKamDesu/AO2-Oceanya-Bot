using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace OceanyaClient.Components
{
    public partial class OOCLog : UserControl
    {
        public Action<string, string> OnSendOOCMessage;

        public OOCLog()
        {
            InitializeComponent();

            LogBox.Document.Blocks.Clear();
            // adds a really really big block of new lines to the log directly through the logbox variable
            LogBox.AppendText(new string('\n', 20));
            ScrollViewer.ScrollToEnd();
        }

        public void AddMessage(string showName, string message, bool isSentFromServer = false)
        {
            // Convert ~text~ to red color text
            message = ConvertFormatting(message);

            // Create a new paragraph
            Paragraph paragraph = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 2), // Adjust vertical spacing
                LineHeight = 2 // Increase/decrease for more spacing
            };

            // Add the player's show name in bold
            Run nameRun = new Run($"{showName}: ") { FontWeight = System.Windows.FontWeights.Bold };
            if (isSentFromServer)
            {
                nameRun.Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x5F, 0x5F, 0x00));
            }
            else
            {
                nameRun.Foreground = Brushes.DarkBlue;
            }

                paragraph.Inlines.Add(nameRun);

            // Add formatted message
            paragraph.Inlines.Add(new Run(message));

            // Append to the log
            LogBox.Document.Blocks.Add(paragraph);

            // Auto-scroll to the bottom
            // Ensure UI updates before scrolling
            LogBox.Dispatcher.InvokeAsync(() =>
            {
                LogBox.ScrollToEnd();
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        private string ConvertFormatting(string message)
        {
            return message;
        }

        private void txtOOCMessage_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtOOCMessage_Placeholder.Visibility = string.IsNullOrWhiteSpace(txtOOCMessage.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void txtOOCShowname_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtOOCShowname_Placeholder.Visibility = string.IsNullOrWhiteSpace(txtOOCShowname.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void txtOOCMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if(txtOOCShowname.Text == "")
                {
                    AddMessage("Oceanya Client", "You must set a showname before sending a message!", true);
                    return;
                }
                e.Handled = true; // Prevents the beep sound from default Enter behavior
                string message = txtOOCMessage.Text;
                txtOOCMessage.Clear();
                OnSendOOCMessage?.Invoke(txtOOCShowname.Text, message);
            }
        }
    }
}
