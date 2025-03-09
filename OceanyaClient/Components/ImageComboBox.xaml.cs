using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace OceanyaClient.Components
{
    public partial class ImageComboBox : UserControl
    {
        public class DropdownItem
        {
            public string Name { get; set; }
            public string ImagePath { get; set; }
        }

        private List<DropdownItem> allItems;
        private bool isManuallySelecting = false;
        private TextBox editableTextBox; // The ComboBox's editable TextBox
        private bool enableAutoComplete = true; // Default to true
        public event EventHandler<string> OnConfirm;
        public bool EnableAutoComplete
        {
            get => enableAutoComplete;
            set
            {
                enableAutoComplete = value;
                if (!enableAutoComplete)
                {
                    cboINISelect.IsDropDownOpen = false; // Ensure dropdown is closed when disabled
                }
            }
        }

        public ImageComboBox()
        {
            InitializeComponent();

            // Get reference to the internal TextBox
            cboINISelect.Loaded += (s, e) =>
            {
                editableTextBox = cboINISelect.Template.FindName("PART_EditableTextBox", cboINISelect) as TextBox;
            };
        }

        public void AddDropdownItem(string name, string imagePath)
        {
            if (allItems == null)
            {
                allItems = new List<DropdownItem>();
            }

            allItems.Add(new DropdownItem { Name = name, ImagePath = imagePath });
            cboINISelect.ItemsSource = null; // Reset the ItemsSource to refresh the ComboBox
            cboINISelect.ItemsSource = allItems;
        }


        string realInput = "";
        private void cboINISelect_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                isManuallySelecting = true;

                if (editableTextBox != null)
                {
                    editableTextBox.SelectionStart = editableTextBox.Text.Length;
                    editableTextBox.SelectionLength = 0;
                }
                // Disable autocomplete after committing selection
                EnableAutoComplete = false;

                // Close dropdown and prevent reopening

                // Move focus away to fully exit editing mode
                cboINISelect.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Keyboard.ClearFocus();
                }), System.Windows.Threading.DispatcherPriority.Background);

                isManuallySelecting = false;
                cboINISelect.IsDropDownOpen = false;

                ConfirmSelection(); //Fire an event when Enter is pressed
            }
            else if (e.Key == Key.Back)
            {
                EnableAutoComplete = false; // Disable autocomplete after Backspace
            }
            else if (!(char.IsLetter((char)KeyInterop.VirtualKeyFromKey(e.Key)) || e.Key == Key.Space))
            {
                return;
            }
            else if (!EnableAutoComplete)
            {
                EnableAutoComplete = true; // Enable autocomplete when typing a normal key
            }

            if (editableTextBox == null) return;

            realInput = editableTextBox.Text.Substring(0, editableTextBox.SelectionStart);

            if (isManuallySelecting || !EnableAutoComplete) return;

            string input = editableTextBox.Text;
            if (string.IsNullOrWhiteSpace(input))
            {
                cboINISelect.ItemsSource = allItems;
                cboINISelect.IsDropDownOpen = false;
                return;
            }

            var match = allItems.FirstOrDefault(item => item.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                cboINISelect.ItemsSource = allItems.Where(item => item.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase)).ToList();
                cboINISelect.IsDropDownOpen = true;

                if (match.Name.ToLower() != realInput.ToLower())
                {
                    if (match.Name.Length >= input.Length)
                    {
                        editableTextBox.Text = match.Name;
                        editableTextBox.SelectionStart = input.Length;
                        editableTextBox.SelectionLength = match.Name.Length - input.Length;
                    }
                }
                else if(match.Name.Length == 1)
                {
                    editableTextBox.SelectionStart = editableTextBox.Text.Length;
                    editableTextBox.SelectionLength = 0;
                }
            }
            else
            {
                cboINISelect.IsDropDownOpen = false;
            }

            // Ensure the TextBox remains focused
            editableTextBox.Focus();
        }





        private void cboINISelect_PreviewKeyDown(object sender, KeyEventArgs e)
        {
        }




        private void cboINISelect_LostFocus(object sender, RoutedEventArgs e)
        {
            cboINISelect.IsDropDownOpen = false;

            if (cboINISelect.Text == "") return;
            
            ConfirmSelection(); // Fire event when focus is lost
        }


        public string SelectedText
        {
            get => cboINISelect.Text;
            set => cboINISelect.Text = value;
        }

        public void SetItemsSource(List<DropdownItem> items)
        {
            allItems = items;
            cboINISelect.ItemsSource = allItems;
        }

        private void ConfirmSelection()
        {
            OnConfirm?.Invoke(this, cboINISelect.Text); // Fire event with selected text
        }

        private void cboINISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboINISelect.SelectedItem != null)
            {
                ConfirmSelection(); // Fire event when dropdown item is selected
            }
        }

    }
}
