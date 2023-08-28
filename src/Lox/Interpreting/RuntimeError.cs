namespace Lox.Interpreting;

internal class RuntimeError : Error
{
    public override string Name => "Runtime";

    public RuntimeError(Token token, string message) : base(token, message)
    {
    }
}
