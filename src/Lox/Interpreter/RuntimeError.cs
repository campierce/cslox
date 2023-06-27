using Lox.Scanning;

namespace Lox.Interpreting;

internal class RuntimeError : Exception
{
    public readonly Token Token;

    public RuntimeError(Token token, string message) : base(message)
    {
        Token = token;
    }
}
