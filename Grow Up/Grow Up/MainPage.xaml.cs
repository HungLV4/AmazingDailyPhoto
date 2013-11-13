//#define DEBUG_AGENT

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
using Grow_Up.Framework;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Microsoft.Live;

namespace Grow_Up
{
    public partial class MainPage : PhoneApplicationPage
    {
        bool _isMenuOpen = false;
        FrameworkElement _contentPanel;

        RAJPEGDecoder _decoder;

        public MainPage()
        {
            InitializeComponent();

            DataContext = App.ViewModelData;

            _contentPanel = this.ContentPanel as FrameworkElement;
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
            var askforReview = (bool)IsolatedStorageSettings.ApplicationSettings["askforreview"];
            if (askforReview)
            {
                //make sure we only ask once! 
                IsolatedStorageSettings.ApplicationSettings["askforreview"] = false;
                var returnvalue = MessageBox.Show(AppResources.TextID20, AppResources.TextID21, MessageBoxButton.OKCancel);
                if (returnvalue == MessageBoxResult.OK)
                {
                    var marketplaceReviewTask = new MarketplaceReviewTask();
                    marketplaceReviewTask.Show();
                }
            }

#if DEBUG_AGENT
            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (ss, ee) =>
            {
                const string current = "ApplicationCurrentMemoryUsage";
                var currentBytes = ((long)DeviceExtendedProperties.GetValue(current)) / 1024.0 / 1024.0;
                System.Diagnostics.Debug.WriteLine(String.Format("Memory  = {0,5:F} MB / {1,5:F} MB\n", currentBytes, DeviceStatus.ApplicationMemoryUsageLimit / 1024 / 1024));
            };
            timer.Interval = new TimeSpan(0, 0, 0, 1, 0);
            timer.Start();
#endif
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
                ContentPanel.IsHitTestVisible = true;
                _decoder.Dispose();
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
            OSHelper.ShowToast(AppResources.TextID19);
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
            ContentPanel.IsHitTestVisible = false;

            if (entry.Note.Equals(String.Empty) || entry.Note.Equals("")) entry.Note = AppResources.TextID27;
            if (entry.Location.Equals(String.Empty) || entry.Location.Equals("")) entry.Location = AppResources.TextID28;

            App.ViewModelData.SelectedEntry = entry;

            _decoder = new RAJPEGDecoder();
            _decoder.Output = PanZoomImageViewer;
            _decoder.Input = entry.ImageStream;

        }

        private void CalendarView_SelectionChanged(object sender, EventArgs e)
        {
            DateTime selectedDate = CalendarViewOnPage.Calendar.SelectedDate.Date;
            App.ViewModelData.SelectedDateData = App.ViewModelData.AllDates.Where(date => date.CalendarItemDate.Equals(selectedDate)).FirstOrDefault();
        }

        private void SetScreenButtonsHitVisible(bool enable)
        {
            this.CalendarViewOnPage.IsHitTestVisible = enable;
            this.DateViewOnPage.IsHitTestVisible = enable;
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

                // close menu
                if (_isMenuOpen) CloseSettings();

                DateViewOnPage.SlideView.SelectedItem = entry;
            }
        }

