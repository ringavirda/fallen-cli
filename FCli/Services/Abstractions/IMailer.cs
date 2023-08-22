using FCli.Models.Dtos;

namespace FCli.Services.Abstractions;

/// <summary>
/// Represents mailing subsystem. Allows to send, read and delete mail.
/// </summary>
public interface IMailer
{
    /// <summary>
    /// List given amount of email headers from the end.
    /// </summary>
    /// <param name="amount">Amount of mail to load.</param>
    /// <returns>Loaded headers.</returns>
    public Task<List<EmailHeaderResponse>> ListHeadersAsync(int amount);

    /// <summary>
    /// Loads full message with the given index.
    /// </summary>
    /// <param name="index">Email identifier.</param>
    /// <returns>Loaded email.</returns>
    public Task<EmailMessageResponse> ReadMessageAsync(int index);

    /// <summary>
    /// Performs send email task.
    /// </summary>
    /// <param name="request">Necessary email information.</param>
    public Task SendMessageAsync(SendEmailRequest request);

    /// <summary>
    /// Deletes email with given index from the provider.
    /// </summary>
    /// <param name="index">Deleting email identifier.</param>
    public Task DeleteMessageAsync(int index);
}