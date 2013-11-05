using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Grow_Up.Helpers
{
    public class ResolutionHelper
    {
        public static int GetScreenWidth()
        {
            if (Environment.OSVersion.Version.Major < 8)
            {
                return 480;
            }
            int scaleFactor = (int)GetProperty(System.Windows.Application.Current.Host.Content, "ScaleFactor");
            switch (scaleFactor)
            {
                case 100:
                    return 480;
                case 160:
                    return 768;
                case 150:
                    return 720;
                default:
                    return 480;
            }
        }

        public static int GetScreenHeight()
        {
            if (Environment.OSVersion.Version.Major < 8)
            {
                return 800;
            }
            int scaleFactor = (int)GetProperty(System.Windows.Application.Current.Host.Content, "ScaleFactor");
            switch (scaleFactor)
            {
                case 100:
                    return 800;
                case 160:
                    return 1280;
                case 150:
                    return 1280;
                default:
                    return 800;
            }
        }

        private static object GetProperty(object instance, string name)
        {
            var getMethod = instance.GetType().GetProperty(name).GetGetMethod();
            return getMethod.Invoke(instance, null);
        }
    }
}
