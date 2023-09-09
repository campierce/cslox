
namespace Lox.Interpreting;

internal class LoxClass : ICallable
{
    public string Name { get; }

    public int Arity => 0;

    public LoxClass(string name)
    {
        Name = name;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new(this);
        return instance;
    }

    public override string ToString()
    {
        return Name;
    }
}
