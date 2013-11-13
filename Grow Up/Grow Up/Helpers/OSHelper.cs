using Coding4Fun.Toolkit.Controls;
using Commons;
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

        public static void ShowToast(string message)
        {
            ToastPrompt toast = new ToastPrompt()
            {
                Message = message,
                MillisecondsUntilHidden = Constant.TOAST_TIME_IN_MILISECOND
            };
            toast.Show();
        }
    }
}
