namespace GoogLib
{
    public class UpdateCheckEventArgs
    {
        public ulong currentBuildID { get; set; } = 0;

        public bool IsUpToDate { get; set; } = false;

        public ulong steamBuildID { get; set; } = 0;
    }
}