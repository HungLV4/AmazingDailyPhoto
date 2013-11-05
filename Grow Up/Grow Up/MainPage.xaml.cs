using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Grow_Up.Resources;
using Grow_Up.ViewModel;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.IO;
using Microsoft.Xna.Framework.Media;
using Coding4Fun.Toolkit.Controls;
using Telerik.Windows.Controls;
using System.ComponentModel;
using Commons;
using Grow_Up.Model;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Grow_Up.View;
using System.Windows.Threading;
using Microsoft.Phone.Info;
using System.Runtime.InteropServices.WindowsRuntime;
using Grow_Up.Helpers;
using ExifLib;
using Models;

namespace Grow_Up
{
    public partial class MainPage : PhoneApplicationPage
    {
        int photoIndex = -1;
        RAJPEGDecoder decoder = new RAJPEGDecoder();

        public MainPage()
        {
            InitializeComponent();

            DataContext = App.ViewModelData;

            //Loaded += (s, e) =>
            //{
            //    DispatcherTimer timer = new DispatcherTimer();
            //    timer.Tick += (ss, ee) =>
            //    {
            //        const string current = "ApplicationCurrentMemoryUsage";
            //        var currentBytes = ((long)DeviceExtendedProperties.GetValue(current)) / 1024.0 / 1024.0;
            //        System.Diagnostics.Debug.WriteLine(String.Format("Memory  = {0,5:F} MB / {1,5:F} MB\n", currentBytes, DeviceStatus.ApplicationMemoryUsageLimit / 1024 / 1024));
            //    };
            //    timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            //    timer.Start();
            //};
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string lockscreenKey = "WallpaperSettings";
            string lockscreenValue = "0";
            bool lockscreenValueExists = NavigationContext.QueryString.TryGetValue(lockscreenKey, out lockscreenValue);
            if (lockscreenValueExists)
            {
                MessageBox.Show("This app is set as lockscreen background provider.");
                SettingsPage.StartAgent();
            }

            CalendarViewOnPage.SelectedDateChanged += CalendarView_SelectionChanged;
            CalendarViewOnPage.Calendar.SelectedDate = DateTime.Now;
            CalendarViewOnPage.Calendar.Refresh();

            DateViewOnPage.EntryDeleted += EntryDeleted;
            DateViewOnPage.DateThumbnailChanged += DateThumbnalChanged;
            DateViewOnPage.Saved += CalendarImageSaved;
            DateViewOnPage.ImageTaped += ViewImage;

            this.Loaded += MainPage_Loaded;
        }

        void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            decoder.Output = PanZoomImageViewer;

            var askforReview = (bool)IsolatedStorageSettings.ApplicationSettings["askforreview"];
            if (askforReview)
            {
                //make sure we only ask once! 
                IsolatedStorageSettings.ApplicationSettings["askforreview"] = false;
                var returnvalue = MessageBox.Show("Thank you for using Amazing Daily Photo for a while now, would you like to review this app?", "Please review my app", MessageBoxButton.OKCancel);
                if (returnvalue == MessageBoxResult.OK)
                {
                    var marketplaceReviewTask = new MarketplaceReviewTask();
                    marketplaceReviewTask.Show();
                }
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            CalendarViewOnPage.SelectedDateChanged -= CalendarView_SelectionChanged;
            DateViewOnPage.EntryDeleted -= EntryDeleted;
            DateViewOnPage.DateThumbnailChanged -= DateThumbnalChanged;
            DateViewOnPage.Saved -= CalendarImageSaved;
            DateViewOnPage.ImageTaped -= ViewImage;

            this.Loaded -= MainPage_Loaded;

            base.OnNavigatedFrom(e);
        }

        protected override void OnRemovedFromJournal(JournalEntryRemovedEventArgs e)
        {
            DateViewOnPage.SlideView.ItemsSource = null;

            base.OnRemovedFromJournal(e);
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            base.OnBackKeyPress(e);

            if (PhotoViewer.IsOpen)
            {
                PhotoViewer.IsOpen = false;
                GC.Collect();
                e.Cancel = true;
            }
        }

        private void CalendarImageSaved(object sender, EventArgs e)
        {
            Entry entry = (Entry)(sender as MenuItem).DataContext;
            if (entry == null) return;

            BackgroundWorker saveImageWorker = new BackgroundWorker();
            saveImageWorker.RunWorkerCompleted += saveImageWorker_RunWorkerCompleted;
            saveImageWorker.DoWork += saveImageWorker_DoWork;
            saveImageWorker.RunWorkerAsync(entry);
        }

