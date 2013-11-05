using Nokia.Graphics.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml.Serialization;
using Windows.Storage.Streams;
using Nokia.InteropServices.WindowsRuntime;

namespace Models
{
    /// <summary>
    /// Photo model is the central piece for keeping the state of the image being edited.
    /// It contains the main image editing session and the filters that have been applied
    /// to the image.
    /// </summary>
    [XmlRootAttribute("PhotoModel", Namespace = "ScheduledLockscreenAgent.Model")]
    public class PhotoModel : IDisposable
    {
        #region Members

        private EditingSession _session = null;

        #endregion

        #region Properties

        /// <summary>
        /// Get and set image data buffer.
        /// </summary>
        [XmlIgnore]
        public IBuffer Buffer
        {
            get
            {
                IBuffer buffer;

                try
                {
                    _session.UndoAll();

                    Task<IBuffer> t = _session.RenderToJpegAsync().AsTask();
                    buffer = t.Result;

                    foreach (FilterModel fm in AppliedFilters)
                    {
                        foreach (IFilter f in fm.Components)
                        {
                            _session.AddFilter(f);
                        }
                    }
                }
                catch (Exception ex)
                {
                    buffer = null;
                }

                return buffer;
            }

            set
            {
                if (_session == null)
                {
                    _session = new EditingSession(value);

                    foreach (FilterModel fm in AppliedFilters)
                    {
                        foreach (IFilter f in fm.Components)
                        {
                            _session.AddFilter(f);
                        }
                    }

                    Width = _session.Dimensions.Width;
                    Height = _session.Dimensions.Height;
                }
                else
                {
                    throw new Exception();
                }
            }
        }

        [XmlArray]
        // art filters
        [XmlArrayItem("NoFilterModel", Type = typeof(NoFilterModel))]
        [XmlArrayItem("GrayscaleFilterModel", Type = typeof(GrayscaleFilterModel))]
        [XmlArrayItem("LomoFilterModel", Type = typeof(LomoFilterModel))]
        [XmlArrayItem("SepiaFilterModel", Type = typeof(SepiaFilterModel))]
        [XmlArrayItem("LoFiFilterModel", Type = typeof(LoFiFilterModel))]
        [XmlArrayItem("OverExposureFilterModel", Type = typeof(OverExposureFilterModel))]

        public List<FilterModel> AppliedFilters { get; set; }

        [XmlIgnore]
        public double Width { get; private set; }

        [XmlIgnore]
        public double Height { get; private set; }

        [XmlAttribute]
        public bool Dirty { get; set; }

        [XmlAttribute]
        public bool Captured { get; set; }

        /// <summary>
        /// Check if there are filters applied that can be removed.
        /// </summary>
        [XmlIgnore]
        public bool CanUndoFilter
        {
            get
            {
                return _session.CanUndo();
            }
        }

        #endregion

        public PhotoModel()
        {
            AppliedFilters = new List<FilterModel>();
            Dirty = false;
            Captured = false;
        }

        public void Dispose()
        {
            if (_session != null)
            {
                _session.Dispose();
            }
        }

        /// <summary>
        /// Renders current image with applied filters to the given bitmap.
        /// </summary>
        /// <param name="bitmap">Bitmap to render to</param>
        public async Task RenderBitmapAsync(WriteableBitmap bitmap)
        {
            await _session.RenderToBitmapAsync(bitmap.AsBitmap());
        }

        /// <summary>
        /// Renders current image with applied filters to a buffer and returns it.
        /// Meant to be used where the filtered image is for example going to be
        /// saved to a file.
        /// </summary>
        /// <returns>Buffer containing the filtered image data</returns>
        public async Task<IBuffer> RenderFullBufferAsync()
        {
            IBuffer buffer;

            try
            {
                buffer = await _session.RenderToJpegAsync();
            }
            catch (Exception)
            {
                buffer = null;
            }

            return buffer;
        }

        /// <summary>
        /// Renders a thumbnail of requested size from the center of the current image with
        /// filters applied.
        /// </summary>
        /// <param name="side">Side length of square thumbnail to render</param>
        /// <returns>Rendered thumbnail bitmap</returns>
        public async Task<Bitmap> RenderThumbnailBitmapAsync(int side)
        {
            int minSide = (int)Math.Min(Width, Height);

            Windows.Foundation.Rect rect = new Windows.Foundation.Rect()
            {
                Width = minSide,
                Height = minSide,
                X = (Width - minSide) / 2,
                Y = (Height - minSide) / 2,
            };

            _session.AddFilter(FilterFactory.CreateCropFilter(rect));

            Bitmap bitmap = new Bitmap(new Windows.Foundation.Size(side, side), ColorMode.Ayuv4444);

            try
            {
                await _session.RenderToBitmapAsync(bitmap, OutputOption.Stretch);
            }
            catch (Exception ex)
            {
                bitmap = null;
            }

            _session.Undo();

            return bitmap;
        }

        /// <summary>
        /// Apply filter to image. Notice that FilterModel may consist of many IFilter components.
        /// </summary>
        /// <param name="filter">Filter to apply</param>
        public void ApplyFilter(FilterModel filter)
        {
            AppliedFilters.Add(filter);

            // cropping photo
            int minSide = (int)Math.Min(Width, Height);
            Windows.Foundation.Rect rect = new Windows.Foundation.Rect()
            {
                Width = minSide,
                Height = minSide,
                X = (Width - minSide) / 2,
                Y = (Height - minSide) / 2,
            };
            _session.AddFilter(FilterFactory.CreateCropFilter(rect));

            foreach (IFilter f in filter.Components)
            {
                _session.AddFilter(f);
            }

            Width = _session.Dimensions.Width;
            Height = _session.Dimensions.Height;
        }

        /// <summary>
        /// Undo last applied filter (if any).
        /// </summary>
        public void UndoFilter()
        {
            if (CanUndoFilter)
            {
                AppliedFilters.RemoveAt(AppliedFilters.Count - 1);

                _session.Undo();

                Width = _session.Dimensions.Width;
                Height = _session.Dimensions.Height;
            }
        }

        public void UndoAllFilters()
        {
            if (CanUndoFilter)
            {
                _session.UndoAll();

                Width = _session.Dimensions.Width;
                Height = _session.Dimensions.Height;
            }
        }
    }
}
