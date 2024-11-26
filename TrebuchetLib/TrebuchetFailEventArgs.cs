namespace Trebuchet
{
    public class TrebuchetFailEventArgs : EventArgs
    {
        public TrebuchetFailEventArgs(Exception exception, int instance = -1)
        {
            Exception = exception;
            Instance = instance;
        }

        public Exception Exception { get; }

        public int Instance { get; }
    }
}