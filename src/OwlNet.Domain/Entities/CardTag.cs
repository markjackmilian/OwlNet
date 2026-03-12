namespace OwlNet.Domain.Entities;

/// <summary>
/// Join entity that represents the association between a <see cref="Card"/> and a
/// <see cref="ProjectTag"/>.
/// </summary>
/// <remarks>
/// <para>
/// The primary key is the composite <c>(CardId, TagId)</c> — there is no separate <c>Guid Id</c>
/// column. The composite key is configured via EF Core Fluent API in
/// <c>CardTagConfiguration</c>.
/// </para>
/// <para>
/// This entity is intentionally thin: it carries only the two foreign keys and the two
/// navigation properties required by EF Core. All business rules (e.g., same-project
/// constraint, idempotency) are enforced by <see cref="Card.AddTag"/>.
/// </para>
/// </remarks>
public sealed class CardTag
{
    /// <summary>
    /// Gets the foreign key of the <see cref="Entities.Card"/> this association belongs to.
    /// </summary>
    public Guid CardId { get; private set; }

    /// <summary>
    /// Gets the foreign key of the <see cref="ProjectTag"/> being associated with the card.
    /// </summary>
    public Guid TagId { get; private set; }

    /// <summary>
    /// Gets the <see cref="Entities.Card"/> navigation property.
    /// </summary>
    public Card Card { get; private set; } = null!;

    /// <summary>
    /// Gets the <see cref="ProjectTag"/> navigation property.
    /// </summary>
    public ProjectTag Tag { get; private set; } = null!;

    /// <summary>
    /// Required by EF Core for materialization. Do not call directly.
    /// </summary>
    private CardTag() { }

    /// <summary>
    /// Creates a new <see cref="CardTag"/> association between the specified card and tag.
    /// </summary>
    /// <param name="cardId">The identifier of the <see cref="Entities.Card"/>.</param>
    /// <param name="tagId">The identifier of the <see cref="ProjectTag"/>.</param>
    /// <returns>A new <see cref="CardTag"/> instance.</returns>
    /// <remarks>
    /// No business validation is performed here. The caller (<see cref="Card.AddTag"/>) is
    /// responsible for enforcing the same-project constraint and idempotency.
    /// </remarks>
    public static CardTag Create(Guid cardId, Guid tagId)
    {
        return new CardTag
        {
            CardId = cardId,
            TagId = tagId
        };
    }
}
