namespace SteamWorksWebAPI
{
    public class GetPlayerSummariesQuery : Query
    {
        public GetPlayerSummariesQuery(string apikey)
        {
            Key = apikey;
        }

        public GetPlayerSummariesQuery(string apikey, IEnumerable<string> ids)
        {
            Key = apikey;
            SteamIDs = new List<string>(ids);
        }

        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// List of SteamIDs (max: 100)
        /// </summary>
        public List<string> SteamIDs { get; set; } = new List<string>();

        public override IEnumerable<KeyValuePair<string, string>> GetQueryArguments()
        {
            yield return new KeyValuePair<string, string>("key", Key);
            yield return new KeyValuePair<string, string>("steamids", string.Join(',', SteamIDs));
        }
    }
}