namespace Lox;

internal class RuntimeError : Error
{
    public override string Type => "Runtime";

    public RuntimeError(Token token, string message) : base(token, message)
    {
    }
}
