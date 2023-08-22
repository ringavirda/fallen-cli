using FCli.Models.Identity;

namespace FCli.Models.Dtos;

/// <summary>
/// Used to request identity creation or override.
/// </summary>
public class IdentityChangeRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public List<string> Aliases { get; set; } = new();

    public Contact ToContact()
        => new()
        {
            Name = Name,
            Email = Email,
            Aliases = Aliases
        };
}