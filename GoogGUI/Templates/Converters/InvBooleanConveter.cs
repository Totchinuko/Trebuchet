using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace GoogGUI.Templates.Converters
{
    public class InvBooleanConveter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value is bool boolean)
            {
                return !boolean;
            }
            else
            {
                throw new ArgumentException("Value must be a boolean", nameof(value));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolean)
            {
                return !boolean;
            }
            else
            {
                throw new ArgumentException("Value must be a boolean", nameof(value));
            }
        }
    }
}
