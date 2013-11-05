using Commons;
using Grow_Up.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Windows.Phone.System.UserProfile;
using WPControls;

namespace Grow_Up.ViewModel
{
    public class VM : INotifyPropertyChanged, IDateToBrushConverter
    {
        public DateDataContext Db;

        public VM(string dbConnection)
        {
            Db = new DateDataContext(dbConnection);
        }

        private DateData _selectedDateData;
        public DateData SelectedDateData
        {
            get { return _selectedDateData; }
            set
            {
                _selectedDateData = value;
                NotifyPropertyChanged("SelectedDateData");
            }
        }

        private ObservableCollection<DateData> _allDates;
        public ObservableCollection<DateData> AllDates
        {
            get { return _allDates; }
            set
            {
                _allDates = value;
                NotifyPropertyChanged("AllDates");
            }
        }

        private ObservableCollection<Entry> _allEntries;
        public ObservableCollection<Entry> AllEntries
        {
            get { return _allEntries; }
            set
            {
                _allEntries = value;
                NotifyPropertyChanged("AllEntries");
            }
        }

        private bool _isLocationOn;
        public bool IsLocationOn
        {
            get { return _isLocationOn; }
            set
            {
                _isLocationOn = value;
                NotifyPropertyChanged("IsLocationOn");
            }
        }

        private int _selectedBackgroundIdx;
        public int SelectedBackgroundIdx
        {
            get { return _selectedBackgroundIdx; }
            set
            {
                _selectedBackgroundIdx = value;
                NotifyPropertyChanged("SelectedBackgroundIdx");
            }
        }

        private int _selectedFontIdx;
        public int SelectedFontIdx
        {
            get { return _selectedFontIdx; }
            set
            {
                _selectedFontIdx = value;
                NotifyPropertyChanged("SelectedFontIdx");
            }
        }

        private bool _isTrial;
        public bool IsTrial
        {
            get { return _isTrial; }
            set
            {
                _isTrial = value;
                NotifyPropertyChanged("IsTrial");
            }
        }

        public List<int> BackgroundSrcList
        {
            get { return new List<int> { 0, 1, 2, 3, 4, 5, 6 }; }
        }

        public List<int> CalendarBackgroundSrcList
        {
            get { return new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 }; }
        }



        public Brush Convert(DateTime dateTime, bool isSelected, Brush defaultValue, BrushType brushType)
        {
            if (brushType == BrushType.Background)
            {
                if (AllDates != null)
                {
                    int index = AllDates.ToList().FindIndex(date => date.CalendarItemDate == dateTime);
                    if (index >= 0)
                    {
                        DateData date = AllDates[index];
                        if (date.ThumbnailEntryId != -1)
                        {
                            Entry entry = date.Entries.Where(o => o.Id == date.ThumbnailEntryId).FirstOrDefault();
                            if (entry != null)
                                return new ImageBrush() { ImageSource = entry.SmallThumbImage };
                        }

                        if (date.Entries.Count > 0)
                        {
                            return new ImageBrush() { ImageSource = date.Entries.First().SmallThumbImage };
                        }
                    }
                }

                if (dateTime == DateTime.Now.Date)
                {
                    return new SolidColorBrush(Colors.Red);
                }

                if (isSelected)
                {
                    return new SolidColorBrush(Color.FromArgb(255, 29, 161, 222));
                }

                return new SolidColorBrush(Colors.Transparent);
            }
            else
            {
                return new SolidColorBrush(Colors.White);
            }
        }

        public void SaveChangesToDb()
        {
            Db.SubmitChanges();
        }

        public void LoadUserSettings()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_LOCATION))
            {
                IsLocationOn = false;
            }
            else
            {
                IsLocationOn = (bool)IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_LOCATION];
            }

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_BACKGROUND_IDX))
            {
                SelectedBackgroundIdx = 0;
            }
            else
            {
                SelectedBackgroundIdx = (int)IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_BACKGROUND_IDX];
            }

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_FONT_IDX))
            {
                SelectedFontIdx = 0;
            }
            else
            {
                SelectedFontIdx = (int)IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_FONT_IDX];
            }
        }

        public void SaveSettings()
        {
            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_LOCATION))
            {
                IsolatedStorageSettings.ApplicationSettings.Add(Constant.SETTING_LOCATION, IsLocationOn);
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_LOCATION] = IsLocationOn;
            }

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_BACKGROUND_IDX))
            {
                IsolatedStorageSettings.ApplicationSettings.Add(Constant.SETTING_BACKGROUND_IDX, SelectedBackgroundIdx);
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_BACKGROUND_IDX] = SelectedBackgroundIdx;
            }

            if (!IsolatedStorageSettings.ApplicationSettings.Contains(Constant.SETTING_FONT_IDX))
            {
                IsolatedStorageSettings.ApplicationSettings.Add(Constant.SETTING_FONT_IDX, SelectedFontIdx);
            }
            else
            {
                IsolatedStorageSettings.ApplicationSettings[Constant.SETTING_FONT_IDX] = SelectedFontIdx;
            }
        }

        public void LoadDataFromDb()
        {
            var entriesInDb = from Entry e in Db.Entries select e;
            AllEntries = new ObservableCollection<Entry>(entriesInDb);

            var dateDataInDb = from DateData d in Db.Dates select d;
            AllDates = new ObservableCollection<DateData>(dateDataInDb);
        }

        public void AddEntry(Entry entity, int todayIndex)
        {
            Db.Entries.InsertOnSubmit(entity);
            Db.SubmitChanges();

            AllEntries.Add(entity);
            AllDates[todayIndex].Entries.Add(entity);
        }

        public void DeleteEntry(Entry entity)
        {
            AllEntries.Remove(entity);

            // delete image in isolate storage
            using (IsolatedStorageFile isolatedStorage = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (entity.ImgSrc != null && isolatedStorage.FileExists(entity.ImgSrc))
                {
                    isolatedStorage.DeleteFile(entity.ImgSrc);
                }
            }

            Db.Entries.DeleteOnSubmit(entity);
            Db.SubmitChanges();
        }

        public void AddDateData(DateData entity)
        {
            Db.Dates.InsertOnSubmit(entity);
            Db.SubmitChanges();

            AllDates.Add(entity);
        }

        public void DeleteDateData(DateData entity)
        {
            AllDates.Remove(entity);

            Entry[] clone = new Entry[AllEntries.Count];
            AllEntries.CopyTo(clone, 0);
            foreach (Entry e in clone)
            {
                if (e.Date == entity)
                {
                    DeleteEntry(e);
                }
            }

            Db.Dates.DeleteOnSubmit(entity);
            Db.SubmitChanges();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
