using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Windows.Phone.Media.Capture;
using System.Threading.Tasks;
using Microsoft.Devices;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Grow_Up.Helpers;
using Microsoft.Phone.Shell;
using Models;
using Commons;
using Microsoft.Phone.Tasks;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Media.PhoneExtensions;
using Telerik.Windows.Controls;

namespace Grow_Up
{
    public partial class PhotoCapturePage : PhoneApplicationPage
    {
        private PhotoCaptureDevice _photoCaptureDevice = null;
        private bool _capturing = false;
        string _dateIndex = String.Empty;

        int _sizeMode = Constant.NORMAL_MODE;
        bool _flashEnabled = true;

        public PhotoCapturePage()
        {
            InitializeComponent();

            LoopingListDataSource photoModes = new LoopingListDataSource(2);
            photoModes.ItemNeeded += photoModes_ItemNeeded;
            photoModes.ItemUpdated += photoModes_ItemUpdated;
            PhotoModeList.DataSource = photoModes;
            PhotoModeList.SelectedIndex = 0;
        }

        void photoModes_ItemUpdated(object sender, LoopingListDataItemEventArgs e)
        {
            (e.Item as ModeItem).Name = e.Index % 2 == 0 ? "NORMAL" : "SQUARE";
        }

        void photoModes_ItemNeeded(object sender, LoopingListDataItemEventArgs e)
        {
            e.Item = new ModeItem() { Name = e.Index % 2 == 0 ? "NORMAL" : "SQUARE" };
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            NavigationContext.QueryString.TryGetValue("index", out _dateIndex);

            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.Dispose();
                _photoCaptureDevice = null;
            }

            ProgressIndicator.IsRunning = true;
            SetScreenButtonsEnabled(false);
            SetCameraButtonsEnabled(false);

            await InitializeCamera(CameraSensorLocation.Back);

            BackgroundVideoBrush.SetSource(_photoCaptureDevice);

            ProgressIndicator.IsRunning = false;
            SetScreenButtonsEnabled(true);
            SetCameraButtonsEnabled(true);

            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Captures a photo. Photo data is stored to ImageStream, and
        /// application is navigated to the preview page after capturing.
        /// </summary>
        private async Task Capture()
        {
            bool goToPreview = false;

            MemoryStream photoStream = new MemoryStream();
            MemoryStream thumbnailStream = new MemoryStream();
            if (!_capturing)
            {
                _capturing = true;

                CameraCaptureSequence sequence = _photoCaptureDevice.CreateCaptureSequence(1);
                sequence.Frames[0].CaptureStream = photoStream.AsOutputStream();
                sequence.Frames[0].ThumbnailStream = thumbnailStream.AsOutputStream();

                await _photoCaptureDevice.PrepareCaptureSequenceAsync(sequence);
                await sequence.StartCaptureAsync();

                _capturing = false;
                goToPreview = true;
            }

            _photoCaptureDevice.SetProperty(
                KnownCameraPhotoProperties.LockedAutoFocusParameters,
                AutoFocusParameters.None);

            if (goToPreview)
            {
                NokiaImaginHelper.PreparePhoto(photoStream, thumbnailStream);
                NavigationService.Navigate(new Uri(String.Format("/FilterPreviewPage.xaml?index={0}&shouldCrop={1}", _dateIndex, _sizeMode), UriKind.Relative));
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.Dispose();
                _photoCaptureDevice = null;
            }

            SetScreenButtonsEnabled(false);
            SetCameraButtonsEnabled(false);

            base.OnNavigatedFrom(e);
        }

        private async Task AutoFocus()
        {
            if (!_capturing && PhotoCaptureDevice.IsFocusSupported(CameraSensorLocation.Back))
            {
                SetScreenButtonsEnabled(false);
                SetCameraButtonsEnabled(false);

                await _photoCaptureDevice.FocusAsync();

                SetScreenButtonsEnabled(true);
                SetCameraButtonsEnabled(true);

                _capturing = false;
            }
        }

        /// <summary>
        /// Enables or disables listening to hardware shutter release key events.
        /// </summary>
        /// <param name="enabled">True to enable listening, false to disable listening.</param>
        private void SetCameraButtonsEnabled(bool enabled)
        {
            if (enabled)
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed += ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed += ShutterKeyPressed;
            }
            else
            {
                Microsoft.Devices.CameraButtons.ShutterKeyHalfPressed -= ShutterKeyHalfPressed;
                Microsoft.Devices.CameraButtons.ShutterKeyPressed -= ShutterKeyPressed;
            }
        }

        /// <summary>
        /// Completely pressing the shutter key initiates capturing a photo.
        /// </summary>
        private async void ShutterKeyPressed(object sender, EventArgs e)
        {
            await Capture();
        }

        /// <summary>
        /// Half-pressing the shutter key initiates autofocus unless tapped to focus.
        /// </summary>
        private async void ShutterKeyHalfPressed(object sender, EventArgs e)
        {
            await AutoFocus();
        }

