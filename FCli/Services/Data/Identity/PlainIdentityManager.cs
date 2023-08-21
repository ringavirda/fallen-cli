using System.Text.Json;

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
    private readonly IConfig _config;


    public PlainIdentityManager(IConfig config)
    {
        _config = config;
    }

    protected IConfig Config => _config;
    protected IdentityStorage? IdentityCashe { get; set; }

    public List<Contact>? LoadContacts()
    {
        if (File.Exists(Config.IdentityFilePath))
        {
            // This also updates cashe.
            var storage = TryLoadStorage();
            return storage?.Contacts ?? null;
        }
        else return null;
    }

    public Contact? LoadContact(string nameOrAlias)
        => ContactExists(nameOrAlias)
            ? GetUserFromAlias(IdentityCashe, nameOrAlias)
            : null;

    public bool ContactExists(string nameOrAlias)
    {
        if (IdentityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null) return false;
        }

        // Search name first.
        var maybeUser = IdentityCashe?.Contacts
            .FirstOrDefault(u => u.Name == nameOrAlias);
        if (maybeUser == null)
        {
            // Search alias if name isn't found.
            maybeUser = GetUserFromAlias(IdentityCashe, nameOrAlias);
            if (maybeUser == null) return false;
        }
        // User found.
        return true;
    }

    public RootUser GetRootUser()
    {
        if (IdentityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                IdentityCashe = new();
                FlushStorage(IdentityCashe);
                return IdentityCashe.RootUser;

            }
            else return storage.RootUser;
        }
        else return IdentityCashe.RootUser;
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

        if (IdentityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                IdentityCashe = new()
                {
                    RootUser = root
                };
            }
        }
        else IdentityCashe.RootUser = root;
        FlushStorage(IdentityCashe);
    }

    public void StoreContact(Contact user)
    {
        if (IdentityCashe == null)
        {
            var storage = TryLoadStorage();
            if (storage == null)
            {
                IdentityCashe = new();
            }
        }
        // Update storage.
        IdentityCashe?.Contacts.Add(user);
        FlushStorage(IdentityCashe);
    }

    public void DeleteContact(string userName)
    {
        if (ContactExists(userName))
        {
            var user = GetUserFromAlias(IdentityCashe, userName);
            if (user != null)
            {
                IdentityCashe?.Contacts.Remove(user);
                FlushStorage(IdentityCashe);
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
        if (File.Exists(Config.IdentityFilePath))
        {
            var json = File.ReadAllText(Config.IdentityFilePath);
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var storage = JsonSerializer.Deserialize<IdentityStorage>(json);
                return IdentityCashe = storage;
            }
            catch (JsonException ex)
            {
                throw new CriticalException(
                    "[Identity] Identity storage is corrupted.", ex);
            }
        }
        else
        {
            IdentityCashe = new();
            FlushStorage(IdentityCashe);
            return IdentityCashe;
        }
    }

    /// <summary>
    /// Refreshes the identity storage.
    /// </summary>
    /// <param name="storage">New storage.</param>
    protected virtual void FlushStorage(IdentityStorage? storage)
    {
        var json = JsonSerializer.Serialize(storage);
        File.WriteAllText(Config.IdentityFilePath, json);
        IdentityCashe = storage;
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