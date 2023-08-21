// FCli namespaces.
using FCli.Models.Dtos;
using FCli.Models.Identity;

namespace FCli.Services.Abstractions;

/// <summary>
/// Manages identity storage. Used by mail subsystem.
/// </summary>
public interface IIdentityManager
{
    /// <summary>
    /// Loads known contacts form the storage.
    /// </summary>
    /// <returns>List of contacts or null if nothing is stored.</returns>
    public List<Contact>? LoadContacts();
    
    /// <summary>
    /// Retrieves a contact with given selector.
    /// </summary>
    /// <param name="nameOrAlias">Selector to search.</param>
    /// <returns>Loaded contact or null if not found.</returns>
    public Contact? LoadContact(string nameOrAlias);
    
    /// <summary>
    /// Checks if storage contains given name or alias.
    /// </summary>
    /// <param name="nameOrAlias">Selector to check.</param>
    /// <returns>True if found.</returns>
    public bool ContactExists(string nameOrAlias);
    
    /// <summary>
    /// Loads last root user profile.
    /// </summary>
    /// <returns>Current root profile.</returns>
    public RootUser GetRootUser();
    
    /// <summary>
    /// Changes root user profile.
    /// </summary>
    /// <param name="newRootProfile">New profile.</param>
    public void UpdateRootUser(IdentityChangeRequest request);
    
    /// <summary>
    /// Persists new contact.
    /// </summary>
    /// <param name="user">Contact profile.</param>
    public void StoreContact(Contact user);
    
    /// <summary>
    /// Removes given user from storage.
    /// </summary>
    /// <param name="userName">Selector to delete.</param>
    public void DeleteContact(string userName);
}
