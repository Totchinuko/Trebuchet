using tot_lib;

namespace Boulder.Commands;

public class LambCommand : ITotCommand, ITotCommandSubCommands
{
    public string Command => "lamb";
    public string Description => "Start a conan process and sacrifice itself";
    public IEnumerable<ITotCommand> GetSubCommands()
    {
        yield return new LambClientCommand();
        yield return new LambServerCommand();
    }
}