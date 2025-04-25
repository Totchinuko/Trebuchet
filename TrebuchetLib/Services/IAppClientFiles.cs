using System.Collections;

namespace TrebuchetLib.Services;

public interface IAppClientFiles : IAppFileHandler<ClientProfile, ClientProfileRef>, IAppFileHandlerWithSize<ClientProfile, ClientProfileRef>
{
}