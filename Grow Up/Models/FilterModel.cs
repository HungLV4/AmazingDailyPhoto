using Nokia.Graphics.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Models
{
    public abstract class FilterModel
    {
        #region Properties

        [XmlIgnore]
        public string Name { get; set; }

        [XmlIgnore]
        public Queue<IFilter> Components { get; set; }

        #endregion

        public FilterModel()
        {
            Components = new Queue<IFilter>();
        }
    }

    #region Art filters
    public class NoFilterModel : FilterModel
    {
        public NoFilterModel()
        {
            Name = "Original";
        }
    }

    public class OverExposureFilterModel : FilterModel
    {
        public OverExposureFilterModel()
        {
            Name = "Over Exposure";
            Components.Enqueue(FilterFactory.CreateExposureFilter(ExposureMode.Natural, 0.2));
        }
    }

    public class LoFiFilterModel : FilterModel
    {
        public LoFiFilterModel()
        {
            Name = "Lo-Fi";
            Components.Enqueue(FilterFactory.CreateContrastFilter(0.5f));
            Components.Enqueue(FilterFactory.CreateHueSaturationFilter(128, 64));
        }
    }

    public class GrayscaleFilterModel : FilterModel
    {
        public GrayscaleFilterModel()
        {
            Name = "Grayscale";
            Components.Enqueue(FilterFactory.CreateGrayscaleFilter());
        }
    }

    public class LomoFilterModel : FilterModel
    {
        public LomoFilterModel()
        {
            Name = "Lomo";
            Components.Enqueue(FilterFactory.CreateLomoFilter(0.5f, 0.75f, LomoVignetting.High, LomoStyle.Neutral));
        }
    }


    public class SepiaFilterModel : FilterModel
    {
        public SepiaFilterModel()
        {
            Name = "Sepia";
            Components.Enqueue(FilterFactory.CreateSepiaFilter());
        }
    }

    #endregion
}
