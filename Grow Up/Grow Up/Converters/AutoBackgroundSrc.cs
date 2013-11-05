using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Grow_Up.Converters
{
    public class AutoBackgroundSrc : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null) return null;
            int index = (int)value;

            ImageBrush background = new ImageBrush()
            {
                ImageSource = new BitmapImage(new Uri(String.Format("/Assets/Background/{0}.jpg", index), UriKind.Relative)),
                Stretch = Stretch.UniformToFill
            };
            return background;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
