using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OceanyaClient.Converters
{
    // New converter to handle boolean to visibility with optional inversion
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter != null && parameter.ToString() == "Inverse";
            bool boolValue = (bool)value;

            if (invert)
                boolValue = !boolValue;

            return boolValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}