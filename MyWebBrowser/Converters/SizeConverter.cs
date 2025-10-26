using System;
using System.Globalization;
using System.Windows.Data;

namespace MyWebBrowser
{
    public class SizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long sizeInBytes)
            {
                if (sizeInBytes >= 1024 * 1024)
                    return $"{sizeInBytes / (1024 * 1024.0):F2} MB";
                if (sizeInBytes >= 1024)
                    return $"{sizeInBytes / 1024.0:F2} KB";
                return $"{sizeInBytes} Bytes";
            }
            return "Unknown Size";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}