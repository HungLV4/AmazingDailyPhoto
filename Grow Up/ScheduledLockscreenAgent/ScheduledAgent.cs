//#define DEBUG_AGENT

using System.Diagnostics;
using System.Windows;
using Microsoft.Phone.Scheduler;
using System;
using System.Net;
using System.Linq;
using Windows.Phone.System.UserProfile;
using System.IO.IsolatedStorage;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using System.Windows.Controls;
using System.Windows.Media;
using Models;
using Commons;
using System.Collections.ObjectModel;
using Microsoft.Phone.Info;
using System.Net.Http;
using System.Threading;
using Microsoft.Phone.Shell;
using System.Windows.Shapes;

namespace ScheduledLockscreenAgent
{
    public class ScheduledAgent : ScheduledTaskAgent
    {
        string bingImageUri = "http://appserver.m.bing.net/BackgroundImageService/TodayImageService.svc/GetTodayImage?dateOffset=0&urlEncodeHeaders=true&osName=wince&osVersion=7.10&orientation=480x800&deviceName=WP7Device&mkt=en-US&AppId=1";

        /// <remarks>
        /// ScheduledAgent constructor, initializes the UnhandledException handler
        /// </remarks>
        static ScheduledAgent()
        {
            // Subscribe to the managed exception handler
            Deployment.Current.Dispatcher.BeginInvoke(delegate
            {
                Application.Current.UnhandledException += UnhandledException;
            });
        }

        /// Code to execute on Unhandled Exceptions
        private static void UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            if (Debugger.IsAttached)
            {
                // An unhandled exception has occurred; break into the debugger
                Debugger.Break();
            }
        }

