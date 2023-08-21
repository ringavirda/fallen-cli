namespace FCli.Models.Dtos;

/// <summary>
/// Necessary info to send an email.
/// </summary>
public class SendEmailRequest
{
    public string ReceiverEmail { get; set; } = string.Empty;
    public string ReceiverName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}
