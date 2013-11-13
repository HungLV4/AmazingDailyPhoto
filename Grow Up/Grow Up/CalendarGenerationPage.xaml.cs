using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.IO;
using System.Windows.Media;
using Commons;
using Grow_Up.View;
using System.Windows.Media.Imaging;
using Microsoft.Xna.Framework.Media;
using Coding4Fun.Toolkit.Controls;
using System.ComponentModel;
using Microsoft.Phone.Tasks;
using Microsoft.Phone;
using System.Windows.Resources;
using Grow_Up.Resources;

namespace Grow_Up
{
    public partial class CalendarGenerationPage : PhoneApplicationPage
    {
        public CalendarGenerationPage()
        {
            InitializeComponent();

            ListBackgroundItem.ItemsSource = App.ViewModelData.CalendarBackgroundSrcList;
            ListBackgroundItem.SelectedIndex = 0;

            Loaded += CalendarGenerationPage_Loaded;
        }

        void CalendarGenerationPage_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressIndicator.IsRunning = false;
        }

        private void Process_Btn_Click(object sender, EventArgs e)
        {
            SetScreenButtonsEnabled(false);
            var index = ListBackgroundItem.SelectedIndex;

            BackgroundWorker generationWorker = new BackgroundWorker();
            generationWorker.RunWorkerCompleted += generationWorker_RunWorkerCompleted;
            generationWorker.DoWork += generationWorker_DoWork;
            generationWorker.RunWorkerAsync(index);
        }

        void generationWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int index = (int)e.Argument;

            DateTime now = DateTime.Now;
            string photoName = String.Format("daily_{0}.jpg", now.Ticks);

            Dispatcher.BeginInvoke(() =>
                {
                    using (MemoryStream stream = new MemoryStream())
                    {
                        Stream bgStream = Application.GetResourceStream(new Uri(String.Format("Assets/CalendarTemplate/{0}.jpg", index), UriKind.Relative)).Stream;
                        var container = new StackPanel()
                        {
                            Width = Constant.GENERATED_IMAGE_WIDTH,
                            Height = Constant.GENERATED_IMAGE_HEIGHT,
                            Background = new ImageBrush()
                            {
                                ImageSource = PictureDecoder.DecodeJpeg(bgStream, Constant.GENERATED_IMAGE_WIDTH, Constant.GENERATED_IMAGE_HEIGHT)
                            }
                        };
                        bgStream.Close();

                        var marginLeft = 20;
                        var marginTop = 50;
                        var offset = 25;

                        var imageContainer = new Canvas()
                        {
                            Width = Constant.GENERATED_CALENDAR_WIDTH + offset * 2,
                            Height = Constant.GENERATED_IMAGE_HEIGHT,
                            Background = new SolidColorBrush(Colors.Transparent),
                            Margin = new Thickness(marginLeft, 0, 0, 0),
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        var opacityBorder = new Border()
                        {
                            Width = Constant.GENERATED_CALENDAR_WIDTH + offset * 2,
                            Height = Constant.GENERATED_IMAGE_HEIGHT,
                            Background = new SolidColorBrush(Colors.Black),
                            Opacity = 0.5
                        };

                        var calendar = new CalendarForRender()
                        {
                            DataContext = App.ViewModelData,
                            Width = Constant.GENERATED_CALENDAR_WIDTH,
                            Margin = new Thickness(offset, marginTop, 0, 0)
                        };
                        //calendar.Calendar.SelectedDate = DateTime.Now;
                        calendar.Calendar.SelectedMonth = DateSelector.SelectedValue.Month;
                        calendar.Calendar.SelectedYear = DateSelector.SelectedValue.Year;
                        calendar.Calendar.Refresh();

                        imageContainer.Children.Add(opacityBorder);
                        imageContainer.Children.Add(calendar);

                        container.Children.Add(imageContainer);
                        container.Measure(new Size(Constant.GENERATED_IMAGE_WIDTH, Constant.GENERATED_IMAGE_HEIGHT));
                        container.Arrange(new Rect(0, 0, Constant.GENERATED_IMAGE_WIDTH, Constant.GENERATED_IMAGE_HEIGHT));

                        var bitmap = new WriteableBitmap(container, null);
                        bitmap.SaveJpeg(stream, bitmap.PixelWidth, bitmap.PixelHeight, 0, 100);
                        stream.Seek(0, SeekOrigin.Begin);
                        using (MediaLibrary library = new MediaLibrary())
                        {
                            library.SavePicture(photoName, stream);
                        }
                    }
                });
        }

        void generationWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            SetScreenButtonsEnabled(true);
            ToastPrompt toast = new ToastPrompt()
            {
                Title = "",
                Message = AppResources.TextID19,
                ImageSource = new BitmapImage(new Uri("Assets/Tiles/icon_small.png", UriKind.Relative)),
                MillisecondsUntilHidden = 2000
            };

            toast.Show();
        }

        private void Template_Selected(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var index = (int)(sender as Grid).DataContext;
            if (index > 0 && App.ViewModelData.IsTrial)
            {
                MessageBoxResult result = MessageBox.Show(AppResources.TextID22, "Amazing Daily Photo", MessageBoxButton.OKCancel);
                if (result == MessageBoxResult.OK)
                {
                    MarketplaceDetailTask task = new MarketplaceDetailTask();
                    task.Show();
                }
                ListBackgroundItem.SelectedIndex = 0;
            }
            e.Handled = true;
        }

        private void SetScreenButtonsEnabled(bool enabled)
        {
            ProgressIndicator.IsRunning = !enabled;
            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }
        }
    }
}