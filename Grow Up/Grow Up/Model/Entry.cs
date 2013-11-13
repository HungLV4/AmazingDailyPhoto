using Commons;
using Microsoft.Phone;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Grow_Up.Model
{
    [Table]
    public class Entry : INotifyPropertyChanged, INotifyPropertyChanging, IDisposable
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
                if (value != _id)
                {
                    NotifyPropertyChanging("Id");
                    _id = value;
                    NotifyPropertyChanged("Id");
                }
            }
        }

        private DateTime _time;
        [Column]
        public DateTime Time
        {
            get
            {
                return _time;
            }
            set
            {
                NotifyPropertyChanging("Time");
                _time = value;
                NotifyPropertyChanged("Time");
            }
        }

        private string _note;
        [Column]
        public string Note
        {
            get
            {
                return _note;
            }
            set
            {
                if (_note != value)
                {
                    NotifyPropertyChanging("Note");
                    _note = value;
                    NotifyPropertyChanged("Note");
                }
            }
        }

        private string _location;
        [Column]
        public string Location
        {
            get
            {
                return _location;
            }
            set
            {
                NotifyPropertyChanging("Location");
                _location = value;
                NotifyPropertyChanged("Location");
            }
        }

        private string _imgSrc;
        [Column]
        public string ImgSrc
        {
            get { return _imgSrc; }
            set
            {
                if (_imgSrc != value)
                {
                    NotifyPropertyChanging("ImgSrc");
                    _imgSrc = value;
                    NotifyPropertyChanged("ImgSrc");
                }
            }
        }

        [IgnoreDataMember]
        public Stream ImageStream
        {
            get
            {
                if (_imgSrc == null || _imgSrc.Equals(String.Empty))
                {
                    return null;
                }

                using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (isoStore.FileExists(_imgSrc))
                    {
                        using (var source = isoStore.OpenFile(_imgSrc, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            var stream = new MemoryStream();
                            source.CopyTo(stream);
                            stream.Seek(0, SeekOrigin.Begin);

                            return stream;
                        }
                    }
                }
                return null;
            }
        }

        [IgnoreDataMember]
        public ImageSource SmallThumbImage
        {
            get
            {
                if (_imgSrc == null || _imgSrc.Equals(String.Empty))
                {
                    return null;
                }

                using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (iso.FileExists(_imgSrc))
                    {
                        using (var stream = iso.OpenFile(_imgSrc, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            return PictureDecoder.DecodeJpeg(stream, Constant.ThumbnailSmallSide, Constant.ThumbnailSmallSide);
                        }
                    }
                }

                return null;
            }
        }

        [IgnoreDataMember]
        public ImageSource LargeThumbImage
        {
            get
            {
                if (_imgSrc == null || _imgSrc.Equals(String.Empty))
                {
                    return null;
                }

                using (IsolatedStorageFile iso = IsolatedStorageFile.GetUserStoreForApplication())
                {
                    if (iso.FileExists(_imgSrc))
                    {
                        using (var stream = iso.OpenFile(_imgSrc, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            return PictureDecoder.DecodeJpeg(stream, Constant.ThumbnailLargeSide, Constant.ThumbnailLargeSide);
                        }
                    }
                }

                return null;
            }
        }

        [Column]
        internal int _dateId;

        private EntityRef<DateData> _date;

        [Association(Storage = "_date", OtherKey = "Id", ThisKey = "_dateId", IsForeignKey = true)]
        public DateData Date
        {
            get { return _date.Entity; }
            set
            {
                NotifyPropertyChanging("Date");
                _date.Entity = value;
                if (value != null)
                {
                    _dateId = value.Id;
                }
                NotifyPropertyChanged("Date");
            }
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

        public void Dispose()
        {

        }
    }
}
