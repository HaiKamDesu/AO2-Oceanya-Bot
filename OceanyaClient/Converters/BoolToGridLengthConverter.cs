using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace OceanyaClient.Converters
{
    // New converter to handle boolean to grid length
    public class BoolToGridLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length < 2 || !(values[0] is bool) || !(values[1] is string))
                return new GridLength(0);

            bool showImage = (bool)values[0];
            int minWidth;

            // Try to parse the minWidth parameter
            if (!int.TryParse((string)values[1], out minWidth))
                minWidth = 20; // Default min width

            if (showImage)
                return new GridLength(minWidth, GridUnitType.Auto);
            else
                return new GridLength(0);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}