using System.Text.Json.Serialization;

namespace FCli.Models.Identity;

/// <summary>
/// Object representation of the identity storage.
/// </summary>
[JsonSerializable(typeof(IdentityStorage))]
public class IdentityStorage
{
    public static readonly string StaticCheckSum = "This should be correct";
    public string CheckSum { get; } = StaticCheckSum;
    public RootUser RootUser { get; set; } = new();
    public List<Contact> Contacts { get; set; } = new();
    public string PassFileName { get; set; } = string.Empty;
}