        /// <summary>
        /// Agent that runs a scheduled task
        /// </summary>
        /// <param name="task">
        /// The invoked task
        /// </param>
        /// <remarks>
        /// This method is called when a periodic or resource intensive task is invoked
        /// </remarks>
        protected override void OnInvoke(ScheduledTask task)
        {
            if (!LockScreenManager.IsProvidedByCurrentApplication)
            {
                try
                {
                    ScheduledActionService.Remove(task.Name);
                }
                catch (Exception)
                {
                }
                return;
            }

            var evnt = new ManualResetEvent(false);

            WebClient client = new WebClient();
            client.OpenReadCompleted += new OpenReadCompletedEventHandler((sender, e) =>
            {
                if (e.Error != null || e.Cancelled)
                {
                    evnt.Set();
                    return;
                }

                Deployment.Current.Dispatcher.BeginInvoke(async () =>
                {
                    string imageName = "";
                    if (LockScreen.GetImageUri().ToString().EndsWith("_A.jpg"))
                    {
                        imageName = "lockscreen_B.jpg";
                    }
                    else
                    {
                        imageName = "lockscreen_A.jpg";
                    }

                    using (var memoryStream = new MemoryStream())
                    {
                        try
                        {
                            e.Result.CopyTo(memoryStream);
                        }
                        catch (Exception)
                        {
                            e.Result.Close();
                            evnt.Set();
                            return;
                        }

                        memoryStream.Position = 0;

                        e.Result.Close();

                        PhotoModel model = new PhotoModel() { Buffer = memoryStream.GetWindowsRuntimeBuffer() };

                        WriteableBitmap photo = new WriteableBitmap((int)model.Width, (int)model.Height);
                        await model.RenderBitmapAsync(photo);

                        // loading image
                        using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                        {
                            if (iso.FileExists(imageName))
                            {
                                iso.DeleteFile(imageName);
                            }

                            var names = iso.GetFileNames("*_sthumb.jpg");
                            if (names.Length > 0)
                            {
                                int index1 = DateTime.Now.Millisecond % names.Length;
                                int index2 = (index1 + 1) % names.Length;
                                int index3 = (index2 + 1) % names.Length;
                                int index4 = (index3 + 1) % names.Length;

                                BitmapImage overlayBitmap1 = new BitmapImage();
                                BitmapImage overlayBitmap2 = new BitmapImage();
                                BitmapImage overlayBitmap3 = new BitmapImage();
                                BitmapImage overlayBitmap4 = new BitmapImage();

                                using (var stream = iso.OpenFile(names[index1], System.IO.FileMode.Open, System.IO.FileAccess.Read))
                                {
                                    overlayBitmap1.SetSource(stream);
                                }

                                using (var stream = iso.OpenFile(names[index2], System.IO.FileMode.Open, System.IO.FileAccess.Read))
                                {
                                    overlayBitmap2.SetSource(stream);
                                }

                                using (var stream = iso.OpenFile(names[index3], System.IO.FileMode.Open, System.IO.FileAccess.Read))
                                {
                                    overlayBitmap3.SetSource(stream);
                                }

                                using (var stream = iso.OpenFile(names[index4], System.IO.FileMode.Open, System.IO.FileAccess.Read))
                                {
                                    overlayBitmap4.SetSource(stream);
                                }

                                StackPanel container = new StackPanel()
                                {
                                    Width = 480,
                                    Height = 800,
                                    Background = new ImageBrush() { ImageSource = photo },
                                };

                                var imageContainer = new Canvas()
                                {
                                    Width = 480,
                                    Height = 800,
                                    VerticalAlignment = VerticalAlignment.Top,
                                    HorizontalAlignment = HorizontalAlignment.Center,
                                    Background = new SolidColorBrush(Colors.Transparent)
                                };

                                var initialMarginTop = 32;
                                var additionalMarginTop = 10;
                                var opacityBorderLength1 = 80;
                                
                                var opacityBorder1 = new Border()
                                {
                                    Width = opacityBorderLength1 + Constant.ThumbnailSmallSide / 2,
                                    Height = Constant.ThumbnailSmallSide,
                                    Margin = new Thickness(0, initialMarginTop, 0, 0),
                                    Background = new SolidColorBrush(Colors.Black),
                                    Opacity = 0.6
                                };
                                Ellipse overlayImage1 = new Ellipse()
                                {
                                    Width = Constant.ThumbnailSmallSide,
                                    Height = Constant.ThumbnailSmallSide,
                                    Fill = new ImageBrush() { ImageSource = overlayBitmap1 },
                                    Margin = new Thickness(opacityBorderLength1, initialMarginTop, 0, 0)
                                };

                                var opacityBorderLength2 = opacityBorderLength1 + Constant.ThumbnailSmallSide;
                                var opacityBorder2 = new Border()
                                {
                                    Width = opacityBorderLength2 + Constant.ThumbnailSmallSide / 2,
                                    Height = Constant.ThumbnailSmallSide,
                                    Margin = new Thickness(0, initialMarginTop + additionalMarginTop + Constant.ThumbnailSmallSide, 0, 0),
                                    Background = new SolidColorBrush(Colors.Black),
                                    Opacity = 0.6
                                };
                                Ellipse overlayImage2 = new Ellipse()
                                {
                                    Width = Constant.ThumbnailSmallSide,
                                    Height = Constant.ThumbnailSmallSide,
                                    Fill = new ImageBrush() { ImageSource = overlayBitmap2 },
                                    Margin = new Thickness(opacityBorderLength2, initialMarginTop + additionalMarginTop + Constant.ThumbnailSmallSide, 0, 0)
                                };

                                var opacityBorderLength3 = opacityBorderLength1 + 2 * Constant.ThumbnailSmallSide;
                                var opacityBorder3 = new Border()
                                {
                                    Width = opacityBorderLength3 + Constant.ThumbnailSmallSide / 2,
                                    Height = Constant.ThumbnailSmallSide,
                                    Margin = new Thickness(0, initialMarginTop + 2 * additionalMarginTop + 2 * Constant.ThumbnailSmallSide, 0, 0),
                                    Background = new SolidColorBrush(Colors.Black),
                                    Opacity = 0.6
                                };
                                Ellipse overlayImage3 = new Ellipse()
                                {
                                    Width = Constant.ThumbnailSmallSide,
                                    Height = Constant.ThumbnailSmallSide,
                                    Fill = new ImageBrush() { ImageSource = overlayBitmap3 },
                                    Margin = new Thickness(opacityBorderLength3, initialMarginTop + 2 * additionalMarginTop + 2 * Constant.ThumbnailSmallSide, 0, 0)
                                };

                                var opacityBorderLength4 = opacityBorderLength1 + 3 * Constant.ThumbnailSmallSide;
                                var opacityBorder4 = new Border()
                                {
                                    Width = opacityBorderLength4 + Constant.ThumbnailSmallSide / 2,
                                    Height = Constant.ThumbnailSmallSide,
                                    Margin = new Thickness(0, initialMarginTop + 3 * additionalMarginTop + 3 * Constant.ThumbnailSmallSide, 0, 0),
                                    Background = new SolidColorBrush(Colors.Black),
                                    Opacity = 0.6
                                };
                                Ellipse overlayImage4 = new Ellipse()
                                {
                                    Width = Constant.ThumbnailSmallSide,
                                    Height = Constant.ThumbnailSmallSide,
                                    Fill = new ImageBrush() { ImageSource = overlayBitmap4 },
                                    Margin = new Thickness(opacityBorderLength4, initialMarginTop + 3 * additionalMarginTop + 3 * Constant.ThumbnailSmallSide, 0, 0)
                                };

                                imageContainer.Children.Add(opacityBorder1);
                                imageContainer.Children.Add(overlayImage1);

                                imageContainer.Children.Add(opacityBorder2);
                                imageContainer.Children.Add(overlayImage2);

                                imageContainer.Children.Add(opacityBorder3);
                                imageContainer.Children.Add(overlayImage3);

                                imageContainer.Children.Add(opacityBorder4);
                                imageContainer.Children.Add(overlayImage4);

                                container.Children.Add(imageContainer);

                                //TextBlock headerTxtBlock = new TextBlock()
                                //{
                                //    FontFamily = new FontFamily("Comic Sans MS"),
                                //    Foreground = new SolidColorBrush(Colors.White),
                                //    FontSize = 40,
                                //    Text = "Amazing Daily Photo",
                                //    HorizontalAlignment = HorizontalAlignment.Center,
                                //    TextWrapping = TextWrapping.Wrap,
                                //    Margin = new Thickness(0, 10, 0, 0)
                                //};
                                //container.Children.Add(headerTxtBlock);

                                container.Measure(new Size(480, 800));
                                container.Arrange(new Rect(0, 0, 480, 800));

                                photo.Render(container, null);
                                photo.Invalidate();
                            }

                            IsolatedStorageFileStream isostream = iso.CreateFile(imageName);
                            Extensions.SaveJpeg(photo, isostream, photo.PixelWidth, photo.PixelHeight, 0, 100);
                            isostream.Close();
                        }
                    }

                    ChangeLockscreen(imageName, false);

                    evnt.Set();
                });
            });
            client.OpenReadAsync(new Uri(bingImageUri, UriKind.Absolute));

            evnt.WaitOne(15000);

#if DEBUG_AGENT
            ScheduledActionService.LaunchForTest(task.Name, TimeSpan.FromSeconds(15));
#endif
            NotifyComplete();
        }

        private void ChangeLockscreen(string filePathOfTheImage, bool isAppResource)
        {
            if (LockScreenManager.IsProvidedByCurrentApplication)
            {
                var schema = isAppResource ? "ms-appx:///{0}" : "ms-appdata:///Local/{0}";
                var uri = new Uri(String.Format(schema, filePathOfTheImage), UriKind.Absolute);

                LockScreen.SetImageUri(uri);
            }
        }
    }
}