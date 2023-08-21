namespace FCli.Models.Dtos;

/// <summary>
/// Summary with body.
/// </summary>
public class EmailMessageResponse : EmailHeaderResponse
{
    public string Body { get; set; } = string.Empty;
}
