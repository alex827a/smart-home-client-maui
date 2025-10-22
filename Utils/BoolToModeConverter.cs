using System.Globalization;
using SmartHome2.Resources.Strings;

namespace SmartHome2.Utils
{
    public class BoolToModeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return "";

            if (values[0] is bool canToggle)
            {
                var loc = AppResources.Instance;
                return canToggle ? loc.CanToggleDevices : loc.ViewOnly;
            }
            return "";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}