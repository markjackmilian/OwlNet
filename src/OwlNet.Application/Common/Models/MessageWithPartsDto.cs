namespace OwlNet.Application.Common.Models;

/// <summary>
/// Represents an OpenCode message composed with its ordered collection of parts.
/// Combines a <see cref="MessageDto"/> with the associated <see cref="MessagePartDto"/> entries.
/// </summary>
/// <param name="Message">The message metadata.</param>
/// <param name="Parts">The ordered collection of message parts.</param>
public sealed record MessageWithPartsDto(
    MessageDto Message,
    IReadOnlyList<MessagePartDto> Parts);
