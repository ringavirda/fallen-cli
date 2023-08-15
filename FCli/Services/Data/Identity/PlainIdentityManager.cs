// Vendor namespaces.
using System.Text.Json;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Models.Identity;
using FCli.Services.Abstractions;

namespace FCli.Services.Data.Identity;

/// <summary>
/// Uses json to store identities. Cashes values.
/// </summary>
public class PlainIdentityManager : IIdentityManager
{
    // DI.
    protected readonly IConfig _config;

    // Cashe.
    protected IdentityStorage? _identityCashe;

    public PlainIdentityManager(IConfig config)
    {
        _config = config;
    }

    public List<Contact>? LoadContacts()
    {
        if (File.Exists(_config.IdentityFilePath))
        {
            // This also updates cashe.
            var storage = TryLoadStorage();
            return storage?.Contacts ?? null;
        }
        else return null;
    }

    public Contact? LoadContact(string nameOrAlias)
        => ContactExists(nameOrAlias)
            ? GetUserFromAlias(_identityCashe, nameOrAlias)
            : null;

    public bool ContactExists(string nameOrAlias)
    {
        if (_identityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null) return false;
        }

        // Search name first.
        var maybeUser = _identityCashe?.Contacts
            .FirstOrDefault(u => u.Name == nameOrAlias);
        if (maybeUser == null)
        {
            // Search alias if name isn't found.
            maybeUser = GetUserFromAlias(_identityCashe, nameOrAlias);
            if (maybeUser == null) return false;
        }
        // User found.
        return true;
    }

    public RootUser GetRootUser()
    {
        if (_identityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                _identityCashe = new();
                FlushStorage(_identityCashe);
                return _identityCashe.RootUser;

            }
            else return storage.RootUser;
        }
        else return _identityCashe.RootUser;
    }

    public void UpdateRootUser(IdentityChangeRequest request)
    {
        // Construct root from request.
        var root = new RootUser()
        {
            Name = request.Name,
            Email = request.Email,
            Password = request.Password
        };

        if (_identityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                _identityCashe = new()
                {
                    RootUser = root
                };
            }
        }
        else _identityCashe.RootUser = root;
        FlushStorage(_identityCashe);
    }

    public void StoreContact(Contact user)
    {
        if (_identityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                _identityCashe = new();
            }
        }
        // Update storage.
        _identityCashe?.Contacts.Add(user);
        FlushStorage(_identityCashe);
    }

    public void DeleteContact(string userName)
    {
        if (ContactExists(userName))
        {
            var user = GetUserFromAlias(_identityCashe, userName);
            if (user != null)
            {
                _identityCashe?.Contacts.Remove(user);
                FlushStorage(_identityCashe);
            }
        }
        else throw new IdentityException(
                $"[Identity] User ({userName}) wasn't found.");
    }

    /// <summary>
    /// Tries to load storage if it exists.
    /// </summary>
    /// <returns>Identity storage, or null if it is not present.</returns>
    /// <exception cref="CriticalException">If storage is corrupted.</exception>
    protected virtual IdentityStorage? TryLoadStorage()
    {
        if (File.Exists(_config.IdentityFilePath))
        {
            var json = File.ReadAllText(_config.IdentityFilePath);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var storage = JsonSerializer.Deserialize<IdentityStorage>(json);
                return _identityCashe = storage;
            }
            catch (JsonException ex)
            {
                throw new CriticalException(
                    "[Identity] Identity storage is corrupted.", ex);
            }
        }
        else
        {
            _identityCashe = new();
            FlushStorage(_identityCashe);
            return _identityCashe;
        }
    }

    /// <summary>
    /// Refreshes the identity storage.
    /// </summary>
    /// <param name="storage">New storage.</param>
    protected virtual void FlushStorage(IdentityStorage? storage)
    {
        var json = JsonSerializer.Serialize(storage);
        File.WriteAllText(_config.IdentityFilePath, json);
        _identityCashe = storage;
    }

    /// <summary>
    /// Checks if user with given selector is present in the storage.
    /// </summary>
    /// <param name="storage">Storage where to look.</param>
    /// <param name="nameOrAlias">Selector to search.</param>
    /// <returns>Contact if found, null if not.</returns>
    protected static Contact? GetUserFromAlias(
        IdentityStorage? storage,
        string nameOrAlias) => storage?.Contacts.FirstOrDefault(
            u => u.Name == nameOrAlias || u.Aliases.Any(a => a == nameOrAlias));
}
