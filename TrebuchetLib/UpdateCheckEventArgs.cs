namespace Trebuchet
{
    public class UpdateCheckEventArgs
    {
        public ulong currentBuildID { get; } = 0;

        public bool IsUpToDate { get; } = false;

        public ulong steamBuildID { get; } = 0;

        public Exception? Exception { get; } = null;

        public UpdateCheckEventArgs(ulong currentBuildID, ulong steamBuildID, Exception? exception = null)
        {
            this.currentBuildID = currentBuildID;
            this.steamBuildID = steamBuildID;
            IsUpToDate = currentBuildID == steamBuildID;
            Exception = exception;
        }
    }
}