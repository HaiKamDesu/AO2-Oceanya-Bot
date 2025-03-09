using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace OceanyaClient.Components
{
    public partial class ICLog : UserControl
    {
        public ICLog()
        {
            InitializeComponent();

            LogBox.Document.Blocks.Clear();
        }

        public void AddMessage(string showName, string message, bool isSentFromSelf = false)
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
            if (isSentFromSelf)
            {
                nameRun.Foreground = Brushes.LightCyan;
            }

            paragraph.Inlines.Add(nameRun);

            // Add formatted message
            paragraph.Inlines.Add(new Run(message));

            // Append to the log
            LogBox.Document.Blocks.Add(paragraph);

            // Auto-scroll to the bottom
            LogBox.ScrollToEnd();
        }

        private string ConvertFormatting(string message)
        {
            // Replace ~text~ with colored red text using XAML formatting
            return Regex.Replace(message, @"~(.*?)~", match =>
            {
                Run coloredRun = new Run(match.Groups[1].Value) { Foreground = Brushes.Red };
                return new TextRange(coloredRun.ContentStart, coloredRun.ContentEnd).Text;
            });
        }
    }
}
