using CreateKnxProd.Model;
using CreateKnxProd.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

namespace CreateKnxProd.Converter
{
    public class EnableTBoolTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(bool?))
            {
                var enable = value as Enable_T?;
                if (enable == Enable_T.Enabled)
                    return true;

                return false;
            }

            if (targetType == typeof(Enable_T))
            {
                var isEnabled = value as bool?;
                if (isEnabled == true)
                    return Enable_T.Enabled;
                return Enable_T.Disabled;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Convert(value, targetType, parameter, culture);
        }
    }
}