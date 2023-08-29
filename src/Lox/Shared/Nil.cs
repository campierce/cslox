using Lox.AST;

namespace Lox;

/// <summary>
/// Represents the value of Lox's `nil` literal.
/// <para>
/// Why bother with a dedicated class? The book uses Java's `null` for that purpose, after all. But
/// if we did the same, then <see cref="Expr.Literal.Value"/> must be typed as an `object?` (or we
/// overcorrect and disable nullable reference types). That would be fine, but it reads like
/// "literals can exist without a value," which is awkward and requires the added knowledge that
/// `null` actually maps onto another concept.
/// </para>
/// </summary>
internal class Nil
{
    /// <summary>
    /// The one true Nil. Its one-ness means `nil == nil` evaluates to true without any special
    /// handling.
    /// </summary>
    public static Nil Instance { get; } = new();

    /// <summary>
    /// Creates a new Nil.
    /// </summary>
    private Nil()
    {
    }

    /// <summary>
    /// Returns a new <see cref="Expr.Literal"/> whose value is <see cref="Instance"/>.
    /// </summary>
    public static Expr.Literal Literal => new(Instance);

    public override string ToString() => "nil";
}
