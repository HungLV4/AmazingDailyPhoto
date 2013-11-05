using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using Telerik.Windows.Controls.SlideView;
using Telerik.Windows.Controls;
using Microsoft.Phone.Info;

namespace Grow_Up.View
{
    public partial class ImageSlideView : UserControl
    {
        public EventHandler EntryDeleted;
        public EventHandler DateThumbnailChanged;
        public EventHandler Saved;
        public EventHandler ImageTaped;

        public ImageSlideView()
        {
            InitializeComponent();
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (EntryDeleted != null)
            {
                EntryDeleted(sender, e);
            }
        }

        private void DateThumbnail_Changed(object sender, RoutedEventArgs e)
        {
            if (DateThumbnailChanged != null)
            {
                DateThumbnailChanged(sender, e);
            }
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (Saved != null)
            {
                Saved(sender, e);
            }
        }

        private void Image_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            e.Handled = true;
            if (ImageTaped != null)
            {
                ImageTaped(sender, e);
            }
        }        
    }
}
