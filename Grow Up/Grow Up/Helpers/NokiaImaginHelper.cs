using Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

namespace Grow_Up.Helpers
{
    public class NokiaImaginHelper
    {
        public static void PreparePhoto(MemoryStream photoStream, MemoryStream thumbnailStream)
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
        }
    }
}
