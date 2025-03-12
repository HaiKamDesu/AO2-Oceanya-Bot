using System;
using System.Globalization;
using System.Windows.Data;

namespace OceanyaClient.Converters
{
    public class WidthAdjustConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width - 35; // Adjust 35px for the dropdown button and padding
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
