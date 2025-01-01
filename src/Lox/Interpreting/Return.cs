namespace Lox;

/// <summary>
/// Exception that's thrown when the interpreter visits a Stmt.Return node, to unwind the .NET call
/// stack back to the function's invocation.
/// </summary>
internal class Return : Exception
{
    /// <summary>
    /// The return value.
    /// </summary>
    public object Value { get; }

    /// <summary>
    /// Creates a Return.
    /// </summary>
    /// <param name="value">The return value.</param>
    public Return(object value) : base(null, null)
    {
        Value = value;
    }
}
