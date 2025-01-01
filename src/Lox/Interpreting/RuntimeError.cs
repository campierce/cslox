namespace Lox;

/// <summary>
/// Exception that's thrown during the interpreting phase.
/// </summary>
internal class RuntimeError : Exception
{
    /// <summary>
    /// The token where the error occurred.
    /// </summary>
    public Token Token { get; }

    /// <summary>
    /// Creates a RuntimeError.
    /// </summary>
    /// <param name="token">The token where the error occurred.</param>
    /// <param name="message">The error message.</param>
    public RuntimeError(Token token, string message) : base(message)
    {
        Token = token;
    }
}
