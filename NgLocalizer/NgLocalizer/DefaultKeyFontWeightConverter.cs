using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace NgLocalizer
{
    public class DefaultKeyFontWeightConverter
    : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool) value)
            {
                return FontWeights.Normal;
            }

            return FontWeights.Bold;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
