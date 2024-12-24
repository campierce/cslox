namespace Lox;

internal class Clock : ILoxCallable
{
    public int Arity => 0;

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
    }

    public override string ToString() => "<native fn>";
}
