using System.Globalization;
using SmartHome2.Resources.Strings;

namespace SmartHome2.Utils
{
    public class BoolToRealtimeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 1)
                return "Toggle";

            if (values[0] is bool isRealtime)
            {
                var loc = AppResources.Instance;
                return isRealtime ? loc.SwitchToHistory : loc.SwitchToRealtime;
            }
            return "Toggle";
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}