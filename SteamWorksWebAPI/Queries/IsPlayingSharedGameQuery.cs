using System.Text.Json.Serialization;

namespace SteamWorksWebAPI
{
    public class IsPlayingSharedGameQuery : Query
    {
        /// <summary>
        /// The game player is currently playing
        /// </summary>
        [JsonPropertyName("appid_playing")]
        public uint AppIDPlayer { get; set; } = 0;

        /// <summary>
        /// Access key
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// The player we're asking about
        /// </summary>
        public ulong SteamID { get; set; } = 0;
    }
}