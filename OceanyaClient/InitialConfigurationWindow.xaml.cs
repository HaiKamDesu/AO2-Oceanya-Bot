using AOBot_Testing.Structures;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace OceanyaClient
{
    /// <summary>
    /// Interaction logic for InitialConfigurationWindow.xaml
    /// </summary>
    public partial class InitialConfigurationWindow : Window
    {
        public InitialConfigurationWindow()
        {
            InitializeComponent();
            // Load saved configuration
            LoadConfiguration();
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe",
                Title = "Select Attorney_Online.exe"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                ExePathTextBox.Text = openFileDialog.FileName;
            }
        }

        private async void OkButton_Click(object sender, RoutedEventArgs e)
        {
            // Validate inputs
            string exePath = ExePathTextBox.Text;
            string connectionPath = ConnectionPathTextBox.Text;

            if (string.IsNullOrWhiteSpace(exePath) || string.IsNullOrWhiteSpace(connectionPath))
            {
                MessageBox.Show("Please provide both the Attorney_Online.exe path and the connection path.",
                                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Save to settings or use as needed
            Globals.BaseFolder = Path.Combine(Path.GetDirectoryName(exePath), "base");
            Globals.ConnectionString = connectionPath;

            // Save configuration to file
            SaveConfiguration(exePath, connectionPath);

            // Refresh character and background info if checkbox is checked
            if (RefreshInfoCheckBox.IsChecked == true)
            {
                await WaitForm.ShowFormAsync("Refreshing character and background info...", this);
                CharacterINI.RefreshCharacterList();
                WaitForm.CloseForm();
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            
            this.Close();
        }

        private void LoadConfiguration()
        {
            // Implement your configuration loading logic here
            // This would replace the LoadConfiguration method from the original code
            try
            {
                // Example implementation (adjust according to your actual storage mechanism)
                if (File.Exists("config.txt"))
                {
                    string[] lines = File.ReadAllLines("config.txt");
                    if (lines.Length >= 2)
                    {
                        ExePathTextBox.Text = lines[0];
                        ConnectionPathTextBox.Text = lines[1];
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading configuration: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveConfiguration(string exePath, string connectionPath)
        {
            // Implement your configuration saving logic here
            // This would replace the SaveConfiguration method from the original code
            try
            {
                File.WriteAllLines("config.txt", new[] { exePath, connectionPath });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving configuration: " + ex.Message,
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
