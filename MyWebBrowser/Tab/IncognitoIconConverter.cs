using System;
using System.Globalization;
using System.Windows.Data;
namespace MyWebBrowser
{
    public class IncognitoIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isIncognito = value is bool b && b;
            return isIncognito ? "/Icon/Incognito.png" : "/Icon/Webbrowser.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}