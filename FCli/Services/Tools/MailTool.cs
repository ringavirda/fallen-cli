// Vendor namespaces.
using System.Net.Mail;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Models.Types;
using FCli.Services.Abstractions;
using static FCli.Models.Args;

namespace FCli.Services.Tools;

public class MailTool : ToolBase
{
    // DI.
    private readonly IMailer _mailer;
    private readonly IIdentityManager _identity;

    /// <summary>
    /// Empty if used as a descriptor.
    /// </summary>
    public MailTool() : base()
    {
        _mailer = null!;
        _identity = null!;
        Description = string.Empty;
    }

    /// <summary>
    /// Main constructor.
    /// </summary>
    public MailTool(
        ICommandLineFormatter formatter,
        IResources resources,
        IMailer mailer,
        IIdentityManager identity)
        : base(formatter, resources)
    {
        _mailer = mailer;
        _identity = identity;

        Description = _resources.GetLocalizedString("Mail_Help");
    }

    // Private data.
    private SendEmailRequest _request = new();
    private bool _list = false;
    private bool _read = false;
    private bool _remove = false;

    // Override.

    public override string Name => "Mail";
    public override string Description { get; }
    public override List<string> Selectors => new()
    {
        "mail", "m"
    };
    public override ToolType Type => ToolType.Mail;

    protected override void GuardInit()
    {
        // Nothing to guard or init.
    }

    protected override void ProcessNextFlag(Flag flag)
    {
        // Execution flags.
        if (flag.Key == "list")
        {
            FlagHasNoValue(flag, Name);
            ExecutionFlag();
            _list = true;
        }
        else if (flag.Key == "read")
        {
            FlagHasNoValue(flag, Name);
            ExecutionFlag();
            _read = true;
        }
        else if (flag.Key == "remove")
        {
            FlagHasNoValue(flag, Name);
            ExecutionFlag();
            _remove = true;
        }
        // Property flags.
        else if (flag.Key == "to")
        {
            FlagHasValue(flag, Name);

            if (MailAddress.TryCreate(flag.Value, out var address))
            {
                // Set mail recipient.
                _request.ReceiverEmail = address.Address;
                _request.ReceiverEmail = address.User;
            }
            else
            {
                // Treat as possible username.
                _request.ReceiverName = flag.Value;
            }
        }
        else if (flag.Key == "sbj")
        {
            FlagHasValue(flag, Name);

            // Set mail recipient.
            _request.Subject = flag.Value;
        }
        // Throw if flag is unrecognized.
        else UnknownFlag(flag, Name);
    }