        void saveImageWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var entry = e.Argument as Entry;
            using (var iso = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (iso.FileExists(entry.ImgSrc))
                {
                    var stream = iso.OpenFile(entry.ImgSrc, FileMode.Open, FileAccess.Read);
                    using (MediaLibrary library = new MediaLibrary())
                    {
                        library.SavePictureToCameraRoll(entry.ImgSrc, stream);
                    }
                    stream.Close();
                }
            }
        }

        void saveImageWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            ToastPrompt toast = new ToastPrompt()
            {
                Message = Constant.MSG_IMAGE_SAVED
            };
            toast.Show();
        }

        private void DateThumbnalChanged(object sender, EventArgs e)
        {
            Entry entry = (Entry)(sender as MenuItem).DataContext;
            if (entry == null) return;

            entry.Date.ThumbnailEntryId = entry.Id;

            CalendarViewOnPage.Calendar.Refresh();
        }

        private void EntryDeleted(object sender, EventArgs e)
        {
            Entry entry = (Entry)(sender as MenuItem).DataContext;
            if (entry == null) return;

            BackgroundWorker deleteImageWorker = new BackgroundWorker();
            deleteImageWorker.RunWorkerCompleted += deleteImageWorker_RunWorkerCompleted;
            deleteImageWorker.DoWork += deleteImageWorker_DoWork;
            deleteImageWorker.RunWorkerAsync(entry);
        }

        private void deleteImageWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var entity = e.Argument as Entry;

            App.ViewModelData.AllEntries.Remove(entity);
            // delete image in isolate storage
            using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (entity.ImgSrc != null && isolatedStorage.FileExists(entity.ImgSrc))
                {
                    isolatedStorage.DeleteFile(entity.ImgSrc);
                }
            }

            Dispatcher.BeginInvoke(() =>
            {
                App.ViewModelData.Db.Entries.DeleteOnSubmit(entity);
                App.ViewModelData.Db.SubmitChanges();
            });
        }

        private void deleteImageWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CalendarViewOnPage.Calendar.Refresh();
        }

        private void ViewImage(object sender, EventArgs e)
        {
            var entry = (Entry)(sender as Image).DataContext;
            if (entry == null) return;

            PhotoViewer.IsOpen = true;
            decoder.Input = entry.ImageStream;
        }

        private void CalendarView_SelectionChanged(object sender, EventArgs e)
        {
            DateTime selectedDate = CalendarViewOnPage.Calendar.SelectedDate.Date;
            App.ViewModelData.SelectedDateData = App.ViewModelData.AllDates.Where(date => date.CalendarItemDate.Equals(selectedDate)).FirstOrDefault();
        }

        private void Add_Btn_Click(object sender, EventArgs e)
        {
            SetScreenButtonsEnabled(false);
            //photoIndex = App.ViewModelData.AllDates.ToList().FindIndex(date => date.CalendarItemDate.Equals(CalendarViewOnPage.Calendar.SelectedDate.Date));
            photoIndex = App.ViewModelData.AllDates.ToList().FindIndex(date => date.CalendarItemDate.Equals(DateTime.Now.Date));
            if (photoIndex == -1)
            {
                // creat new date data
                DateData today = new DateData()
                {
                    CalendarItemDate = DateTime.Now.Date
                    //CalendarItemDate = CalendarViewOnPage.Calendar.SelectedDate.Date
                };

                App.ViewModelData.AddDateData(today);
                App.ViewModelData.SaveChangesToDb();

                photoIndex = App.ViewModelData.AllDates.Count - 1;
            }

            Uri uri = new Uri(String.Format("/PhotoCapturePage.xaml?index={0}", photoIndex), UriKind.Relative);
            SetScreenButtonsEnabled(true);
            NavigationService.Navigate(uri);
        }

        private void Generate_Btn_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/CalendarGenerationPage.xaml", UriKind.Relative));
        }

        private void Setting_Btn_Click(object sender, EventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void SetScreenButtonsEnabled(bool enabled)
        {
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }
        }

        private void SearchAutoCompleteBox_SuggestionSelected(object sender, SuggestionSelectedEventArgs e)
        {
            var entry = e.SelectedSuggestion as Entry;
            if (entry != null)
            {
                CalendarViewOnPage.Calendar.SelectedDate = entry.Time.Date;
                CalendarViewOnPage.Calendar.SelectedMonth = entry.Time.Month;
                CalendarViewOnPage.Calendar.SelectedYear = entry.Time.Year;
                CalendarViewOnPage.Calendar.Refresh();

                slidePanorama(PanoramaRoot);

                DateViewOnPage.SlideView.SelectedItem = entry;
            }
        }

        private void slidePanorama(Panorama pan)
        {
            //System.Diagnostics.Debug.WriteLine(CalendarViewOnPage.Calendar.SelectedMonth);

            FrameworkElement panWrapper = VisualTreeHelper.GetChild(pan, 0) as FrameworkElement;
            FrameworkElement panTitle = VisualTreeHelper.GetChild(panWrapper, 1) as FrameworkElement;

            //Get the panorama layer to calculate all panorama items size 
            FrameworkElement panLayer = VisualTreeHelper.GetChild(panWrapper, 2) as FrameworkElement;

            //Get the title presenter to calculate the title size
            FrameworkElement panTitlePresenter = VisualTreeHelper.GetChild(VisualTreeHelper.GetChild(panTitle, 0) as FrameworkElement, 1) as FrameworkElement;

            //Current panorama item index
            int curIndex = pan.SelectedIndex;

            //Get the next of next panorama item 
            FrameworkElement third = VisualTreeHelper.GetChild(pan.Items[(curIndex + 2) % pan.Items.Count] as PanoramaItem, 0) as FrameworkElement;

            //Be sure the RenderTransform is TranslateTransform 
            if (!(pan.RenderTransform is TranslateTransform) || !(panTitle.RenderTransform is TranslateTransform))
            {
                pan.RenderTransform = new TranslateTransform();
                panTitle.RenderTransform = new TranslateTransform();
            }

            //Increase width of panorama to let it render the next slide (if not, default panorama is 480px and the null area appear if we transform it) 
            pan.Width = 960;

            //Animate panorama control to the right   
            Storyboard sb = new Storyboard();
            DoubleAnimation a = new DoubleAnimation();
            a.From = 0;
            a.To = -(pan.Items[curIndex] as PanoramaItem).ActualWidth;
            //Animate the x transform to a width of one item  
            a.Duration = new Duration(TimeSpan.FromMilliseconds(700));
            a.EasingFunction = new CircleEase();
            //This is default panorama easing effect  
            sb.Children.Add(a);
            Storyboard.SetTarget(a, pan.RenderTransform);
            Storyboard.SetTargetProperty(a, new PropertyPath(TranslateTransform.XProperty));

            //Animate panorama title separately 
            DoubleAnimation aTitle = new DoubleAnimation();
            aTitle.From = 0;
            aTitle.To = (panLayer.ActualWidth - panTitlePresenter.ActualWidth) / (pan.Items.Count - 1) * 1.5;
            //Calculate where should the title animate to 
            aTitle.Duration = a.Duration;
            aTitle.EasingFunction = a.EasingFunction;
            //This is default panorama easing effect   
            sb.Children.Add(aTitle);
            Storyboard.SetTarget(aTitle, panTitle.RenderTransform);
            Storyboard.SetTargetProperty(aTitle, new PropertyPath(TranslateTransform.XProperty));

            //Start the effect  
            sb.Begin();
            //After effect completed, we change the selected item  
            a.Completed += (obj, args) =>
            {
                //Reset panorama width       
                pan.Width = 480;
                //Change the selected item       
                (pan.Items[curIndex] as PanoramaItem).Visibility = Visibility.Collapsed;
                pan.SetValue(Panorama.SelectedItemProperty, pan.Items[(curIndex + 1) % pan.Items.Count]);
                pan.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                (pan.Items[curIndex] as PanoramaItem).Visibility = Visibility.Visible;
                //Reset panorama render transform   
                (pan.RenderTransform as TranslateTransform).X = 0;
                //Reset title render transform      
                (panTitle.RenderTransform as TranslateTransform).X = 0;
                //Because of the next of next item will be load after we change the selected index to next item  
                //I do not want it appear immediately without any effect, so I create a custom effect for it   
                if (!(third.RenderTransform is TranslateTransform))
                {
                    third.RenderTransform = new TranslateTransform();
                }
                Storyboard sb2 = new Storyboard();
                DoubleAnimation aThird = new DoubleAnimation()
                {
                    From = 100,
                    To = 0,
                    Duration = new Duration(TimeSpan.FromMilliseconds(300))
                };
                sb2.Children.Add(aThird);
                Storyboard.SetTarget(aThird, third.RenderTransform);
                Storyboard.SetTargetProperty(aThird, new PropertyPath(TranslateTransform.XProperty));
                sb2.Begin();
            };
        }

        //private void ImportBtn_Click(object sender, EventArgs e)
        //{
        //    MediaLibrary library = new MediaLibrary();
        //    foreach (var pic in library.Pictures)
        //    {
        //        ExifReader reader = null;
        //        try
        //        {
        //            reader = new ExifReader(pic.GetImage());
        //            DateTime capturedDate;
        //            reader.GetTagValue(ExifTags.DateTime, out capturedDate);
        //        }
        //        catch (ExifLibException) { System.Diagnostics.Debug.WriteLine("exception everywhere!"); }
        //        finally
        //        {
        //            if (reader != null) reader.Dispose();
        //        }
        //    }
        //}
    }
}