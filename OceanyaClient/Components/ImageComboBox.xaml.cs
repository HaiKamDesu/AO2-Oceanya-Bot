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
        private const int MaxVisibleItems = 20000; // Show only 20 at a time

        private ObservableCollection<DropdownItem> allItems = new();
        private bool isManuallySelecting = false;
        private TextBox editableTextBox; // The ComboBox's editable TextBox
        private bool enableAutoComplete = true; // Default to true
        public event EventHandler<string> OnConfirm;
        private bool isReadOnly = false;
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
        public static readonly DependencyProperty ShowImageProperty =
            DependencyProperty.Register("ShowImage", typeof(bool), typeof(ImageComboBox),
                new PropertyMetadata(true));

        public static readonly DependencyProperty IsEditableProperty =
            DependencyProperty.Register("IsEditable", typeof(bool), typeof(ImageComboBox),
                new PropertyMetadata(true));

        public bool ShowImage
        {
            get { return (bool)GetValue(ShowImageProperty); }
            set { SetValue(ShowImageProperty, value); }
        }

        public bool IsEditable
        {
            get { return (bool)GetValue(IsEditableProperty); }
            set { SetValue(IsEditableProperty, value); }
        }
        Grid grid;
        private ColumnDefinition columnIcon;
        private ColumnDefinition columnText;
        private ColumnDefinition columnDropdown;

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


        private void FilterDropdown(string input)
        {
            var filteredItems = allItems
                .Where(item => item.Name.StartsWith(input, StringComparison.OrdinalIgnoreCase))
                .Take(MaxVisibleItems)
                .ToList();

            cboINISelect.ItemsSource = filteredItems;
            cboINISelect.IsDropDownOpen = filteredItems.Count > 0;
        }


        public void Add(string name, string imagePath)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                allItems.Add(new DropdownItem { Name = name, ImagePath = imagePath });
            });
        }



        string userTypedInput = "";
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

                ConfirmSelection(cboINISelect.Text); //Fire an event when Enter is pressed
            }
        }

        private CancellationTokenSource debounceToken;

        private void cboINISelect_TextChanged(object sender, TextChangedEventArgs e)
        {
            userTypedInput = editableTextBox.Text.Substring(0, editableTextBox.SelectionStart);

            FilterDropdown(userTypedInput);

            EnableAutoComplete = true; 

            if (editableTextBox == null) return;

            if (isManuallySelecting || !EnableAutoComplete) return;


            var match = allItems.FirstOrDefault(item => item.Name.StartsWith(userTypedInput, StringComparison.OrdinalIgnoreCase));
            if (match != null)
            {
                if (match.Name.ToLower() != userTypedInput.ToLower())
                {
                    if (match.Name.Length >= userTypedInput.Length && !string.IsNullOrEmpty(userTypedInput))
                    {
                        editableTextBox.TextChanged -= cboINISelect_TextChanged;
                        editableTextBox.Text = match.Name;
                        editableTextBox.TextChanged += cboINISelect_TextChanged;
                        editableTextBox.SelectionStart = userTypedInput.Length;
                        editableTextBox.SelectionLength = match.Name.Length - userTypedInput.Length;
                    }
                }
                else if (match.Name.Length == 1)
                {
                    editableTextBox.SelectionStart = editableTextBox.Text.Length;
                    editableTextBox.SelectionLength = 0;
                }
            }

            // Ensure the TextBox remains focused
            editableTextBox.Focus();
        }




        private void cboINISelect_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Back)
            {
                EnableAutoComplete = false; // Disable autocomplete after Backspace

                // Check if the selection crosses the user-typed input
                if (editableTextBox.SelectionStart < userTypedInput.Length)
                {
                    string newText = userTypedInput.Substring(0, editableTextBox.SelectionStart);
                    editableTextBox.Text = newText;
                }
                else
                {
                    // Otherwise, handle backspace normally
                    editableTextBox.Text = userTypedInput;
                }

                editableTextBox.SelectionStart = editableTextBox.Text.Length;
                editableTextBox.SelectionLength = 0;
            }
        }




        private void cboINISelect_LostFocus(object sender, RoutedEventArgs e)
        {
            cboINISelect.IsDropDownOpen = false;

            if (cboINISelect.Text == "") return;
        }


        public string SelectedText
        {
            get => cboINISelect.Text;
            set 
            {
                var prevFocusable = editableTextBox.Focusable;
                editableTextBox.Focusable = false;
                cboINISelect.Text = value;
                ConfirmSelection(cboINISelect.Text);
                editableTextBox.Focusable = prevFocusable;
            }
        }

        private void ConfirmSelection(string text)
        {
            cboINISelect.IsDropDownOpen = false;
            editableTextBox.SelectionStart = editableTextBox.Text.Length;
            editableTextBox.SelectionLength = 0;

            var selectedItem = allItems.FirstOrDefault(item => item.Name == text);
            if (selectedItem != null)
            {
                SetSelectedItemImage(selectedItem.ImagePath);
            }
            else
            {
                SetSelectedItemImage("");
            }

            OnConfirm?.Invoke(this, text); // Fire event with selected text
        }


        private void cboINISelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                var item = (DropdownItem)e.AddedItems[0];
                ConfirmSelection(item.Name); // Fire event when dropdown item is selected
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

        public void SetComboBoxReadOnly(bool isReadOnly)
        {
            this.isReadOnly = isReadOnly;
            Focusable = !isReadOnly;
            IsTabStop = !isReadOnly;
            //cboINISelect.Focusable = !isReadOnly;
            //cboINISelect.Focusable = !isReadOnly;
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
                cboINISelect.IsDropDownOpen = !cboINISelect.IsDropDownOpen;
            }
        }
    }
}
