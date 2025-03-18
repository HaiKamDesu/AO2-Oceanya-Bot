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

        // Modify the FilterDropdown method
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
            cboINISelect.ItemsSource = filteredItems;
            isInternalUpdate = false;

            // Only open dropdown if we have items and user is actively typing
            cboINISelect.IsDropDownOpen = filteredItems.Count > 0;
        }

        // Update the TextChanged handler
        private void cboINISelect_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isReadOnly || isInternalUpdate) return;

            // Store current selection positions
            int selectionStart = editableTextBox.SelectionStart;
            int selectionLength = editableTextBox.SelectionLength;

            // Filter dropdown based on current text
            FilterDropdown(editableTextBox.Text);

            // Restore selection after filtering
            editableTextBox.SelectionStart = selectionStart;
            editableTextBox.SelectionLength = selectionLength;
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



        private void cboINISelect_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                
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
}
