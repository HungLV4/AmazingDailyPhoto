using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class FiltersModel
    {
        #region Properties

        public List<FilterModel> ArtisticFilters { get; private set; }

        #endregion

        public FiltersModel()
        {
            LoadArtisticFilters();
        }

        #region Private methods

        private void LoadArtisticFilters()
        {
            ArtisticFilters = new List<FilterModel>();

            ArtisticFilters.Add(new NoFilterModel());
            ArtisticFilters.Add(new LoFiFilterModel());
            ArtisticFilters.Add(new GrayscaleFilterModel());
            ArtisticFilters.Add(new LomoFilterModel());
            ArtisticFilters.Add(new SepiaFilterModel());
            ArtisticFilters.Add(new OverExposureFilterModel());
        }
        #endregion
    }
}