        private void Setting_Btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void Generate_Btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/CalendarGenerationPage.xaml", UriKind.Relative));
        }

        #region slide menu gesture
        private void GestureListener_DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            var position = e.GetPosition(this.DateViewOnPage);
            if (position.X > 0 && position.Y > 0
                && position.X < this.DateViewOnPage.ActualWidth
                && position.Y < this.DateViewOnPage.ActualHeight
                && App.ViewModelData.SelectedDateData != null
                && App.ViewModelData.SelectedDateData.Entries.Count > 1)
            {
                return;
            }

            if (e.Direction == System.Windows.Controls.Orientation.Horizontal && e.HorizontalChange > 0)
            {
                double offset = _contentPanel.GetHorizontalOffset().Value + e.HorizontalChange;
                if (offset > Constant.DISTANCE_DRAG_DELTA_2_OPEN && !_isMenuOpen)
                {
                    OpenSettings();
                }
                else if (offset > Constant.DISTANCE_DRAG_DELTA_2_OPEN && _isMenuOpen)
                {

                }
                else
                {
                    _contentPanel.SetHorizontalOffset(offset);
                }
            }
            else if (e.Direction == System.Windows.Controls.Orientation.Horizontal && e.HorizontalChange < 0)
            {
                double offset = _contentPanel.GetHorizontalOffset().Value + e.HorizontalChange;
                if (offset < Constant.DISTANCE_DRAG_DELTA_2_CLOSE && _isMenuOpen)
                {
                    CloseSettings();
                }
                else if (offset < Constant.DISTANCE_DRAG_DELTA_2_CLOSE && !_isMenuOpen) { }
                else
                {
                    _contentPanel.SetHorizontalOffset(offset);
                }
            }
        }

        private void GestureListener_DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            var position = e.GetPosition(CalendarViewOnPage);
            if (position.X > 0 && position.Y > 0
                && position.X < CalendarViewOnPage.ActualWidth
                && position.Y < CalendarViewOnPage.ActualHeight
                && e.Direction == System.Windows.Controls.Orientation.Vertical)
            {
                if (e.VerticalChange > 0) CalendarViewOnPage.Calendar.IncrementMonth();
                else CalendarViewOnPage.Calendar.DecrementMonth();
            }

            double offset = _contentPanel.GetHorizontalOffset().Value;
            if (offset > Constant.DISTANCE_DRAG_COMPLETED_2_OPENCLOSE && !_isMenuOpen) OpenSettings();
            else if (offset <= Constant.DISTANCE_DRAG_COMPLETED_2_OPENCLOSE && _isMenuOpen) CloseSettings();
            else ResetLayoutRoot();
        }

        private void CloseSettings()
        {
            var trans = _contentPanel.GetHorizontalOffset().Transform;
            trans.Animate(trans.X, 0, TranslateTransform.XProperty, 300, 0, new QuadraticEase
            {
                EasingMode = EasingMode.EaseOut
            });

            _isMenuOpen = false;
            SetScreenButtonsHitVisible(true);
        }

        private void OpenSettings()
        {
            var trans = _contentPanel.GetHorizontalOffset().Transform;
            trans.Animate(trans.X, 380, TranslateTransform.XProperty, 300, 0, new QuadraticEase
            {
                EasingMode = EasingMode.EaseOut
            });

            _isMenuOpen = true;
            SetScreenButtonsHitVisible(false);
        }

        private void ResetLayoutRoot()
        {
            if (!_isMenuOpen)
                CloseSettings();
            else
                OpenSettings();
        }
        #endregion

        private void CameraTakeBtn_Click(object sender, RoutedEventArgs e)
        {
            int photoIndex = App.ViewModelData.AllDates.ToList().FindIndex(date => date.CalendarItemDate.Equals(DateTime.Now.Date));
            if (photoIndex == -1)
            {
                // creat new date data
                DateData today = new DateData()
                {
                    CalendarItemDate = DateTime.Now.Date
                };

                App.ViewModelData.AddDateData(today);
                App.ViewModelData.SaveChangesToDb();

                photoIndex = App.ViewModelData.AllDates.Count - 1;
            }

            Uri uri = new Uri(String.Format("/PhotoCapturePage.xaml?index={0}", photoIndex), UriKind.Relative);
            NavigationService.Navigate(uri);
        }

        private void ChoosePhotoBtn_Click(object sender, RoutedEventArgs e)
        {
            PhotoChooserTask photoTask = new PhotoChooserTask();
            photoTask.Completed += photoTask_Completed;
            photoTask.Show();
        }

        void photoTask_Completed(object sender, PhotoResult e)
        {
            if (e.TaskResult == TaskResult.OK && e.Error == null)
            {
                var photoStream = new MemoryStream();
                var thumbnailStream = new MemoryStream();
                var library = new MediaLibrary();
                var dateTaken = DateTime.Now;

                bool found = false;
                foreach (var pic in library.Pictures)
                {
                    if (e.OriginalFileName.Equals(MediaLibraryExtensions.GetPath(pic)))
                    {
                        pic.GetImage().CopyTo(photoStream);
                        photoStream.Seek(0, SeekOrigin.Begin);

                        pic.GetPreviewImage().CopyTo(thumbnailStream);
                        thumbnailStream.Seek(0, SeekOrigin.Begin);

                        dateTaken = pic.Date;

                        NokiaImaginHelper.PreparePhoto(photoStream, thumbnailStream);
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    NavigationService.Navigate(new Uri(String.Format("/FilterPreviewPage.xaml?dateTaken={0}&shouldCrop={1}", dateTaken.Ticks, Constant.NORMAL_MODE), UriKind.Relative));
                }
            }
        }

        private void skydrive_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e != null && e.Status == LiveConnectSessionStatus.Connected)
            {
                App.ViewModelData.LiveClient = new LiveConnectClient(e.Session);
                UploadBtn.IsEnabled = true;
            }
            else
            {
                App.ViewModelData.LiveClient = null;
                if(e.Error != null)
                    OSHelper.ShowToast(AppResources.TextID31);
            }
        }

        #region currently not used functions
        private async void GetAccountPicture()
        {
            try
            {
                LiveOperationResult operationResult = await App.ViewModelData.LiveClient.GetAsync("me/picture");
                var jsonResult = operationResult.Result as dynamic;
                string location = jsonResult.location ?? string.Empty;
                if (!location.Equals(string.Empty))
                {
                    ImageProfilePicture.Source = new BitmapImage(new Uri(location, UriKind.Absolute));
                }
            }
            catch (Exception e)
            {

            }
        }
        #endregion

        private void Upload_Btn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Uri("/UploadPage.xaml", UriKind.Relative));
        }

        private void Menu_Btn_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_isMenuOpen) CloseSettings();
            else OpenSettings();
        }
    }
}