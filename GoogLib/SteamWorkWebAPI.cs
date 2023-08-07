using GoogLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Goog
{
    public class SteamWorkWebAPI
    {
        public JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        #region constants
        public const string PublishedFilesURL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";
        public const string PublishedCollectionURL = "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
        public const string PublishedCollectionCount = "collectioncount";
        public const string PublishedFileCount = "itemcount";
        public const string PublishedFileArg = "publishedfileids[{0}]";
        #endregion

        private string _apikey;

        public SteamWorkWebAPI(string apikey) 
        {
            _apikey = apikey;
        }

        public async Task<Dictionary<string, SteamPublishedFile>> GetPublishedFiles(List<string> IDs, CancellationToken token)
        {
            Dictionary<string, SteamPublishedFile> manifest = new Dictionary<string, SteamPublishedFile>(IDs.Count);
            Dictionary<string, string> request = new Dictionary<string, string>
            {
                { PublishedFileCount, IDs.Count.ToString() }
            };

            for (int i = 0; i < IDs.Count; i++)
                request.Add(string.Format(PublishedFileArg, i), IDs[i]);

            string json = await RequestPostAsync(PublishedFilesURL, request, token);
            SteamResponse<SteamPublishedFilesResult>? response = JsonSerializer.Deserialize<SteamResponse<SteamPublishedFilesResult>>(json, _jsonOptions);

            if (response == null || response.response == null || response.response.publishedFileDetails == null)
                throw new Exception("Could not parse reponse from steam api.");

            foreach (SteamPublishedFile file in response.response.publishedFileDetails)
                manifest.Add(file.publishedFileID, file);
            return manifest;
        }

        protected virtual async Task<string> RequestPostAsync(string url, Dictionary<string, string> postRequest, CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                HttpContent content = new FormUrlEncodedContent(postRequest);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using (var response = await client.PostAsync(url, content, token))
                {
                    using (var download = await response.Content.ReadAsStreamAsync(token))
                    {
                        using (StreamReader sr = new StreamReader(download))
                        {
                            return await sr.ReadToEndAsync(token);
                        }
                    }
                }
            }
        }
    }
}
