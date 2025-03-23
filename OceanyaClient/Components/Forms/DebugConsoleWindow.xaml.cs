using Common;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OceanyaClient
{
    public partial class DebugConsoleWindow : Window
    {
        static DebugConsoleWindow _instance;
        public static void ShowWindow()
        {
            if(_instance == null)
            {
                _instance = new DebugConsoleWindow();
                _instance.Activate();
                _instance.Show();
                _instance.Focus();
                
            }
            else
            {
                _instance.Activate();
                _instance.Focus();
            }
            
        }
        public DebugConsoleWindow()
        {
            InitializeComponent();
            //WindowHelper.AddWindow(this);
            CustomConsole.OnWriteLine += AppendText;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var item in CustomConsole.lines)
            {
                AppendText(item);
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            CustomConsole.OnWriteLine -= AppendText;
            _instance = null;
        }

        private ScrollViewer GetTextBoxScrollViewer()
        {
            if (ConsoleTextBox == null) return null;

            // Get the ScrollViewer from the TextBox template
            return ConsoleTextBox.Template.FindName("PART_ContentHost", ConsoleTextBox) as ScrollViewer;
        }

        private void ScrollToBottom()
        {
            if (ConsoleTextBox != null)
            {
                ConsoleTextBox.Dispatcher.InvokeAsync(() =>
                {
                    ConsoleTextBox.ScrollToEnd();
                }, System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private bool IsScrolledToBottom()
        {
            ScrollViewer scrollViewer = GetTextBoxScrollViewer();
            if (scrollViewer == null) return true;

            return scrollViewer.VerticalOffset >= scrollViewer.ScrollableHeight - 10; // 10px tolerance
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging of the window by clicking and dragging on the title bar.
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // Close the window when the close button is clicked.
            this.Close();
        }

        /// <summary>
        /// Appends the provided text to the debug console and scrolls to the end.
        /// </summary>
        /// <param name="text">The text to append.</param>
        public void AppendText(string text)
        {
            // Ensure we're on the UI thread.
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AppendText(text));
                return;
            }

            // Append the new text along with a new line and scroll to the end.
            ConsoleTextBox.AppendText(text + Environment.NewLine);

            if (IsScrolledToBottom())
            {
                ScrollToBottom();
            }
        }
    }
}
