using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace VoidStrike.Converters
{
    public class EqualValueToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;

            string viewName = value.ToString();
            string targetView = parameter.ToString();

            // Handle Active Tag for Styles
            if (targetView.EndsWith("_Tag"))
            {
                string cleanTarget = targetView.Replace("_Tag", "");
                return viewName == cleanTarget ? "Active" : "Inactive";
            }

            return viewName == targetView ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
