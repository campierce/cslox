namespace Lox.IR;

/// <summary>
/// Boxed null.
/// If we were to represent Lox's nil with C#'s null, then any literal value must be typed as an
/// object? (or we overcorrect and disable nullable reference types in the project). That's fine,
/// but it reads like "literals can be missing" when really the missing state still has meaning.
/// Instead, we represent Lox's nil with a dedicated class.
/// </summary>
internal class Nil
{
    /// <summary>
    /// The one true Nil. Its one-ness means `nil == nil` evaluates to true by default.
    /// </summary>
    public static Nil Instance { get; } = new();

    private Nil()
    {
    }

    public static Expr.Literal Literal => new(Instance);

    public override string ToString() => "nil";
}
