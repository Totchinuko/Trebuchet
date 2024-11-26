using System.Net.Http.Headers;
using System.Text.Json;

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

#if DEBUG
                        string json = new StreamReader(download).ReadToEnd();

                        try
                        {
                            return (JsonSerializer.Deserialize<APIResponse<T>>(json, jsonOptions))?.Response;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Couldn't Parse Steam Response\nContent\n" + json, ex);
                        }
#else
                        return (await JsonSerializer.DeserializeAsync<APIResponse<T>>(download, jsonOptions))?.Response;
#endif
                    }
                }
            }
        }

        public static async Task<T?> GetAsync<T>(string url, Query query, CancellationToken token) where T : class
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(15);

                UriBuilder builder = new UriBuilder(url);
                builder.Query = query.GetFlatQuery();

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