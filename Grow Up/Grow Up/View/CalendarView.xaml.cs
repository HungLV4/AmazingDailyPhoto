using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Grow_Up.View
{
    public partial class CalendarView : UserControl
    {
        public EventHandler SelectedDateChanged;

        public CalendarView()
        {
            InitializeComponent();
        }

        private void CalendarView_SelectionChanged(object sender, WPControls.SelectionChangedEventArgs e)
        {
            if (SelectedDateChanged != null)
            {
                SelectedDateChanged(sender, e);
            }
        }
    }
}
