using FCli.Models;

namespace FCli.Services.Data;

public interface ICommandLoader
{
    public Command LoadCommand(string name);
    public List<Command> LoadCommands();
    public void SaveCommand(Command command);
    public bool CommandExists(string name);
    public void DeleteCommand(string name);
}
