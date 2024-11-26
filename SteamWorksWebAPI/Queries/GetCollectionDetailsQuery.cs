namespace SteamWorksWebAPI
{
    public class GetCollectionDetailsQuery : Query
    {
        public GetCollectionDetailsQuery(IEnumerable<ulong> ids)
        {
            PublishedFileIds = new HashSet<ulong>(ids);
        }

        public GetCollectionDetailsQuery(ulong id)
        {
            PublishedFileIds = new HashSet<ulong> { id };
        }

        /// <summary>
        /// collection ids to get the details for
        /// </summary>
        public HashSet<ulong> PublishedFileIds { get; set; } = new HashSet<ulong>();

        public override IEnumerable<KeyValuePair<string, string>> GetQueryArguments()
        {
            yield return new KeyValuePair<string, string>("collectioncount", PublishedFileIds.Count.ToString());

            int i = 0;
            foreach (var id in PublishedFileIds)
            {
                yield return new KeyValuePair<string, string>($"publishedfileids[{i}]", id.ToString());
                i++;
            }
        }
    }
}