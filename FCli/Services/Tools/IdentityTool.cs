using System.Globalization;
using System.Text;

using FCli.Exceptions;
using FCli.Models;
using FCli.Models.Dtos;
using FCli.Models.Identity;
using FCli.Models.Types;
using FCli.Services.Abstractions;

namespace FCli.Services.Tools;

public class IdentityTool : ToolBase
{
    // DI.
    private readonly IIdentityManager _identity;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public IdentityTool() : base()
    {
        _identity = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public IdentityTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IIdentityManager identity)
        : base(formatter, resources)
    {
        _identity = identity;

        Description = resources.GetLocalizedString("Identity_Help");
    }

    // Private data.
    private bool _override;
    private bool _remove;
    private bool _list;

    private readonly IdentityChangeRequest _request = new();
    private Contact _original = null!;
    private RootUser _root = null!;

    // Overrides.

    public override string Name => "Identity";
    public override string Description { get; }
    public override List<string> Selectors => new()
    {
        "identity", "id"
    };
    public override ToolType Type => ToolType.Identity;

    protected override void GuardInit()
    {
        // Load root.
        _root = _identity.GetRootUser();
        // List flag ignores everything.
        if (Flags.Any(f => f.Key == "list"))
        {
            _list = true;
            return;
        }
        // Guard against no arg.
        if (string.IsNullOrEmpty(Arg))
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("FCli_ArgMissing"),
                    Name));
            throw new ArgumentException("[Identity] No arg was given.");
        }
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // List ignores everything.
        if (_list) return;

        // Ignore existence check.
        if (flag.Key == "override")
        {
            FlagHasNoValue(flag, Name);
            // Guard against remove.
            if (_remove)
            {
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("Identity_OverRemove"));
                throw new FlagException("[Identity] Flag collision occurred.");
            }
            // Set override.
            _override = true;
        }
        // Remove given.
        else if (flag.Key == "remove")
        {
            FlagHasNoValue(flag, Name);

            // Remove should be single flag.
            if (Flags.Count != 1)
            {
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("Identity_RemoveMultipleFlags"));
                throw new FlagException("[Identity] Flag collision occurred.");
            }
            // Cannot remove root.
            else if (_root.IsRoot(Arg))
            {
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("Identity_RemoveRoot"));
                throw new FlagException("[Identity] Attempted remove root.");
            }
            // Set remove.
            _remove = true;
        }
        // Property flags.
        else if (flag.Key == "email")
        {
            FlagHasValue(flag, Name);

            // Validate given mail.
            var email = ValidateEmail(flag.Value, Name);
            // Set email in request.
            _request.Email = email;
        }
        else if (flag.Key == "name")
        {
            FlagHasValue(flag, Name);

            // Set name in request.
            _request.Name = flag.Value;
        }
        else if (flag.Key == "password")
        {
            FlagHasValue(flag, Name);

            // Password can be changed only in root.
            if (!_root.IsRoot(Arg))
            {
                Formatter.DisplayError(
                    Name,
                    Resources.GetLocalizedString("Identity_NonRootPass"));
                throw new FlagException(
                    "[Identity] Non root password attempt.");
            }
            // Set password in request.
            _request.Password = flag.Value;
        }
        else if (flag.Key == "aliases")
        {
            FlagHasValue(flag, Name);

            // Aliases should be packed as a string separated by spaces.
            var aliases = flag.Value.Split(' ')
                .Where(s => !string.IsNullOrEmpty(s));
            // Set aliases in request.
            _request.Aliases = aliases.ToList();
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override Task ActionAsync()
    {
        // List all contacts.
        if (_list)
        {
            if (_root.IsRoot(Arg))
            {
                Formatter.DisplayInfo(
                    Name,
                    Resources.GetLocalizedString("Identity_DisplayRoot"));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Name"),
                        _root.Name));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Email"),
                        _root.Email));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Password"),
                        !string.IsNullOrEmpty(_root.Password)));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Aliases"),
                        string.Join(", ", _root.Aliases)));
                // Exit tool.
                return Task.CompletedTask;
            }
            var contacts = _identity.LoadContacts();
            // Report if nothing to list.
            if (contacts == null
                || contacts.Count == 0)
            {
                Formatter.DisplayInfo(
                    Name,
                    Resources.GetLocalizedString("Identity_EmptyStorage"));
                return Task.CompletedTask;
            }
            else
            {
                if (string.IsNullOrEmpty(Arg))
                {
                    Formatter.DisplayInfo(
                        Name,
                        Resources.GetLocalizedString(
                            "Identity_DisplayAllContacts"));
                    // List.
                    foreach (var contact in contacts)
                        DisplayContact(contact);
                }
                // Use arg as filter if present.
                else
                {
                    var filtered = contacts
                        .Where(c =>
                            c.Name.Contains(Arg)
                            || c.Aliases.Any(
                                a => a.Contains(Arg)));
                    // Change report if not found.
                    if (!filtered.Any())
                    {
                        Formatter.DisplayInfo(
                            Name,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.GetLocalizedString(
                                    "Identity_NothingFiltered"),
                                Arg));
                    }
                    else
                    {
                        Formatter.DisplayInfo(
                            Name,
                            string.Format(
                                CultureInfo.CurrentCulture,
                                Resources.GetLocalizedString(
                                    "Identity_DisplayFiltered"),
                                Arg));
                        // List.
                        foreach (var contact in filtered)
                            DisplayContact(contact);
                    }
                }
            }
            // Exit tool.
            return Task.CompletedTask;
        }

        // Create user if nothing is specified.
        if (!_override && !_remove)
        {
            // Guard against known name.
            if (_identity.ContactExists(Arg))
            {
                Formatter.DisplayError(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Exists"),
                        Arg));
                throw new ArgumentException(
                    "[Identity] Attempted creating using existing name.");
            }
            // Guard against root.
            else if (_root.IsRoot(Arg))
            {
                Formatter.DisplayError(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_CreateRoot"),
                        Arg));
                throw new ArgumentException("[Identity] Root creation.");

            }
            else
            {
                // Construct new identity.
                _request.Name = Arg;
                if (string.IsNullOrEmpty(_request.Email))
                {
                    Formatter.DisplayError(
                        Name,
                        Resources.GetLocalizedString("Identity_CreateNoEmail"));
                    throw new FlagException(
                        "[Identity] No email flag was given during creation.");
                }
                Formatter.DisplayInfo(
                    Name,
                    Resources.GetLocalizedString("Identity_Constructed"));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Name"),
                        _request.Name));
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Email"),
                        _request.Email));
                if (_request.Aliases.Count > 0)
                {
                    Formatter.DisplayMessage(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            Resources.GetLocalizedString("Identity_Aliases"),
                            string.Join(", ", _request.Aliases)));
                }
                // Store constructed entity.
                Formatter.DisplayMessage(
                    Resources.GetLocalizedString("FCli_Saving"));
                _identity.StoreContact(_request.ToContact());
                Formatter.DisplayInfo(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Stored"),
                        _request.Name));
            }
            // Exit tool.
            return Task.CompletedTask;
        }

        // Parse Arg.
        // Root change requested.
        if (_root.IsRoot(Arg))
        {
            _original = _root;
        }
        // Try extract contact.
        else if (_identity.ContactExists(Arg))
        {
            _original = _identity.LoadContact(Arg)
                ?? throw new CriticalException(
                    "[Identity] User was found but didn't load.");
        }
        else
        {
            Formatter.DisplayError(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Identity_Unknown"),
                    Arg));
            throw new ArgumentException(
                "[Identity] Unknown identity specified.");
        }
        // Change existing identity.
        if (_override)
        {
            // Guard against no change.
            if (!CheckChange())
            {
                Formatter.DisplayInfo(
                    Name,
                    Resources.GetLocalizedString("Identity_NoChanges"));
                // Exit tool.
                return Task.CompletedTask;
            }
            // Report request.
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Identity_ChangeWarning"),
                    _original.Name));
            Formatter.DisplayMessage(
                Resources.GetLocalizedString("Identity_OldNew"));
            if (_original.Name != _request.Name)
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_OverrideName"),
                        _original.Name,
                        _request.Name));
            if (_original.Email != _request.Email)
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_OverrideEmail"),
                        _original.Email,
                        _request.Email));
            if (_root.IsRoot(Arg)
                && ((RootUser)_original).Password != _request.Password)
            {
                var hiddenPassword = _root.Password
                    .Select(c => c == ' ' ? c : '*')
                    .Aggregate(new StringBuilder(), (sb, c) => sb.Append(c))
                    .ToString();
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_OverridePassword"),
                        hiddenPassword,
                        _request.Password));
            }
            if (!_original.Aliases.All(a => _request.Aliases.Contains(a)))
                Formatter.DisplayMessage(
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_OverrideAliases"),
                        string.Join(", ", _request.Aliases)));
            // Exit if user decline.
            if (!UserConfirm()) return Task.CompletedTask;
            else
            {
                // Compile request.
                _request.Name = string.IsNullOrEmpty(_request.Name)
                    ? _original.Name
                    : _request.Name;
                _request.Email = string.IsNullOrEmpty(_request.Email)
                    ? _original.Email
                    : _request.Email;
                _request.Aliases = _request.Aliases.Count == 0
                    ? _original.Aliases
                    : _request.Aliases;

                // Change logic if root change.
                if (_root.IsRoot(Arg))
                {
                    _request.Password = string.IsNullOrEmpty(_request.Password)
                        ? _root.Password
                        : _request.Password;
                    _identity.UpdateRootUser(_request);
                }
                else
                {
                    _identity.DeleteContact(_original.Name);
                    _identity.StoreContact(_request.ToContact());
                }
                // Confirm update.
                Formatter.DisplayInfo(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Changed"),
                        _root.IsRoot(Arg) ? Arg : _original.Name));
            }
        }
        // Delete identity from storage.
        else if (_remove)
        {
            Formatter.DisplayWarning(
                Name,
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Identity_RemoveWarning"),
                    _original.Name));
            // Exit if user declined.
            if (!UserConfirm()) return Task.CompletedTask;
            else
            {
                _identity.DeleteContact(_original.Name);
                Formatter.DisplayInfo(
                    Name,
                    string.Format(
                        CultureInfo.CurrentCulture,
                        Resources.GetLocalizedString("Identity_Removed"),
                        _original.Name));
            }
        }
        // Guard against stupidity.
        else throw new CriticalException(
            "[Identity] Execution should never get here.");
        // Final.
        return Task.CompletedTask;
    }

    /// <summary>
    /// Displays contact for the contact list.
    /// </summary>
    /// <param name="contact">Contact to display.</param>
    private void DisplayContact(Contact contact)
    {
        Formatter.DisplayMessage($"{contact.Name}: {contact.Email}");
        if (contact.Aliases.Count > 0)
        {
            Formatter.DisplayMessage(
                string.Format(
                    CultureInfo.CurrentCulture,
                    Resources.GetLocalizedString("Identity_Aliases"),
                    string.Join(", ", contact.Aliases)));
        }
    }

    /// <summary>
    /// Checks if there is any difference between request and original.
    /// </summary>
    /// <returns>True if difference is found.</returns>
    private bool CheckChange()
    {
        if (_original.Name == _request.Name
            && _original.Email == _request.Email
            && _original.Aliases.All(a => _request.Aliases.Contains(a)))
        {
            return _root.IsRoot(Arg)
                && _root.Password
                != _request.Password;
        }
        return true;
    }
}