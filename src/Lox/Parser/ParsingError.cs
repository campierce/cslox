namespace Lox.Parsing;

internal class ParsingError : Error
{
    public override string Name => "Parsing";

    public ParsingError(Token token, string message) : base(token, message)
    {
    }
}
