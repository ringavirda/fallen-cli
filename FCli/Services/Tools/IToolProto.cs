using FCli.Models;
using FCli.Services.Data;

namespace FCli.Services.Tools;

public interface IToolProto
{
    public Tool GetTool(ICommandLoader loader);
}