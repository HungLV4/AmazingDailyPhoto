using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPControls;

namespace Grow_Up.Model
{
    [Table]
    public class DateData : INotifyPropertyChanged, INotifyPropertyChanging, ISupportCalendarItem
    {
        private int _id;
        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "INT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public int Id
        {
            get
            {
                return _id;
            }
            set
            {
                if (_id != value)
                {
                    NotifyPropertyChanging("Id");
                    _id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private DateTime _calendarItemDate;
        [Column]
        public DateTime CalendarItemDate
        {
            get { return _calendarItemDate; }
            set
            {
                if (_calendarItemDate != value)
                {
                    NotifyPropertyChanging("CalendarItemDate");
                    _calendarItemDate = value;
                    NotifyPropertyChanged("CalendarItemDate");
                }
            }
        }

        private int _thumbnailEntryId = -1;
        [Column]
        public int ThumbnailEntryId
        {
            get { return _thumbnailEntryId; }
            set
            {
                if (_thumbnailEntryId != value)
                {
                    NotifyPropertyChanging("ThumbnailEntryId");
                    _thumbnailEntryId = value;
                    NotifyPropertyChanged("ThumbnailEntryId");
                }
            }
        }

        private EntitySet<Entry> _entries;
        [Association(Storage = "_entries", OtherKey = "_dateId", ThisKey = "Id")]
        public EntitySet<Entry> Entries
        {
            get { return this._entries; }
            set
            {
                this._entries.Assign(value);
            }
        }

        public DateData()
        {
            _entries = new EntitySet<Entry>(
                new Action<Entry>(attach_entry),
                new Action<Entry>(detach_entry)
                );
        }


        private void attach_entry(Entry obj)
        {
            NotifyPropertyChanging("Entry");
            obj.Date = this;
        }

        private void detach_entry(Entry obj)
        {
            NotifyPropertyChanging("Entry");
            obj.Date = null;
        }

        #region notify event
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangingEventHandler PropertyChanging;
        private void NotifyPropertyChanging(string propertyName)
        {
            if (PropertyChanging != null)
            {
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
            }
        }
        #endregion
    }
}
