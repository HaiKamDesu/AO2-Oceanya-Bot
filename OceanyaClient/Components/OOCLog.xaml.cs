﻿using AOBot_Testing.Agents;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;

namespace OceanyaClient.Components
{
    public partial class OOCLog : UserControl
    {
        public static int OOCShownameLengthLimit = 30;
        public Action<string, string> OnSendOOCMessage;

        private Dictionary<AOClient, FlowDocument> clientLogs = new Dictionary<AOClient, FlowDocument>();

        private AOClient currentClient = null;
        private ScrollViewer ScrollViewer;

        // URL detection regex pattern
        private static readonly Regex UrlRegex = new Regex(@"(https?:\/\/[^\s]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public OOCLog()
        {
            InitializeComponent();
            LogBox.Document = new FlowDocument();

            Loaded += OOCLog_Loaded;
            txtOOCShowname.MaxLength = OOCShownameLengthLimit;
        }

        private void OOCLog_Loaded(object sender, RoutedEventArgs e)
        {
            ScrollViewer = GetScrollViewer(LogBox);
        }

        public void SetCurrentClient(AOClient client)
        {
            currentClient = client;

            if (!clientLogs.ContainsKey(client))
            {
                clientLogs[client] = new FlowDocument();
            }

            LogBox.Document = clientLogs[client];
            UpdateStreamLabel(client);
            ScrollToBottom();
        }

        public void UpdateStreamLabel(AOClient client)
        {
            lblStream.Content = client != null ? $"[{client.playerID}] {client.iniPuppetName} (\"{client.clientName}\")" : "[STREAM]";
        }

        public void AddMessage(AOClient client, string showName, string message, bool isSentFromServer = false)
        {
            if (client == null)
            {
                DisplayMessage("System", "No client selected. Message not stored.", true);
                return;
            }

            if (!clientLogs.ContainsKey(client))
            {
                clientLogs[client] = new FlowDocument();
            }

            bool shouldScroll = IsScrolledToBottom();

            FlowDocument clientDoc = clientLogs[client];

            Paragraph paragraph = new Paragraph
            {
                Margin = new Thickness(0, 2, 0, 2),
                LineHeight = 2
            };

            Run nameRun = new Run($"{showName}: ") { FontWeight = FontWeights.Bold };
            nameRun.Foreground = isSentFromServer
                ? new SolidColorBrush(Color.FromArgb(0xFF, 0x5F, 0x5F, 0x00))
                : Brushes.DarkBlue;

            paragraph.Inlines.Add(nameRun);

            // Process the message to detect and convert URLs to hyperlinks
            AddTextWithHyperlinks(paragraph, message);

            clientDoc.Blocks.Add(paragraph);

            if (client == currentClient)
            {
                LogBox.Document = clientDoc;
                if (shouldScroll)
                    ScrollToBottom();
            }
        }

        private void AddTextWithHyperlinks(Paragraph paragraph, string text)
        {
            // Find all URLs in the text
            var matches = UrlRegex.Matches(text);

            if (matches.Count == 0)
            {
                // No URLs, just add the text
                paragraph.Inlines.Add(new Run(text));
                return;
            }

            int lastIndex = 0;
            foreach (Match match in matches)
            {
                // Add text before the URL
                if (match.Index > lastIndex)
                {
                    string beforeText = text.Substring(lastIndex, match.Index - lastIndex);
                    paragraph.Inlines.Add(new Run(beforeText));
                }

                // Create and add the hyperlink
                string url = match.Value;
                Hyperlink hyperlink = new Hyperlink(new Run(url))
                {
                    NavigateUri = new Uri(url)
                };
                hyperlink.RequestNavigate += Hyperlink_RequestNavigate;
                paragraph.Inlines.Add(hyperlink);

                lastIndex = match.Index + match.Length;
            }

            // Add any remaining text after the last URL
            if (lastIndex < text.Length)
            {
                string afterText = text.Substring(lastIndex);
                paragraph.Inlines.Add(new Run(afterText));
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open the URL in the default browser
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                DisplayMessage("System", $"Failed to open URL: {ex.Message}", true);
            }
        }

        private ScrollViewer GetScrollViewer(DependencyObject dep)
        {
            if (dep is ScrollViewer) return dep as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
            {
                var child = VisualTreeHelper.GetChild(dep, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void DisplayMessage(string showName, string message, bool isSentFromServer)
        {
            if (currentClient == null) return;
            AddMessage(currentClient, showName, message, isSentFromServer);
        }

        public void ClearClientLog(AOClient client)
        {
            if (clientLogs.ContainsKey(client))
            {
                clientLogs[client] = new FlowDocument();

                if (client == currentClient)
                {
                    LogBox.Document = clientLogs[client];
                }
            }
        }

        public void ClearAllLogs()
        {
            clientLogs.Clear();
            LogBox.Document = new FlowDocument();
        }

        public void ScrollToBottom()
        {
            if (ScrollViewer != null)
            {
                ScrollViewer.Dispatcher.InvokeAsync(() => ScrollViewer.ScrollToEnd(),
                    System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private bool IsScrolledToBottom()
        {
            if (ScrollViewer == null) return true;
            return ScrollViewer.VerticalOffset >= ScrollViewer.ScrollableHeight - 10; // 10px tolerance
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
                e.Handled = true;

                if (string.IsNullOrWhiteSpace(txtOOCShowname.Text))
                {
                    AddMessage(currentClient, "Oceanya Client", "You must set a showname before sending a message!", true);
                    return;
                }

                if (currentClient == null)
                {
                    AddMessage(currentClient, "Oceanya Client", "No client selected. Please select a client first.", true);
                    return;
                }

                string message = txtOOCMessage.Text;
                txtOOCMessage.Clear();
                OnSendOOCMessage?.Invoke(txtOOCShowname.Text, message);
            }
        }
    }
}