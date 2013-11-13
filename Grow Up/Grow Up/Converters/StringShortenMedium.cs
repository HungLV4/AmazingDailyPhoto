using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Grow_Up.Converters
{
    public class StringShortenMedium : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return String.Empty;

            var content = value as String;
            if (content.Length >= 70)
            {
                content = content.Substring(0, 70);
                content = String.Concat(content, "...");
            }
            return content;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
