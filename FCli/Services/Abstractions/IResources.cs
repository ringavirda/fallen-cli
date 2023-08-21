namespace FCli.Services.Abstractions;

/// <summary>
/// Wrapper for all interactions with app resources.
/// </summary>
public interface IResources
{
    /// <summary>
    /// Should load the string from the resource files according to the user's
    /// local culture.
    /// </summary>
    /// <param name="name">Name of the resource string .</param>
    /// <returns>Loaded string.</returns>
    public string GetLocalizedString(string name);
}