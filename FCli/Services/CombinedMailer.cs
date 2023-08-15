// Vendor namespaces.
using MimeKit;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
// FCli namespaces.
using FCli.Exceptions;
using FCli.Models.Dtos;
using FCli.Services.Abstractions;
using FCli.Models.Identity;

namespace FCli.Services;

/// <summary>
/// Uses both SMTP and IMAP to manage mail.
/// </summary>
public class CombinedMailer : IMailer
{
    // DI.
    private readonly ICommandLineFormatter _formatter;
    private readonly IResources _resources;
    private readonly IIdentityManager _identity;

    public CombinedMailer(
        ICommandLineFormatter formatter,
        IResources resources,
        IIdentityManager identity)
    {
        _formatter = formatter;
        _resources = resources;
        _identity = identity;
    }

    // Private data.
    private string _smtpHost = string.Empty;
    private string _imapHost = string.Empty;
    private RootUser _root = null!;

    /// <summary>
    /// Uses IMAP client to read given amount of emails from the default profile.
    /// </summary>
    /// <param name="amount">Amount of messages from the top.</param>
    /// <returns>List of message headers.</returns>
    /// <exception cref="IdentityException">If authentication failed.</exception>
    /// <exception cref="MailException">If list failed.</exception>
    public async Task<List<EmailHeaderResponse>> ListHeadersAsync(int amount)
    {
        // Guard against uninitialized profile.
        InitRoot();
        ValidateMailProfile();

        var response = new List<EmailHeaderResponse>();

        // Construct client and connect.
        using var imap = new ImapClient();
        await imap.ConnectAsync(_imapHost, 993, true);
        try
        {
            await imap.AuthenticateAsync(_root.Email, _root.Password);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_AuthFailed"));
            throw new IdentityException(
                "[Mail] Service authentication failed.",
                ex);
        }
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_Connected"));
        // Open the inbox.
        var inbox = imap.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);
        // Read the mail.
        IList<IMessageSummary> headers;
        try
        {
            headers = await inbox.FetchAsync(
                inbox.Count - amount,
                inbox.Count,
                MessageSummaryItems.Envelope | MessageSummaryItems.Flags);

        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_ListFailed"));
            // Rethrow if list failed.
            throw new MailException("[Mail] Message list fail.", ex);
        }
        finally
        {
            // Disconnect.
            await imap.DisconnectAsync(true);
        }
        // Construct the response.
        foreach (var header in headers)
        {
            response.Add(new()
            {
                Index = header.Index,
                SenderEmail = string.Join(", ",
                    header.Envelope.Sender.Select(s => s.ToString())),
                Subject = header.Envelope.Subject,
                Date = header.Date.LocalDateTime,
                IsRead = header.Flags != null
                && header.Flags.Value.HasFlag(MessageFlags.Seen)
            });
        }
        // Report.
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_ListCompleted"));
        // Return the response.
        return response;
    }

    /// <summary>
    /// Uses IMAP client to read the contents of the email with given index. 
    /// </summary>
    /// <param name="index">Email identifier.</param>
    /// <returns>Email response.</returns>
    /// <exception cref="IdentityException">If authentication failed.</exception>
    /// <exception cref="MailException">If read failed.</exception>
    public async Task<EmailMessageResponse> ReadMessageAsync(int index)
    {
        // Guard against uninitialized profile.
        InitRoot();
        ValidateMailProfile();

        // Construct client and connect.
        using var imap = new ImapClient();
        await imap.ConnectAsync(_imapHost, 993, true);
        try
        {
            await imap.AuthenticateAsync(_root.Email, _root.Password);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_AuthFailed"));
            throw new IdentityException(
                "[Mail] Service authentication failed.",
                ex);
        }
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_Connected"));
        // Open the inbox.
        var inbox = imap.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly);
        // Read the mail.
        MimeMessage email;
        try
        {
            email = await inbox.GetMessageAsync(index);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_ReadFailed"));
            // Rethrow if read failed.
            throw new MailException("[Mail] Message read fail.", ex);
        }
        finally
        {
            // Disconnect.
            await imap.DisconnectAsync(true);
        }
        // Report.
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_ReadCompleted"));
        // Return the response.
        return new EmailMessageResponse()
        {
            Index = index,
            SenderEmail = email.From.First().ToString(),
            SenderName = email.From.First().Name,
            Subject = email.Subject,
            Body = email.TextBody,
            Date = email.Date.LocalDateTime
        };
    }

    /// <summary>
    /// Uses SMTP client to send and email to the given address
    /// </summary>
    /// <exception cref="IdentityException">If authentication failed.</exception>
    /// <exception cref="MailException">If send failed.</exception>
    public async Task SendMessageAsync(SendEmailRequest request)
    {
        // Guard against uninitialized profile.
        InitRoot();
        ValidateMailProfile();

        // Construct the message.
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _root.Name,
            _root.Email));
        message.To.Add(new MailboxAddress(
            request.ReceiverName,
            request.ReceiverEmail));
        message.Subject = request.Subject;
        message.Body = new TextPart("plain")
        {
            Text = string.Format(
                _resources.GetLocalizedString("Mail_SendBodyTemplate"),
                request.Body,
                _root.Name)
        };

        // Construct client and connect.
        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(_smtpHost, 465, true);
        try
        {
            await smtp.AuthenticateAsync(_root.Email, _root.Password);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_AuthFailed"));
            throw new IdentityException(
                "[Mail] Service authentication failed.",
                ex);
        }
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_Connected"));
        // Send the email.
        try
        {
            await smtp.SendAsync(message);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                "Message send fail!");
            // Rethrow if send failed.
            throw new MailException("[Mail] Message send fail.", ex);
        }
        finally
        {
            // Disconnect.
            await smtp.DisconnectAsync(true);
        }
        // Report.
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_SendBodyCompleted"));
    }

    /// <summary>
    /// Use IMAP client to identify and delete message.
    /// </summary>
    /// <param name="index">Message identifier.</param>
    public async Task DeleteMessageAsync(int index)
    {
        // Guard against uninitialized profile.
        InitRoot();
        ValidateMailProfile();

        // Construct client and connect.
        using var imap = new ImapClient();
        await imap.ConnectAsync(_imapHost, 993, true);
        try
        {
            await imap.AuthenticateAsync(_root.Email, _root.Password);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_AuthFailed"));
            throw new IdentityException(
                "[Mail] Service authentication failed.",
                ex);
        }
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_Connected"));
        // Open the inbox.
        var inbox = imap.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadWrite);
        // Delete the email.
        try
        {
            await inbox.AddFlagsAsync(index, MessageFlags.Deleted, false);
        }
        catch (Exception ex)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_DeleteFailed"));
            // Rethrow if delete failed.
            throw new MailException("[Mail] Message delete fail.", ex);
        }
        finally
        {
            // Disconnect.
            await imap.DisconnectAsync(true);
        }
        // Report.
        _formatter.DisplayProgressMessage(
            _resources.GetLocalizedString("Mail_DeleteCompleted"));
    }

    /// <summary>
    /// Setup sender account.
    /// </summary>
    /// <exception cref="MailException">If root isn't setup.</exception>
    private void InitRoot()
    {
        // Extract root.
        _root = _identity.GetRootUser();

        // Figure out hosts.
        var domain = new MailboxAddress(
            null, _root.Email
            ).Domain;
        try
        {
            _smtpHost = domain switch
            {
                "gmail.com" => "smtp.gmail.com",
                "outlook.com" => "smtp-mail.outlook.com",
                _ => throw new MailException(
                    $"[Mail] Unsupported email host [{domain}].")
            };
            _imapHost = domain switch
            {
                "gmail.com" => "imap.gmail.com",
                "outlook.com" => "outlook.office365.com",
                _ => throw new MailException(
                    $"[Mail] Unsupported email host [{domain}].")
            };
        } 
        catch(MailException)
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_UnsupportedHost"));
            throw;
        }
    }

    /// <summary>
    /// Makes sure that mail profile is constructed in the config.
    /// </summary>
    /// <exception cref="IdentityException">If something is wrong.</exception>
    private void ValidateMailProfile()
    {
        if (string.IsNullOrEmpty(_root.Name))
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_NoRootName"));
            throw new IdentityException("[Mail] Root name isn't set.");
        }
        if (string.IsNullOrEmpty(_root.Email))
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_NoRootEmail"));
            throw new IdentityException("[Mail] Root email isn't set.");
        }
        if (string.IsNullOrEmpty(_root.Password))
        {
            _formatter.DisplayError(
                "Mail",
                _resources.GetLocalizedString("Mail_NoRootPassword"));
            throw new IdentityException("[Mail] Root password isn't set.");
        }
    }
}
