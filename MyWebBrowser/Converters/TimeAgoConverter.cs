using System;
using System.Globalization;
using System.Windows.Data;

namespace MyWebBrowser
{
    public class TimeAgoConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DateTime dateTime)
            {
                var timeSpan = DateTime.Now - dateTime;
                if (timeSpan.TotalDays >= 1)
                    return $"{(int)timeSpan.TotalDays} days ago";
                if (timeSpan.TotalHours >= 1)
                    return $"{(int)timeSpan.TotalHours} hours ago";
                if (timeSpan.TotalMinutes >= 1)
                    return $"{(int)timeSpan.TotalMinutes} minutes ago";
                return "Just now";
            }
            return "Unknown Time";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}