
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Grow_Up.Commands;

namespace Grow_Up.BindableCommand
{
    public class CommandSampleViewModel
    {
        public ICommand RateThisAppCommand
        {
            get;
            private set;
        }

        public ICommand SendAnEmailCommand
        {
            get;
            private set;
        }

        public ICommand OpenLockscreenCommand
        {
            get;
            private set;
        }

        public CommandSampleViewModel()
        {
            RateThisAppCommand = new RateThisAppCommand();
            SendAnEmailCommand = new SendAnEmailCommand();
            OpenLockscreenCommand = new OpenLockscreenSettingCommand();
        }
    }
}
