using System.Threading.Tasks;

namespace Trebuchet.ViewModels;


public delegate Task IPublishedModFileArgs(IPublishedModFile file);
public interface IPublishedModFile : IModFile
{
    ulong PublishedId { get; }
}