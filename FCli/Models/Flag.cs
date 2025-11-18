namespace FCli.Models;

/// <summary>
/// Key-Value record that represents command line flag.
/// </summary>
/// <param name="Key">Flag selector.</param>
/// <param name="Value">Flag argument.</param>
public record Flag(
    string Key,
    string Value
);