﻿using GoogLib;
using HtmlAgilityPack;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Web;

namespace Goog
{
    public class SteamWorkWebAPI
    {
        public JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            IncludeFields = true
        };

        #region constants

        public const string httpUserProfileURL = "https://steamcommunity.com/profiles/{0}";
        public const string httpSearchURL = "https://steamcommunity.com/workshop/browse/";
        public const string PublishedCollectionCount = "collectioncount";
        public const string PublishedCollectionURL = "https://api.steampowered.com/ISteamRemoteStorage/GetCollectionDetails/v1/";
        public const string PublishedFileArg = "publishedfileids[{0}]";
        public const string PublishedFileCount = "itemcount";
        public const string PublishedFilesURL = "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/";

        #endregion constants

        private string _apikey;
        private Dictionary<string, string> _usernamesCache = new Dictionary<string, string>();

        public SteamWorkWebAPI(string apikey)
        {
            _apikey = apikey;
        }

        public Dictionary<string, string> UsernamesCache => _usernamesCache;

        public async Task<List<SteamWebSearchResult>> ExtractWebSearch(string search, CancellationToken token)
        {
            List<SteamWebSearchResult> results = new List<SteamWebSearchResult>();
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "appid", "440900" },
                { "searchtext", search },
                { "childpublishedfileid", "0" },
                { "browsesort", "textsearch" },
                { "section", "readytouseitems" }
            };

            string html = await RequestGetHTMLAsync(httpSearchURL, parameters, token);

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(html);
            var nodes = htmldoc.DocumentNode.SelectNodes("//div[@class='workshopBrowseItems']/div[@class='workshopItem']");

            foreach(var node in nodes)
            {
                var ugcLink = node.SelectSingleNode(".//a[contains(@class,'ugc')]");
                var title = node.SelectSingleNode(".//div[contains(@class,'workshopItemTitle')]");
                var author = node.SelectSingleNode(".//a[contains(@class,'workshop_author_link')]");
                var preview = node.SelectSingleNode(".//img[contains(@class,'workshopItemPreviewImage')]");

                string id = ugcLink.GetAttributeValue("data-publishedfileid", string.Empty);
                if (!string.IsNullOrEmpty(id) && long.TryParse(id, out _))
                    results.Add(new SteamWebSearchResult { 
                        modID = id,
                        modName = title?.InnerText ?? string.Empty,
                        authorName = author?.InnerText ?? string.Empty,
                        previewURL = ParsePreviewURL(preview?.GetAttributeValue("src", string.Empty) ?? string.Empty)
                    });
            }

            return results.ToList();
        }

        public async Task<string> ExtractUserName(string steamID, CancellationToken token)
        {
            if (_usernamesCache.TryGetValue(steamID, out string? cachedName))
                return cachedName;

            string html = await RequestGetHTMLAsync(string.Format(httpUserProfileURL, steamID), null, token);
            if (string.IsNullOrEmpty(html)) return string.Empty;

            var htmldoc = new HtmlDocument();
            htmldoc.LoadHtml(html);
            var node = htmldoc.DocumentNode.SelectSingleNode("//title");
            string name = node.GetDirectInnerText().Split("::", StringSplitOptions.TrimEntries)[1];
            _usernamesCache[steamID] = name;

            return name;
        }

        public async Task<Dictionary<string, string>> ExtractUserNames(List<string> steamUserIDs, CancellationToken token)
        {
            Dictionary<string, string> results = new Dictionary<string, string>();
            foreach (string userID in steamUserIDs)
            {
                string name = await ExtractUserName(userID, token);
                if (!string.IsNullOrEmpty(name))
                    results.TryAdd(userID, name);
            }

            return results;
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

        protected virtual async Task<string> RequestGetHTMLAsync(string url, Dictionary<string, string>? getRequest, CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                UriBuilder builder = new UriBuilder(url);
                if (getRequest != null)
                {
                    var query = HttpUtility.ParseQueryString(builder.Query);
                    foreach (KeyValuePair<string, string> p in getRequest)
                        query.Set(p.Key, p.Value);
                    builder.Query = query.ToString();
                }

                using (var response = await client.GetAsync(builder.ToString(), token))
                {
                    using (var download = await response.Content.ReadAsStreamAsync(token))
                    {
                        using (StreamReader sr = new StreamReader(download))
                        {
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                return await sr.ReadToEndAsync(token);
                            else
                                return string.Empty;
                        }
                    }
                }
            }
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
                            if (response.StatusCode == System.Net.HttpStatusCode.OK)
                                return await sr.ReadToEndAsync(token);
                            else
                                return string.Empty;
                        }
                    }
                }
            }
        }

        protected virtual string ParsePreviewURL(string url)
        {
            if (string.IsNullOrEmpty(url)) return string.Empty;

            UriBuilder builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query.Remove("imcolor");
            query.Remove("letterbox");
            builder.Query = query.ToString();
            return builder.ToString();
        }
    }
}