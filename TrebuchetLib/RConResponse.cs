namespace TrebuchetLib;

public class RConResponse
{
    public RConResponse(int id, string response)
    {
        Id = id;
        Response = response;
    }

    public RConResponse(int id, Exception exception)
    {
        Id = id;
        Response = exception.Message;
        Exception = exception;
    }
    
    public RConResponse(Exception exception)
    {
        Id = -1;
        Response = exception.Message;
        Exception = exception;
    }

    public Exception? Exception { get; }
    public int Id { get; }
    public string Response { get; }

    public bool IsEmpty => Id == -1 && string.IsNullOrEmpty(Response);

    public static RConResponse Empty
    {
        get => new RConResponse(-1, string.Empty);
    }
}