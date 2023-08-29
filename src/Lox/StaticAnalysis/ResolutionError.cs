namespace Lox.StaticAnalysis;

internal class ResolutionError : Error
{
    public override string Type => "Resolution";

    public ResolutionError(Token token, string message) : base(token, message)
    {
    }
}
