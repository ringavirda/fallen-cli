// Vendor namespaces.
using System.Globalization;
using System.Reflection;
using System.Resources;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Services.Abstractions;

namespace FCli.Services.Data;

/// <summary>
/// Uses ResourceManager class to load strings from app resources.
/// </summary>
public class StringResources : IResources
{
    // Configured resource manager.
    private readonly ResourceManager _manager;

    public StringResources(IConfig config)
    {
        _manager = new ResourceManager(
            config.StringsResourceLocation,
            Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Uses resource manager to extract string according to user's locale.
    /// </summary>
    /// <param name="name">String name in the Strings resource file.</param>
    /// <returns>Loaded string.</returns>
    public string GetLocalizedString(string name) 
        => _manager.GetString(name) ?? StringNotLoaded();

    /// <summary>
    /// Default missing resource message.
    /// </summary>
    /// <returns>To satisfy null operator.</returns>
    private static string StringNotLoaded()
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[FCli] Resource Err: ");
        Console.ResetColor();
        Console.WriteLine($"""
            Text wasn't loaded!
            This may be due to your locale ({CultureInfo.CurrentUICulture.Name}).
            """);
        throw new ResourceNotLoadedException("[Resources] Wasn't able to load.");
    }
}
