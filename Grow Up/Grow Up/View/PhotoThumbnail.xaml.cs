﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace Grow_Up.View
{
    public partial class PhotoThumbnail : UserControl, INotifyPropertyChanged
    {
        private WriteableBitmap _bitmap = null;
        private string _text = null;

        public WriteableBitmap Bitmap
        {
            get
            {
                return _bitmap;
            }

            set
            {
                if (_bitmap != value)
                {
                    _bitmap = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Bitmap"));
                    }
                }
            }
        }

        public string Text
        {
            get
            {
                return _text;
            }

            set
            {
                if (_text != value)
                {
                    _text = value;

                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("Text"));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        
        public PhotoThumbnail()
        {
            InitializeComponent();
            DataContext = this;
        }
    }
}
