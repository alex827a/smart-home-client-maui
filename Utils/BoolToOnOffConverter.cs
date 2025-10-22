using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using SmartHome2.Resources.Strings;

namespace SmartHome2.Utils
{
    public class BoolToOnOffConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is bool isOn)
            {
                var loc = AppResources.Instance;
                return isOn ? loc.On : loc.Off;
            }
            return "";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}