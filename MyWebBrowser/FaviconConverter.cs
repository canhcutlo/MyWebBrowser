using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace MyWebBrowser
{
    public class FaviconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string url && Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return $"{uri.Scheme}://{uri.Host}/favicon.ico";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}