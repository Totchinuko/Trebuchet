namespace TrebuchetLib;

public enum UGCStatus
{
    Missing,
    Corrupted,
    Updatable,
    UpToDate
}

public struct UGCFileStatus(ulong publishedId, UGCStatus status)
{
    public ulong PublishedId { get; } = publishedId;
    public UGCStatus Status { get; } = status;

    public static UGCFileStatus Default(ulong publishedId) => new UGCFileStatus(publishedId, UGCStatus.Missing);
}