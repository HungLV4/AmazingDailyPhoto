using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Grow_Up.Helpers;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows.Input;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Net.NetworkInformation;
using System.Device.Location;
using System.Threading.Tasks;
using Coding4Fun.Toolkit.Controls;
using Microsoft.Phone.Maps.Services;
using System.ComponentModel;
using System.Xml.Linq;
using System.Text;
using Models;
using Commons;
using Grow_Up.Model;

namespace Grow_Up
{
    public partial class AddMetaDataPage : PhoneApplicationPage
    {
        int _dateIndex = -1;
        BackgroundWorker _locationWorker;

        public AddMetaDataPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            string index;
            NavigationContext.QueryString.TryGetValue("index", out index);
            try
            {
                if (index != null) _dateIndex = int.Parse(index);
            }
            catch (Exception) { }

            if (_dateIndex == -1)
            {
                OSHelper.RemoveAllBackStackButFirst();
                if (NavigationService.CanGoBack)
                {
                    NavigationService.GoBack();
                }
            }

            this.Loaded += AddMetaDataPage_Loaded;
        }

        void AddMetaDataPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatingLocationAddress();
        }

        private async void Save_Btn_Click(object sender, EventArgs e)
        {
            ProgressIndicator.IsRunning = true;
            SetScreenButtonsEnabled(false);

            //save to isolated storage
            DateTime now = DateTime.Now;
            string photoName = String.Format("{0}.jpg", now.Ticks);
            //string smallThumbName = String.Format("{0}_sthumb.jpg", now.Ticks);
            //string largeThumbName = String.Format("{0}_lthumb.jpg", now.Ticks);

            using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (isolatedStorage.FileExists(photoName))
                {
                    isolatedStorage.DeleteFile(photoName);
                }

                //save photo
                WriteableBitmap photo = new WriteableBitmap((int)App.PhotoModel.Width, (int)App.PhotoModel.Height);
                await App.PhotoModel.RenderBitmapAsync(photo);

                IsolatedStorageFileStream fileStream = isolatedStorage.CreateFile(photoName);
                System.Windows.Media.Imaging.Extensions.SaveJpeg(photo, fileStream, photo.PixelWidth, photo.PixelHeight, 0, Constant.JpegQuality);
                fileStream.Close();

                //save thumbnail
                //WriteableBitmap smallThumbnailPhoto = new WriteableBitmap(Constant.ThumbnailSmallSide, Constant.ThumbnailSmallSide);
                //Bitmap smallThumbnail = await App.ThumbnailModel.RenderThumbnailBitmapAsync(Constant.ThumbnailSmallSide);
                //using (EditingSession session = new EditingSession(smallThumbnail))
                //{
                //    await session.RenderToBitmapAsync(smallThumbnailPhoto.AsBitmap());
                //}
                //IsolatedStorageFileStream smallThumbnailStream = isolatedStorage.CreateFile(smallThumbName);
                //System.Windows.Media.Imaging.Extensions.SaveJpeg(smallThumbnailPhoto, smallThumbnailStream, Constant.ThumbnailSmallSide, Constant.ThumbnailSmallSide, 0, Constant.JpegQuality);
                //smallThumbnailStream.Close();

                //WriteableBitmap largeThumbnailPhoto = new WriteableBitmap(Constant.ThumbnailLargeSide, Constant.ThumbnailLargeSide);
                //Bitmap largeThumbnail = await App.ThumbnailModel.RenderThumbnailBitmapAsync(Constant.ThumbnailLargeSide);
                //using (EditingSession session = new EditingSession(largeThumbnail))
                //{
                //    await session.RenderToBitmapAsync(largeThumbnailPhoto.AsBitmap());
                //}
                //IsolatedStorageFileStream largethumbnailStream = isolatedStorage.CreateFile(largeThumbName);
                //System.Windows.Media.Imaging.Extensions.SaveJpeg(largeThumbnailPhoto, largethumbnailStream, Constant.ThumbnailLargeSide, Constant.ThumbnailLargeSide, 0, Constant.JpegQuality);
                //largethumbnailStream.Close();
            }

            Entry entry = new Entry()
            {
                ImgSrc = photoName,
                //ImgSmallThumbSrc = smallThumbName,
                //ImgLargeThumbSrc = largeThumbName,
                Time = now,
                Date = App.ViewModelData.AllDates[_dateIndex],
                Note = TxtBoxCaption.Text,
                Location = TxtBoxLocation.Text
            };
            App.ViewModelData.AddEntry(entry, _dateIndex);

            ProgressIndicator.IsRunning = false;

            // clean up
            if (App.PhotoModel != null)
            {
                App.PhotoModel.Dispose();
                App.PhotoModel = null;
                GC.Collect();
            }

            if (App.ThumbnailModel != null)
            {
                App.ThumbnailModel.Dispose();
                App.ThumbnailModel = null;
                GC.Collect();
            }

            OSHelper.RemoveAllBackStackButFirst();
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void SetScreenButtonsEnabled(bool enabled)
        {
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }
        }

        private void TxtBoxCaption_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                TxtBoxLocation.Focus();
            }
        }

        private void TxtBoxLocation_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
            {
                Save_Btn_Click(null, null);
            }
        }

        private void UpdatingLocationAddress()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_FIRST_TIME))
            {
                MessageBoxResult result = MessageBox.Show(Constant.MSG_LOCATION_WARNING, Constant.MSG_LOCATION_TITLE, MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    App.ViewModelData.IsLocationOn = true;
                }
                else
                {
                    App.ViewModelData.IsLocationOn = false;
                }

                IsolatedStorageSettings.ApplicationSettings.Add(Constant.SETTING_FIRST_TIME, 1);
                App.ViewModelData.SaveSettings();
            }

            if (App.ViewModelData.IsLocationOn)
            {
                //getting location
                _locationWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };
                _locationWorker.DoWork += locationWorker_DoWork;
                _locationWorker.RunWorkerAsync();
            }
        }

        async void locationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke(() =>
            {
                LocationIndicator.IsRunning = true;
                TxtBoxLocation.IsEnabled = false;
                SetScreenButtonsEnabled(false);
            });

            if (NetworkInterface.NetworkInterfaceType == NetworkInterfaceType.None)
            {
                Dispatcher.BeginInvoke(() =>
                {
                    ToastPrompt toast = new ToastPrompt() { Message = Constant.MSG_NO_INTERNET_CONNECTION };
                    toast.Show();

                    LocationIndicator.IsRunning = false;
                    TxtBoxLocation.IsEnabled = true;
                    SetScreenButtonsEnabled(true);
                });
                return;
            }

            var currentPosition = await GetCurrentLocation();
            if (currentPosition != null)
            {
                //string reverseGeocodingQuery = String.Format(Constant.GOOGLE_REVERSE_GEOCODING_URI, currentPosition.Latitude, currentPosition.Longitude);
                //HttpClient client = new HttpClient()
                //{
                //    Timeout = TimeSpan.FromMilliseconds(5000)
                //};
                //HttpRequestMessage req = new HttpRequestMessage()
                //{
                //    RequestUri = new Uri(reverseGeocodingQuery, UriKind.Absolute)
                //};
                //var res = await client.SendAsync(req);
                //string content = await res.Content.ReadAsStringAsync();
                //string address = ParseGoogleResponse(content);

                Dispatcher.BeginInvoke(async () =>
                {
                    ReverseGeocodeQuery reverseGeocodingQuery = new ReverseGeocodeQuery()
                    {
                        GeoCoordinate = currentPosition
                    };
                    var results = await reverseGeocodingQuery.GetMapLocationsAsync();

                    LocationIndicator.IsRunning = false;
                    TxtBoxLocation.IsEnabled = true;
                    SetScreenButtonsEnabled(true);

                    if (results.Count > 0)
                    {
                        var geoAddress = results[0].Information.Address;
                        TxtBoxLocation.Text = FormatAddress(geoAddress);
                    }
                });
            }
            else
            {
                Dispatcher.BeginInvoke(() =>
                {
                    LocationIndicator.IsRunning = false;
                    TxtBoxLocation.IsEnabled = true;
                    SetScreenButtonsEnabled(true);
                });
            }
        }

        private string FormatAddress(MapAddress address)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(address.BuildingName))
            {
                sb.Append(address.HouseNumber).Append(",");
            }
            if (!string.IsNullOrEmpty(address.Street))
            {
                sb.Append(address.Street).Append(",");
            }
            if (!string.IsNullOrEmpty(address.City))
            {
                sb.Append(address.City).Append(",");
            }
            if (!string.IsNullOrEmpty(address.Country))
            {
                sb.Append(address.Country);
            }
            return sb.ToString();
        }

        //string ParseGoogleResponse(string data)
        //{
        //    var xmlElm = XElement.Parse(data);
        //    var status = (from elm in xmlElm.Descendants() where elm.Name == "status" select elm).FirstOrDefault();
        //    if (status.Value.ToLower() == "ok")
        //    {
        //        var res = (from elm in xmlElm.Descendants() where elm.Name == "formatted_address" select elm).FirstOrDefault();
        //        return res.Value;
        //    }
        //    return null;
        //}

        private async Task<GeoCoordinate> GetCurrentLocation()
        {
            Geolocator geoLocator = new Geolocator() { DesiredAccuracy = PositionAccuracy.High };
            try
            {
                var position = await geoLocator.GetGeopositionAsync(TimeSpan.FromHours(24), TimeSpan.FromSeconds(5));
                return CoordinateConverter.ConvertGeocoordinate(position.Coordinate);
            }
            catch (Exception) { }

            Dispatcher.BeginInvoke(() =>
            {
                ToastPrompt toast = new ToastPrompt() { Message = Constant.MSG_NO_GPS_CONNECTION };
                toast.Show();
            });

            return null;
        }

        protected override void OnBackKeyPress(CancelEventArgs e)
        {
            if (_locationWorker != null && _locationWorker.WorkerSupportsCancellation && _locationWorker.IsBusy)
            {
                _locationWorker.CancelAsync();
                _locationWorker = null;
                LocationIndicator.IsRunning = false;
                TxtBoxLocation.IsEnabled = true;
                SetScreenButtonsEnabled(true);
                e.Cancel = true;
            }

            base.OnBackKeyPress(e);
        }
    }
}