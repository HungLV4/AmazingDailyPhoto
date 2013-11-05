using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Commons
{
    public class Constant
    {
        public const string DBConnectionString = "Data Source=isostore:/GrowUp.sdf";

        public const int JpegQuality = 80;
        public const int DefaultCameraResolutionWidth = 640;
        public const int DefaultCameraResolutionHeight = 480;
        public const int ThumbnailLargeSide = 300;
        public const int FilterPreviewSize = 108;
        public const int ThumbnailSmallSide = 100;

        public const int GENERATED_CALENDAR_WIDTH = 480;
        public const int GENERATED_IMAGE_WIDTH = 1280;
        public const int GENERATED_IMAGE_HEIGHT = 720;

        public const string SETTING_LOCATION = "settingLocation";
        public const string SETTING_FIRST_TIME = "settingFirstTime";
        public const string SETTING_BACKGROUND_IDX = "settingBackgroundIdx";
        public const string SETTING_FONT_IDX = "settingFontIdx";

        public const string MSG_LOCATION_WARNING = "Would you allow the application to use your GPS position?";
        public const string MSG_LOCATION_TITLE = "Location";
        public const string MSG_NO_INTERNET_CONNECTION = "Network is not avaiable";
        public const string MSG_NO_GPS_CONNECTION = "Location service is not avaiable.";
        public const string MSG_IMAGE_SAVED = "Image is saved to camera roll";
        public const string MSG_APP_BACKGROUND_AGENT_DESCRIPTION = "";

        public const string GOOGLE_REVERSE_GEOCODING_URI = "http://maps.googleapis.com/maps/api/geocode/xml?latlng={0},{1}&sensor=false";
        public const string BING_REVERSE_GEOCODING_URI = "http://dev.virtualearth.net/REST/v1/Locations/{0},{1}?includeEntityTypes=Address&o=xml&key={2}";
        public const string BING_KEY = "AmroOJQT5T6dROJoborfuM5KysrjhZrmVrR8-nZvViTKka1Pemg-49QA-OHwA463";
    }
}
