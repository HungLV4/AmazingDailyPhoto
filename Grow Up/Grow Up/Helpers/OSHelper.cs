using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Phone.System.UserProfile;

namespace Grow_Up.Helpers
{
    public class OSHelper
    {
        public static void RemoveAllBackStackButFirst()
        {
            int length = App.RootFrame.BackStack.ToList().Count();
            for (int i = 0; i < length - 1; i++)
            {
                App.RootFrame.RemoveBackEntry();
            }
        }
    }
}
