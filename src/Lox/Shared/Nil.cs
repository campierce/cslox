namespace Lox;

/// <summary>
/// Represents the value of Lox's `nil` literal.
/// <para>
/// Why bother with a dedicated class, when jlox uses `null` for that purpose? If we did the same
/// then <see cref="Expr.Literal.Value"/> must be typed as an `object?`. That would work, but it
/// reads like "literals can exist without a value," which is awkward and requires the added
/// knowledge/handling that `null` actually maps onto another concept.
/// </para>
/// </summary>
internal class Nil
{
    /// <summary>
    /// The one true Nil. Its one-ness means `nil == nil` is true without any special handling.
    /// </summary>
    public static Nil Instance { get; } = new();

    /// <summary>
    /// Returns a new <see cref="Expr.Literal"/> whose value is <see cref="Instance"/>.
    /// </summary>
    public static Expr.Literal Literal => new(Instance);

    /// <summary>
    /// Creates a new Nil.
    /// </summary>
    private Nil()
    {
    }

    public override string ToString() => "nil";
}
