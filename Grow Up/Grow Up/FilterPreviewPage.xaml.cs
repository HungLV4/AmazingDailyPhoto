using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media.Imaging;
using System.IO.IsolatedStorage;
using System.Threading.Tasks;
using Nokia.Graphics.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Nokia.InteropServices.WindowsRuntime;
using Grow_Up.View;
using Grow_Up.Helpers;
using Models;
using Commons;

namespace Grow_Up
{
    public partial class FilterPreviewPage : PhoneApplicationPage
    {
        int _dateIndex = -1;

        public FilterPreviewPage()
        {
            InitializeComponent();

            Loaded += FilterPreviewPage_Loaded;
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
        }

        async void FilterPreviewPage_Loaded(object sender, RoutedEventArgs e)
        {
            ProgressIndicator.IsRunning = true;

            App.ThumbnailModel.UndoAllFilters();
            App.ThumbnailModel.ApplyFilter(new NoFilterModel());
            App.ThumbnailModel.Dirty = true;

            WriteableBitmap photo = new WriteableBitmap((int)App.ThumbnailModel.Width, (int)App.ThumbnailModel.Height);
            await App.ThumbnailModel.RenderBitmapAsync(photo);
            PhotoViewer.Source = photo;

            await RenderAsync();
        }

        /// <summary>
        /// Asynchronously renders filter image thumbnails.
        /// </summary>
        private async Task RenderAsync()
        {
            int side = Constant.FilterPreviewSize;

            try
            {
                Bitmap bitmap = await App.ThumbnailModel.RenderThumbnailBitmapAsync(side);

                await RenderThumbnailsAsync(bitmap, side, App.FiltersModel.ArtisticFilters, FiltersWrapPanel);
            }
            catch (Exception)
            {
                // add code later to navigate to mainpage
            }
        }

        /// <summary>
        /// For the given bitmap renders filtered thumbnails for each filter in given list and populates
        /// the given wrap panel with the them.
        /// 
        /// For quick rendering, renders 10 thumbnails synchronously and then releases the calling thread.
        /// </summary>
        /// <param name="bitmap">Source bitmap to be filtered</param>
        /// <param name="side">Side length of square thumbnails to be generated</param>
        /// <param name="list">List of filters to be used, one per each thumbnail to be generated</param>
        /// <param name="panel">Wrap panel to be populated with the generated thumbnails</param>
        private async Task RenderThumbnailsAsync(Bitmap bitmap, int side, List<FilterModel> list, WrapPanel FiltersWrapPanel)
        {
            using (EditingSession session = new EditingSession(bitmap))
            {
                //render filtered photo
                int i = 0;
                foreach (FilterModel filter in list)
                {
                    WriteableBitmap writeableBitmap = new WriteableBitmap(side, side);

                    //crop the bitmap
                    foreach (IFilter f in filter.Components)
                    {
                        session.AddFilter(f);
                    }

                    Windows.Foundation.IAsyncAction action = session.RenderToBitmapAsync(writeableBitmap.AsBitmap());

                    i++;
                    if (i % 10 == 0)
                    {
                        // async, give control back to UI before proceeding.
                        await action;
                    }
                    else
                    {
                        // synchroneous, we keep the CPU for ourselves.
                        Task task = action.AsTask();
                        task.Wait();
                    }

                    PhotoThumbnail photoThumbnail = new PhotoThumbnail()
                    {
                        Bitmap = writeableBitmap,
                        Text = filter.Name,
                        Width = side,
                        Margin = new Thickness(6)
                    };

                    photoThumbnail.Tap += async delegate
                    {
                        ProgressIndicator.IsRunning = true;
                        SetScreenButtonsEnabled(false);

                        App.ThumbnailModel.UndoAllFilters();
                        App.ThumbnailModel.ApplyFilter(filter);
                        App.ThumbnailModel.Dirty = true;

                        WriteableBitmap photo = new WriteableBitmap((int)App.ThumbnailModel.Width, (int)App.ThumbnailModel.Height);
                        await App.ThumbnailModel.RenderBitmapAsync(photo);
                        PhotoViewer.Source = photo;

                        SetScreenButtonsEnabled(true);
                        ProgressIndicator.IsRunning = false;
                    };

                    FiltersWrapPanel.Children.Add(photoThumbnail);

                    session.UndoAll();
                }
            }
            ProgressIndicator.IsRunning = false;
        }

        private void Apply_Btn_Click(object sender, EventArgs e)
        {
            ProgressIndicator.IsRunning = true;
            SetScreenButtonsEnabled(false);

            var filter = App.ThumbnailModel.AppliedFilters.Count > 0 ? App.ThumbnailModel.AppliedFilters.Last() : new NoFilterModel();
            //apply filter for real model here
            App.PhotoModel.ApplyFilter(filter);
            App.PhotoModel.Dirty = true;

            ProgressIndicator.IsRunning = false;
            SetScreenButtonsEnabled(true);
            NavigationService.Navigate(new Uri(String.Format("/AddMetaDataPage.xaml?index={0}", _dateIndex), UriKind.Relative));
        }

        private void SetScreenButtonsEnabled(bool enabled)
        {
            foreach (var thumbnail in FiltersWrapPanel.Children)
            {
                (thumbnail as PhotoThumbnail).IsEnabled = enabled;
            }

            foreach (ApplicationBarIconButton b in ApplicationBar.Buttons)
            {
                b.IsEnabled = enabled;
            }
        }
    }
}