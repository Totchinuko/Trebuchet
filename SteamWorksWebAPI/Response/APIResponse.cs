namespace SteamWorksWebAPI
{
    public class APIResponse<T>
    {
        public T? Response { get; set; } = default;
    }
}