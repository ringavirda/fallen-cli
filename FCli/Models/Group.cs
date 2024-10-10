using FCli.Models.Dtos;

namespace FCli.Models;

/// <summary>
/// Abstraction for a sequence of commands.
/// </summary>
public class Group : Command
{
    /// <summary>
    /// Command designators, stored in an execution sequence.
    /// </summary>
    public List<string> Sequence { get; init; } = [];

    /// <summary>
    /// Transforms this group to an alteration request.
    /// </summary>
    /// <returns>AlterRequest for this group.</returns>
    public new GroupAlterRequest ToAlterRequest()
        => new()
        {
            Name = Name,
            Sequence = Sequence
        };
}