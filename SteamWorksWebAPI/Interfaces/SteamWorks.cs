using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;

namespace SteamWorksWebAPI
{
    public static class SteamWorks
    {
        public static JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public const string APIUserHost = "api.steampowered.com";
        public const string SteamCommunityHost = "steamcommunity.com";

        public static string MakeURL(string host, string steamInterface, string method, int version)
        {
            return $"https://{host}/{steamInterface}/{method}/v{version}/";
        }

        public static async Task<T?> PostAsync<T>(string url, Query query, CancellationToken token) where T : class
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);
                HttpContent content = new FormUrlEncodedContent(query.GetQueryArguments());
                content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

                using (var response = await client.PostAsync(url, content, token))
                {
                    using (var download = await response.Content.ReadAsStreamAsync(token))
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK) return null;

                        return (await JsonSerializer.DeserializeAsync<APIResponse<T>>(download, jsonOptions))?.Response;
                    }
                }
            }
        }

        public static async Task<T?> GetAsync<T>(string url, Query query, CancellationToken token) where T : class
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                NameValueCollection urlQuery = new NameValueCollection();
                foreach (var q in query.GetQueryArguments())
                    urlQuery.Add(q.Key, q.Value);

                UriBuilder builder = new UriBuilder(url);
                builder.Query = urlQuery.ToString();

                using (var response = await client.GetAsync(builder.Uri, token))
                {
                    using (var download = await response.Content.ReadAsStreamAsync(token))
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK) return null;

                        return (await JsonSerializer.DeserializeAsync<APIResponse<T>>(download, jsonOptions))?.Response;
                    }
                }
            }
        }
    }
}
