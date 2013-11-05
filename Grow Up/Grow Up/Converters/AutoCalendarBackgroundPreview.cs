using Commons;
using Grow_Up.Helpers;
using Microsoft.Phone;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Grow_Up.Converters
{
    public class AutoCalendarBackgroundPreview : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            int index = (int)value;

            using (var stream = Application.GetResourceStream(new Uri(String.Format("Assets/CalendarTemplate/{0}.jpg", index), UriKind.Relative)).Stream)
            {
                return PictureDecoder.DecodeJpeg(stream, ResolutionHelper.GetScreenWidth() / 2, ResolutionHelper.GetScreenHeight() / 2);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
