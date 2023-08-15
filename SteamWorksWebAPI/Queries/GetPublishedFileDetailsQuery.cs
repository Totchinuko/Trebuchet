namespace SteamWorksWebAPI
{
    public class GetPublishedFileDetailsQuery : Query
    {
        public GetPublishedFileDetailsQuery(IEnumerable<ulong> ids)
        {
            PublishedFileIds = new HashSet<ulong>(ids);
        }

        public GetPublishedFileDetailsQuery(ulong id)
        {
            PublishedFileIds = new HashSet<ulong> { id };
        }

        /// <summary>
        /// published file id to look up
        /// </summary>
        public HashSet<ulong> PublishedFileIds { get; set; } = new HashSet<ulong>();

        public override IEnumerable<KeyValuePair<string, string>> GetQueryArguments()
        {
            yield return new KeyValuePair<string, string>("itemcount", PublishedFileIds.Count.ToString());

            int i = 0;
            foreach (var id in PublishedFileIds)
            {
                yield return new KeyValuePair<string, string>($"publishedfileids[{i}]", id.ToString());
                i++;
            }
        }
    }
}