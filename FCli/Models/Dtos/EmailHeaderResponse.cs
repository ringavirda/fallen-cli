namespace FCli.Models.Dtos;

/// <summary>
/// Represents email summary.
/// </summary>
public class EmailHeaderResponse
{
    public int Index { get; internal set; }
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRead { get; set; }
}
