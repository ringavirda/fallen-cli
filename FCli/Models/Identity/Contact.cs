// FCli namespaces.
using FCli.Models.Dtos;

namespace FCli.Models.Identity;

/// <summary>
/// Represents a known user in the identity storage.
/// </summary>
public class Contact
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public List<string> Aliases { get; set; } = new();

    public IdentityChangeRequest ToChangeRequest()
        => new()
        {
            Name = Name,
            Email = Email,
            Aliases = Aliases
        };
}
