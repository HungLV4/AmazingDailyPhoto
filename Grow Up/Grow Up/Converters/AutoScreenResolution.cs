using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Grow_Up.Converters
{
    public class AutoScreenResolution
    {
        public static DependencyProperty AutoResProperty = DependencyProperty.RegisterAttached(
                                                                                    "AutoRes",
                                                                                    typeof(double),
                                                                                    typeof(AutoScreenResolution),
                                                                                    new PropertyMetadata(OnAutoResChanged));

        public static double GetAutoRes(Image image)
        {
            if (null == image)
            {
                throw new ArgumentNullException("image");
            }
            return (double)image.GetValue(AutoResProperty);
        }

        public static void SetAutoRes(Image image, double value)
        {
            if (null == image)
            {
                throw new ArgumentNullException("image");
            }
            image.SetValue(AutoResProperty, value);
        }

        private static void OnAutoResChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as Image).Width = Grow_Up.Helpers.ResolutionHelper.GetScreenWidth();
            (d as Image).Height = Grow_Up.Helpers.ResolutionHelper.GetScreenHeight();
        }
    }
}
