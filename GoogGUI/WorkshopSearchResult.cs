using Goog;
using GoogLib;
using System.Drawing;

namespace GoogGUI
{
    public class WorkshopSearchResult
    {
        private string _authorName = string.Empty;
        private SteamWebSearchResult _result;

        public WorkshopSearchResult(SteamWebSearchResult result)
        {
            _result = result;
        }

        public string AuthorName => _result.authorName;

        public string ImageURL => _result.previewURL;

        public string ModID => _result.modID;

        public SteamPublishedFile PublishedFile
        {
            get
            {
                return new SteamPublishedFile
                {
                    publishedFileID = _result.modID,
                    previewUrl = _result.previewURL,
                    title = _result.modName
                };
            }
        }

        public string Title => _result.modName;
    }
}