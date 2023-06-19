namespace cslox.lox;

/// <summary>
/// Non-null wrapper around null.
/// If we use C#'s `null` to represent Lox's `nil`, then _any_ literal's value
/// must be typed as as `object?`. That's fine, but it suggests our literals can
/// be absent a value, when in fact that absence is meant to represent a valid object.
/// This class exists to un-blur the lines and "shield" the literal `null` until it's needed.
/// </summary>
internal class Nil
{
    public static Nil Instance { get; } = new();

    private Nil()
    {
    }

    public readonly object? Value = null;

    public override string ToString()
    {
        return "nil";
    }
}
