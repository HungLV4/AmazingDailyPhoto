using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Grow_Up.Model;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.ComponentModel;

namespace Grow_Up
{
    public partial class GifGeneratorPage : PhoneApplicationPage
    {
        public GifGeneratorPage()
        {
            InitializeComponent();

            PhotoViewOnPage.DataContext = this;
        }

        public List<Helpers.KeyedList<DateTime, Entry>> GroupedEntries
        {
            get
            {
                var groupedEntries = from e in App.ViewModelData.AllEntries
                                     group e by e.Date.CalendarItemDate into entriesByDate
                                     select new Helpers.KeyedList<DateTime, Entry>(entriesByDate);

                return new List<Helpers.KeyedList<DateTime, Entry>>(groupedEntries);
            }
        }

        private void ProcessBtn_Click(object sender, EventArgs e)
        {
            var selectedItems = PhotoViewOnPage.PhotosHub.SelectedItems;
            List<WriteableBitmap> frames = new List<WriteableBitmap>();
            foreach (var entry in selectedItems)
            {
                WriteableBitmap wb = new WriteableBitmap((entry as Entry).ThumbImage);
                frames.Add(wb);
            }

            BackgroundWorker gifGeneratorWorker = new BackgroundWorker();
            gifGeneratorWorker.DoWork += gifGeneratorWorker_DoWork;
            gifGeneratorWorker.ProgressChanged += gifGeneratorWorker_ProgressChanged;
            gifGeneratorWorker.RunWorkerCompleted += gifGeneratorWorker_RunWorkerCompleted;
            gifGeneratorWorker.RunWorkerAsync(frames);
        }

        void gifGeneratorWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var frames = e.Argument as List<WriteableBitmap>;
            
            AnimatedGifEncoder encoder = new AnimatedGifEncoder();
            encoder.SetDelay(1000);
            encoder.SetRepeat(0);
            encoder.SetTransparent(Colors.Transparent);
            foreach (var frame in frames)
            {
                encoder.AddFrame(frame);
            }
        }

        void gifGeneratorWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        void gifGeneratorWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

        }

    }
}