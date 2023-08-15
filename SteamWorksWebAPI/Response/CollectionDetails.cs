namespace SteamWorksWebAPI
{
    public class CollectionDetails
    {
        public CollectionElement[] Children { get; set; } = new CollectionElement[0];

        public string PublishedFileId { get; set; } = string.Empty;

        public int Result { get; set; }
    }
}