    protected override async Task ActionAsync()
    {
        var root = _identity.GetRootUser();
        var cTokenSource = new CancellationTokenSource();
        var cToken = cTokenSource.Token;
        // Handle listing of the mail.
        if (_list)
        {
            _formatter.DisplayInfo(
                Name, 
                _resources.GetLocalizedString("Mail_AttemptList"));
            var progress = _formatter.DrawProgressAsync(cToken);
            progress.Start();
            var headers = string.IsNullOrEmpty(Arg)
                ? await _mailer.ListHeadersAsync(10)
                : await _mailer.ListHeadersAsync(ArgIsPositiveNumeric());
            // Reverse to get historical order.
            headers.Reverse();
            // Parse arg, load 10 messages otherwise.
            cTokenSource.Cancel();
            await Task.Delay(100);
            // Guard against empty inbox.
            if (!headers.Any())
            {
                _formatter.DisplayInfo(
                    Name,
                    _resources.GetLocalizedString("Mail_EmptyInbox"));
                return;
            }
            // Display received mail.
            _formatter.DisplayInfo(
                Name,
                string.Format(
                    _resources.GetLocalizedString("Mail_Listing"),
                    headers.Count,
                    root.Email));
            foreach (var header in headers)
            {
                _formatter.DisplayMessage("");
                _formatter.DisplayMessage(
                    string.Format(
                        _resources.GetLocalizedString("Mail_Header"),
                        header.Index,
                        header.Date,
                        header.SenderName,
                        header.IsRead));
                _formatter.DisplayMessage(
                    $"{header.SenderEmail}: {header.Subject}");
            }
        }
        // Handle reading given email.
        else if (_read)
        {
            var index = ArgIsPositiveNumeric();
            // Read.
            _formatter.DisplayInfo(
                Name, 
                _resources.GetLocalizedString("Mail_AttemptRead"));
            _formatter.DrawProgressAsync(cToken).Start();
            var email = await _mailer.ReadMessageAsync(index);
            cTokenSource.Cancel();
            // Display.
            _formatter.DisplayInfo(
                Name,
                _resources.GetLocalizedString("Mail_ReadSuccess"));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Mail_ReadFrom"),
                    email.SenderName,
                    email.SenderEmail));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Mail_ReadSubject"),
                    email.Subject));
            _formatter.DisplayMessage(_resources.GetLocalizedString("Mail_ReadBody"));
            _formatter.DisplayMessage(email.Body);
        }
        // Handle deleting email.
        else if (_remove)
        {
            var index = ArgIsPositiveNumeric();
            // Confirm.
            _formatter.DisplayWarning(
                Name,
                string.Format(
                    _resources.GetLocalizedString("Mail_DeleteWarning"),
                    index));
            if (!UserConfirm()) return;
            // Delete the email.
            _formatter.DrawProgressAsync(cToken).Start();
            await _mailer.DeleteMessageAsync(index);
            cTokenSource.Cancel();
        }
        // Send mail otherwise.
        else
        {
            // Guard against no message body.
            if (string.IsNullOrEmpty(Arg))
            {
                _formatter.DisplayError(
                    Name,
                    _resources.GetLocalizedString("Mail_NoEmail"));
                throw new ArgumentException(
                    "[Mail] No email body while sending.");
            }
            // Arg is email body.
            _request.Body = Arg;
            // Set default subject if not specified.
            if (string.IsNullOrEmpty(_request.Subject))
                _request.Subject = "FCli";
            // Try identity if possible identity given.
            if (string.IsNullOrEmpty(_request.ReceiverEmail))
            {
                if (_identity.ContactExists(_request.ReceiverName))
                {
                    var contact = _identity.LoadContact(_request.ReceiverName);
                    _request.ReceiverName
                        = contact?.Name ?? _request.ReceiverName;
                    _request.ReceiverEmail
                        = contact?.Email ?? _request.ReceiverEmail;
                }
                // Handle root.
                else if (root.IsRoot(_request.ReceiverName))
                {
                    _request.ReceiverEmail = root.Email;
                    _request.ReceiverName = root.Name;
                }
                // Throw if unknown identity.
                else
                {
                    _formatter.DisplayError(
                        Name,
                        string.Format(
                            _resources.GetLocalizedString("Mail_UnknownIdentity"),
                            _request.ReceiverName));
                    throw new IdentityException(
                        "[Mail] Unknown identity was specified during send.");
                }
            }
            // Display constructed header:
            _formatter.DisplayInfo(
                Name,
                _resources.GetLocalizedString("Mail_SendSummary"));
            _formatter.DisplayMessage(
                string.Format(
                    _resources.GetLocalizedString("Mail_SendTo"),
                    _request.ReceiverName,
                    _request.ReceiverEmail));
            _formatter.DisplayMessage(
                string.Format(
                _resources.GetLocalizedString("Mail_SendSubject"),
                _request.Subject));
            // Verify send.
            _formatter.DisplayWarning(
                Name,
                _resources.GetLocalizedString("Mail_SendWarning"));
            if (!UserConfirm()) return;
            // Send.
            _formatter.DisplayInfo(
                Name,
                _resources.GetLocalizedString("Mail_SendAttempt"));
            _formatter.DrawProgressAsync(cToken).Start();
            await _mailer.SendMessageAsync(_request);
            cTokenSource.Cancel();
        }
    }

    /// <summary>
    /// Asserts that arg is positive numeric value.
    /// </summary>
    /// <returns>Parsed number.</returns>
    /// <exception cref="ArgumentException">If arg is invalid.</exception>
    private int ArgIsPositiveNumeric()
    {
        if (!int.TryParse(Arg, out var number))
        {
            _formatter.DisplayError(
                Name,
                _resources.GetLocalizedString("Mail_ArgNonNumeric"));
            throw new ArgumentException("[Mail] Non numeric list arg");
        }
        else if (number < 0)
        {
            _formatter.DisplayError(
                Name,
                _resources.GetLocalizedString("Mail_ArgNegative"));
            throw new ArgumentException("[Mail] Non numeric list arg");
        }
        return number;
    }

    /// <summary>
    /// Verifies that this flag is the only one used.
    /// </summary>
    /// <exception cref="FlagException">If multiple flags.</exception>
    private void ExecutionFlag()
    {
        if (Flags.Count != 1)
        {
            _formatter.DisplayError(
                Name,
                _resources.GetLocalizedString("Mail_MultipleExecutionFlags"));
            throw new FlagException("[Mail] Tried to execute multiple flags.");
        }
    }
}