        private void SetScreenButtonsEnabled(bool enabled)
        {
            CaptureBtn.IsHitTestVisible = enabled;
        }

        /// <summary>
        /// Adjusts UI according to device orientation.
        /// </summary>
        /// <param name="e">Orientation event arguments.</param>
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            base.OnOrientationChanged(e);

            SetOrientation(e.Orientation);
        }

        /// <summary>
        /// Initializes camera.
        /// </summary>
        /// <param name="sensorLocation">Camera sensor to initialize</param>
        private async Task InitializeCamera(CameraSensorLocation cameraSensorLocation)
        {
            Windows.Foundation.Size initialResolution =
                new Windows.Foundation.Size(Constant.DefaultCameraResolutionWidth,
                                            Constant.DefaultCameraResolutionHeight);
            Windows.Foundation.Size previewResolution =
                new Windows.Foundation.Size(Constant.DefaultCameraResolutionWidth,
                                            Constant.DefaultCameraResolutionHeight);

            // Find out the largest 4:3 capture resolution available on device
            IReadOnlyList<Windows.Foundation.Size> availableResolutions =
                PhotoCaptureDevice.GetAvailableCaptureResolutions(cameraSensorLocation);
            Windows.Foundation.Size captureResolution = new Windows.Foundation.Size(0, 0);
            for (int i = 0; i < availableResolutions.Count; i++)
            {
                double ratio = availableResolutions[i].Width / availableResolutions[i].Height;
                if (ratio > 1.32 && ratio < 1.34)
                {
                    if (captureResolution.Width < availableResolutions[i].Width)
                    {
                        captureResolution = availableResolutions[i];
                    }
                }
            }

            PhotoCaptureDevice device =
                await PhotoCaptureDevice.OpenAsync(cameraSensorLocation, initialResolution);
            await device.SetCaptureResolutionAsync(captureResolution);
            await device.SetPreviewResolutionAsync(previewResolution);

            _photoCaptureDevice = device;

            IReadOnlyList<object> supportedFlashmodes = PhotoCaptureDevice.GetSupportedPropertyValues(CameraSensorLocation.Back, KnownCameraPhotoProperties.FlashMode);
            if (supportedFlashmodes.Count > 1)
            {
                _photoCaptureDevice.SetProperty(KnownCameraPhotoProperties.FlashMode, FlashMode.Off);
            }
            else
            {
                TxtBlock_Flash.Visibility = Visibility.Collapsed;
            }

            SetOrientation(this.Orientation);
        }

        /// <summary>
        /// Makes adjustments to UI depending on device orientation. Ensures 
        /// that the viewfinder stays fully visible in the middle of the 
        /// screen. This requires dynamic changes to title and video canvas.
        /// </summary>
        /// <param name="orientation">Device orientation.</param>
        private void SetOrientation(PageOrientation orientation)
        {
            int videoBrushTransformRotation = 0;

            // Orientation.specific changes to default values
            if (orientation == PageOrientation.PortraitUp)
            {
                videoBrushTransformRotation = 90;
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                videoBrushTransformRotation = 180;
            }

            // Set correct values
            if (_sizeMode == Constant.SQUARE_MODE)
            {
                VideoCanvas.Width = VideoCanvas.Height = 480.0;
            }
            else
            {
                VideoCanvas.Width = 480;
                VideoCanvas.Height = 640;
            }

            VideoBrushTransform.Rotation = videoBrushTransformRotation;

            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.SetProperty(
                    KnownCameraGeneralProperties.EncodeWithOrientation,
                    VideoBrushTransform.Rotation);
            }
        }

        private void FlashMode_Tapped(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (_flashEnabled)
                TxtBlock_Flash.Text = "FLASH OFF";
            else
                TxtBlock_Flash.Text = "FLASH ON";

            _flashEnabled = !_flashEnabled;

            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.SetProperty(
                    KnownCameraPhotoProperties.FlashMode,
                    _flashEnabled ? FlashMode.On : FlashMode.Off);
            }
        }

        private async void CaptureBtn_Clicked(object sender, RoutedEventArgs e)
        {
            await AutoFocus();
            await Capture();
        }

        private void PhotoModeList_SelectedIndexChanged(object sender, EventArgs e)
        {
            _sizeMode = PhotoModeList.SelectedIndex % 2 == 0 ? Constant.NORMAL_MODE : Constant.SQUARE_MODE;
            if (_sizeMode == Constant.NORMAL_MODE)
            {
                BackgroundVideoBrush.Stretch = System.Windows.Media.Stretch.Fill;
            }
            else
            {
                BackgroundVideoBrush.Stretch = System.Windows.Media.Stretch.UniformToFill;
            }

            SetOrientation(this.Orientation);
        }
    }

    class ModeItem : LoopingListDataItem
    {
        public ModeItem()
        {

        }

        public String Name { get; set; }
    }
}