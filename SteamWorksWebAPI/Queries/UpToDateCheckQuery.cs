namespace SteamWorksWebAPI
{
    public class UpToDateCheckQuery : Query
    {
        public uint AppID { get; set; } = 0;

        public uint Version { get; set; } = 0;
    }
}