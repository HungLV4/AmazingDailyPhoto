using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Commons
{
    public class ResourceHelper
    {
        public static WriteableBitmap LoadBitmap(string fileName)
        {
            Stream maskStream = Application.GetResourceStream(new Uri("Assets/" + fileName, UriKind.Relative)).Stream;
            BitmapImage maskImage = new BitmapImage();
            maskImage.SetSource(maskStream);
            return new WriteableBitmap(maskImage);
        }
    }
}
