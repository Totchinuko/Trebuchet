namespace Trebuchet.ViewModels;


public delegate void IPublishedModFileArgs(IPublishedModFile file);
public interface IPublishedModFile : IModFile
{
    ulong PublishedId { get; }
}