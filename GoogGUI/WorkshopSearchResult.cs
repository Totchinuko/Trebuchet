using Goog;
using GoogLib;
using System.ComponentModel;

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

        public string AuthorName =>_result.authorName;

        public string ImageURL => _result.previewURL;

        public string Title => _result.modName;

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
    }
}