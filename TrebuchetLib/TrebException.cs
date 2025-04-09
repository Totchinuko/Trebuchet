using System.Runtime.Serialization;

namespace TrebuchetLib;

public class TrebException : Exception
{
    public TrebException()
    {
    }

    public TrebException(string? message) : base(message)
    {
    }

    public TrebException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}