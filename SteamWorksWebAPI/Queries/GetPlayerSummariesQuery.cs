namespace SteamWorksWebAPI
{
    public class GetPlayerSummariesQuery : Query
    {
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// List of SteamIDs (max: 100)
        /// </summary>
        public List<string> SteamIDs { get; set; } = new List<string>();
    }
}