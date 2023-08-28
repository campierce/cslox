namespace Lox.StaticAnalysis;

internal class ResolutionError : Error
{
    public override string Name => "Resolution";

    public ResolutionError(Token token, string message) : base(token, message)
    {
    }
}
