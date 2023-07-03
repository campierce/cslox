namespace Lox.IR;

/// <summary>
/// Box around null.
/// If we use C#'s null to represent Lox's nil, then any literal must be typed
/// as an object?. That's fine, but it reads like "literals can be missing" when
/// really the missing state still has meaning. This class exists to unblur the
/// lines and shield null until it's needed.
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
