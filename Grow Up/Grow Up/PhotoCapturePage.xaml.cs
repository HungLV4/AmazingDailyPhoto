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

namespace Grow_Up
{
    public partial class PhotoCapturePage : PhoneApplicationPage
    {
        int _dateIndex = -1;
        private PhotoCaptureDevice _photoCaptureDevice = null;
        private bool _capturing = false;

        public PhotoCapturePage()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
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

            SetOrientation(this.Orientation);

            base.OnNavigatedTo(e);
        }

        private async void Capture_Btn_Click(object sender, EventArgs e)
        {
            await AutoFocus();
            await Capture();
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

                App.PhotoModel = new PhotoModel() { Buffer = photoStream.GetWindowsRuntimeBuffer() };
                App.PhotoModel.Captured = true;
                App.PhotoModel.Dirty = true;

                App.ThumbnailModel = new PhotoModel() { Buffer = thumbnailStream.GetWindowsRuntimeBuffer() };
                App.ThumbnailModel.Captured = true;
                App.ThumbnailModel.Dirty = true;

                photoStream.Dispose();
                thumbnailStream.Dispose();

                NavigationService.Navigate(new Uri(String.Format("/FilterPreviewPage.xaml?index={0}", _dateIndex), UriKind.Relative));
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
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }
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
            int videoCanvasDimension = 480;

            Thickness videoCanvasMargin = new Thickness(-60, 0, 0, 0);

            // Orientation.specific changes to default values
            if (orientation == PageOrientation.PortraitUp)
            {
                videoBrushTransformRotation = 90;
                videoCanvasMargin = new Thickness(0, -20, 0, 0);
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                videoBrushTransformRotation = 180;
                videoCanvasMargin = new Thickness(60, 0, 0, 0);
            }

            // Set correct values
            VideoBrushTransform.Rotation = videoBrushTransformRotation;
            VideoCanvas.Width = videoCanvasDimension;
            VideoCanvas.Height = videoCanvasDimension;
            VideoCanvas.Margin = videoCanvasMargin;

            if (_photoCaptureDevice != null)
            {
                _photoCaptureDevice.SetProperty(
                    KnownCameraGeneralProperties.EncodeWithOrientation,
                    VideoBrushTransform.Rotation);
            }
        }
    }
}