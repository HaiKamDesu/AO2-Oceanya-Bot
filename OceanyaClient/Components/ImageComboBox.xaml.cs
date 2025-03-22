using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OceanyaClient.Components
{
    public partial class ImageComboBox : UserControl
    {
        public class DropdownItem
        {
            public string Name { get; set; }
            public string ImagePath { get; set; }
        }
        private const int MaxVisibleItems = 20000; // Show only X at a time

        private ObservableCollection<DropdownItem> allItems = new();
        private TextBox editableTextBox; // The ComboBox's editable TextBox
        public event EventHandler<string> OnConfirm;
        private bool isReadOnly = false;
        public ImageComboBox()
        {
            InitializeComponent();

            // Get reference to the internal TextBox
            cboINISelect.Loaded += (s, e) =>
            {
                editableTextBox = cboINISelect.Template.FindName("PART_EditableTextBox", cboINISelect) as TextBox;
                if (editableTextBox != null)
                {
                    editableTextBox.IsReadOnly = isReadOnly;
                    editableTextBox.TextChanged += cboINISelect_TextChanged;
                }
            };
        }
        public void Add(string name, string imagePath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                allItems.Add(new DropdownItem { Name = name, ImagePath = imagePath });
                cboINISelect.ItemsSource = allItems;
            });
        }


        // Add a flag to track when we're programmatically changing text
        private bool isInternalUpdate = false;
        private string lastConfirmedText = string.Empty;

        private void FilterDropdown(string input)
        {
            // Don't filter if we're in the middle of an internal update
            if (isInternalUpdate) return;

            var filteredItems = allItems
                .Where(item => item.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .Take(MaxVisibleItems)
                .ToList();

            // Update the dropdown items without changing the text
            isInternalUpdate = true;
            string currentText = cboINISelect.Text; // Store current text
            int selectionStart = editableTextBox?.SelectionStart ?? 0;
            int selectionLength = editableTextBox?.SelectionLength ?? 0;

            cboINISelect.ItemsSource = filteredItems;

            // Preserve text and selection
            if (!string.IsNullOrEmpty(currentText))
            {
                cboINISelect.Text = currentText;
                if (editableTextBox != null)
                {
                    editableTextBox.SelectionStart = selectionStart;
                    editableTextBox.SelectionLength = selectionLength;
                }
            }

            isInternalUpdate = false;

            // Only open dropdown if we have items
            cboINISelect.IsDropDownOpen = filteredItems.Count > 0;
        }

        // Modify the cboINISelect_TextChanged handler
        private void cboINISelect_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isReadOnly || isInternalUpdate) return;

            // Store current selection positions
            int selectionStart = editableTextBox.SelectionStart;
            int selectionLength = editableTextBox.SelectionLength;

            // Filter dropdown based on current text
            FilterDropdown(editableTextBox.Text);

            // Restore selection after filtering, but only if we haven't completely changed the text
            // When a user types a character after all text is selected, we want the cursor to be after that character
            if (!(selectionLength == lastConfirmedText.Length && editableTextBox.Text.Length == 1))
            {
                editableTextBox.SelectionStart = selectionStart;
                editableTextBox.SelectionLength = selectionLength;
            }
            else
            {
                // If we've replaced all text with a single character, put cursor at the end
                editableTextBox.SelectionStart = editableTextBox.Text.Length;
                editableTextBox.SelectionLength = 0;
            }
        }

        // Update the SelectionChanged handler to prevent unwanted selections
        private void cboINISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only process selection changes that come from user clicking on an item
            // Not from filtering or programmatic changes
            if (e.AddedItems.Count > 0 && !isInternalUpdate && cboINISelect.IsDropDownOpen)
            {
                var item = (DropdownItem)e.AddedItems[0];
                ConfirmSelection(item.Name);
            }
        }

        // Modify ConfirmSelection to be more robust
        private void ConfirmSelection(string text)
        {
            isInternalUpdate = true;
            cboINISelect.IsDropDownOpen = false;

            // Find the exact match first
            var selectedItem = allItems.FirstOrDefault(item =>
                string.Equals(item.Name, text, StringComparison.OrdinalIgnoreCase));

            // If no exact match and we want to allow selecting text that doesn't match any item
            if (selectedItem == null)
            {
                SetSelectedItemImage("");
                cboINISelect.Text = text; // Preserve user input
            }
            else
            {
                // We found a match, use its properties
                SetSelectedItemImage(selectedItem.ImagePath);
                cboINISelect.Text = selectedItem.Name; // Use exact case from the item
            }

            lastConfirmedText = cboINISelect.Text;
            OnConfirm?.Invoke(this, cboINISelect.Text);

            if (!isReadOnly && editableTextBox != null)
            {
                editableTextBox.SelectionStart = editableTextBox.Text.Length;
                editableTextBox.SelectionLength = 0;
            }

            cboINISelect.Dispatcher.BeginInvoke(new Action(() =>
            {
                cboINISelect.SelectedItem = selectedItem;
                isInternalUpdate = false;
                cboINISelect.ItemsSource = allItems;
            }), System.Windows.Threading.DispatcherPriority.Background);


        }

        // Modify the LostFocus handler to be less disruptive
        private void cboINISelect_LostFocus(object sender, RoutedEventArgs e)
        {
            cboINISelect.IsDropDownOpen = false;

            // When focus is lost, confirm the current text if it has changed
            if (!isReadOnly && cboINISelect.Text != lastConfirmedText)
            {
                ConfirmSelection(cboINISelect.Text);
            }
        }

        // Update KeyDown handler to be more specific
        private void cboINISelect_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.isReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                cboINISelect.IsDropDownOpen = false;

                var match = allItems.FirstOrDefault(item => item.Name.StartsWith(cboINISelect.Text, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    ConfirmSelection(match.Name);
                }
                else
                {
                    ConfirmSelection(cboINISelect.Text);
                }
            }
        }



        // Update the existing PreviewKeyDown handler
        private void cboINISelect_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (isReadOnly)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                var match = allItems.FirstOrDefault(item => item.Name.StartsWith(cboINISelect.Text, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    ConfirmSelection(match.Name);
                }
                else
                {
                    ConfirmSelection(cboINISelect.Text);
                }

                cboINISelect.IsDropDownOpen = false;
            }
            // Handle single character input when all text is selected
            else if (editableTextBox != null &&
                    !e.Key.IsModifierKey() &&
                    editableTextBox.SelectionLength == editableTextBox.Text.Length &&
                    editableTextBox.SelectionLength > 0)
            {
                // For printable characters only
                if ((e.Key >= Key.A && e.Key <= Key.Z) ||
                    (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                    (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                    e.Key == Key.Space ||
                    e.Key == Key.OemMinus ||
                    e.Key == Key.OemPeriod ||
                    e.Key == Key.OemQuestion ||
                    (e.Key >= Key.Oem1 && e.Key <= Key.OemBackslash))
                {
                    // Get the character that would be entered
                    string key = e.Key.ToString();

                    // Handle letter keys
                    if (key.Length == 1 || (e.Key >= Key.A && e.Key <= Key.Z))
                    {
                        isInternalUpdate = true;

                        // For letters, need to handle shift for uppercase
                        if (e.Key >= Key.A && e.Key <= Key.Z)
                        {
                            key = ((char)('a' + (e.Key - Key.A))).ToString();

                            // Handle uppercase if shift is pressed
                            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                            {
                                key = key.ToUpper();
                            }
                        }
                        // Handle numbers and symbols
                        else if (e.Key >= Key.D0 && e.Key <= Key.D9)
                        {
                            key = ((char)('0' + (e.Key - Key.D0))).ToString();
                        }
                        else if (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9)
                        {
                            key = ((char)('0' + (e.Key - Key.NumPad0))).ToString();
                        }

                        // Set new text and position cursor at end
                        editableTextBox.Text = key;
                        editableTextBox.SelectionStart = 1;
                        editableTextBox.SelectionLength = 0;

                        // Filter dropdown with new text
                        FilterDropdown(key);

                        isInternalUpdate = false;
                        e.Handled = true; // Prevent default handling
                    }
                }
            }
        }

        public string SelectedText
        {
            get => cboINISelect.Text;
            set
            {
                var prevFocusable = editableTextBox.Focusable;
                editableTextBox.Focusable = false;
                cboINISelect.Text = value;
                ConfirmSelection(value);
                editableTextBox.Focusable = prevFocusable;
            }
        }

        public void Clear()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                allItems.Clear();
                cboINISelect.ItemsSource = null;
            });
        }
        public void SetSelectedItemImage(string imagePath)
        {
            if (cboINISelect.Template.FindName("imgSelected", cboINISelect) is Image imgSelected)
            {
                try
                {
                    Uri imageUri;
                    if (imagePath.StartsWith("pack://application:,,,"))
                    {
                        imageUri = new Uri(imagePath, UriKind.Absolute);
                    }
                    else
                    {
                        imageUri = new Uri(imagePath, UriKind.RelativeOrAbsolute);
                    }

                    imgSelected.Source = new BitmapImage(imageUri);
                }
                catch (Exception)
                {
                    imgSelected.Source = null; // Set to no image if there's an error
                }
            }
        }

        public void SetImageFieldVisible(bool isVisible)
        {
        }

        private void EditableTextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (isReadOnly)
            {
                e.Handled = true; // Prevents the TextBox from being clicked/focused
                cboINISelect.IsDropDownOpen = !cboINISelect.IsDropDownOpen;
            }
        }

        private void EditableTextBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (isReadOnly)
            {
                e.Handled = true; // Prevents the TextBox from being clicked/focused

                // Don't toggle the dropdown here, as it might conflict with mouse click behavior
                // Just prevent focus
            }
        }

        // Update the SetComboBoxReadOnly method:
        public void SetComboBoxReadOnly(bool isReadOnly)
        {
            this.isReadOnly = isReadOnly;

            // Update control properties based on read-only state
            if (editableTextBox != null)
            {
                editableTextBox.IsReadOnly = isReadOnly;
            }

            Focusable = !isReadOnly;
            IsTabStop = !isReadOnly;

            // If the control is read-only, ensure the dropdown button is still clickable
            // and the text area won't try to get focus
            if (isReadOnly)
            {
                // The EditableTextBox_PreviewMouseDown and EditableTextBox_PreviewGotKeyboardFocus
                // methods will handle clicks and focus events when in read-only mode
            }
            else
            {
                // Ensure the textbox can receive input when not read-only
                if (editableTextBox != null)
                {
                    editableTextBox.Focusable = true;
                }
            }
        }


    }

    // Add this extension method to Key enum
    public static class KeyExtensions
    {
        public static bool IsModifierKey(this Key key)
        {
            return key == Key.LeftShift || key == Key.RightShift ||
                   key == Key.LeftCtrl || key == Key.RightCtrl ||
                   key == Key.LeftAlt || key == Key.RightAlt ||
                   key == Key.LWin || key == Key.RWin;
        }
    }
}
