using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Live;
using Grow_Up.Model;
using Windows.Storage;
using System.IO.IsolatedStorage;
using Commons;
using Grow_Up.Resources;
using System.Threading.Tasks;
using Grow_Up.Helpers;

namespace Grow_Up
{
    public partial class UploadPage : PhoneApplicationPage
    {
        public UploadPage()
        {
            InitializeComponent();
            BuildLocalizedApplicationBar();

            ListPhotos.ItemsSource = App.ViewModelData.AllEntriesByDate;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (string.IsNullOrEmpty(App.ViewModelData.SkydriveFolderID))
            {
                SetScreenButonVisible(true);
                PrepareFolderSkydrive();
            }
        }

        private async void Upload_Btn_Click(object sender, EventArgs e)
        {
            if (ListPhotos.SelectedItems.Count > 0)
            {
                StorageFolder folder = await ApplicationData.Current.LocalFolder.CreateFolderAsync("shared", CreationCollisionOption.OpenIfExists);
                folder = await folder.CreateFolderAsync("transfers", CreationCollisionOption.OpenIfExists);

                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    for (int i = 0; i < ListPhotos.SelectedItems.Count; i++)
                    {
                        var entry = ListPhotos.SelectedItems[i] as Entry;

                        // copy to shared/transfers folder
                        if (isoStore.FileExists(entry.ImgSrc))
                        {
                            try
                            {
                                isoStore.CopyFile(entry.ImgSrc, "/shared/transfers/" + entry.ImgSrc);
                                LiveOperationResult res = await App.ViewModelData.LiveClient.BackgroundUploadAsync("me/skydrive",
                                    new Uri("/shared/transfers/" + entry.ImgSrc, UriKind.Relative), OverwriteOption.Rename);
                            }
                            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.ToString()); }
                        }
                    }
                }
            }
        }

        private async void PrepareFolderSkydrive()
        {
            try
            {
                LiveOperationResult operationResult = await App.ViewModelData.LiveClient.GetAsync("me/skydrive/files?filter=folders,albums");
                var folderData = operationResult.Result as dynamic;
                var folders = folderData.data as dynamic;
                foreach (dynamic folder in folders)
                {
                    if (folder.name.ToString().Equals(Constant.SKYDRIVE_FOLDER_NAME))
                    {
                        App.ViewModelData.SkydriveFolderID = folder.id.ToString();
                        break;
                    }
                }

                if (string.IsNullOrEmpty(App.ViewModelData.SkydriveFolderID))
                {
                    Dictionary<string, object> skyDriveFolderData = new Dictionary<string, object>();
                    skyDriveFolderData.Add("name", Constant.SKYDRIVE_FOLDER_NAME);
                    LiveOperationResult createFolderResult = await App.ViewModelData.LiveClient.PostAsync("me/skydrive", skyDriveFolderData);
                    var folder = createFolderResult.Result as dynamic;

                    App.ViewModelData.SkydriveFolderID = folder.id.ToString();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Exception everywhere " + ex.GetType());
                OSHelper.ShowToast(AppResources.TextID32);
            }

            if (!string.IsNullOrEmpty(App.ViewModelData.SkydriveFolderID))
            {
                SetAppBarButtonsEnable(true);
            }

            ProgressIndicator.IsRunning = false;
        }

        private void SetScreenButonVisible(bool isRunning)
        {
            ProgressIndicator.IsRunning = isRunning;
            SetAppBarButtonsEnable(!isRunning);
        }

        private void SetAppBarButtonsEnable(bool enable)
        {
            foreach (ApplicationBarIconButton btn in ApplicationBar.Buttons)
            {
                btn.IsEnabled = enable;
            }
        }

        private void BuildLocalizedApplicationBar()
        {
            ApplicationBarMenuItem selectAll = new ApplicationBarMenuItem(AppResources.TextID33);
            selectAll.Click += selectAll_Click;

            ApplicationBarMenuItem unselectAll = new ApplicationBarMenuItem(AppResources.TextID34);
            unselectAll.Click += unselectAll_Click;

            ApplicationBar.MenuItems.Add(selectAll);
            ApplicationBar.MenuItems.Add(unselectAll);
        }

        void unselectAll_Click(object sender, EventArgs e)
        {
            foreach (var item in ListPhotos.ItemsSource)
            {
                var container = ListPhotos.ContainerFromItem(item)
                                      as LongListMultiSelectorItem;
                if (container != null) container.IsSelected = false;
            }
        }

        void selectAll_Click(object sender, EventArgs e)
        {
            foreach (var item in ListPhotos.ItemsSource)
            {
                var container = ListPhotos.ContainerFromItem(item)
                                      as LongListMultiSelectorItem;
                if (container != null) container.IsSelected = true;
            }
        }
    }
}