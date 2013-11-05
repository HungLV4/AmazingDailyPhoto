using Microsoft.Phone.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grow_Up.View
{
    public class PanoramaFullScreen : Panorama
    {
        protected override System.Windows.Size MeasureOverride(System.Windows.Size availableSize)
        {
            availableSize.Width += 48;
            return base.MeasureOverride(availableSize);
        }
    }
}
