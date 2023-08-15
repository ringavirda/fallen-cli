// FCli namespaces.
using FCli.Models.Dtos;

namespace FCli.Models.Identity;

/// <summary>
/// Represents a root user in the identity storage.
/// </summary>
/// <remarks>
/// Used as sender for emails.
/// </remarks>
public class RootUser : Contact
{
    public string Password { get; set; } = string.Empty;

    public RootUser()
    {
        Name = Environment.UserName;
        Email = string.Empty;
        // Default aliases.
        Aliases.Add("root");
        Aliases.Add("me");
    }

    public bool IsRoot(string selector) 
        => selector == Name || Aliases.Any(a => a == selector);

    public new IdentityChangeRequest ToChangeRequest()
        => new()
        {
            Name = Name,
            Email = Email,
            Aliases = Aliases,
            Password = Password
        };
}