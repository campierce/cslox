namespace Lox.Interpreting;

internal class LoxClass : ICallable
{
    private readonly Dictionary<string, LoxFunction> _methods;

    public string Name { get; }

    public int Arity => 0;

    public LoxClass(string name, Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        _methods = methods;
    }

    public bool TryGetMethod(string name, out LoxFunction? method)
    {
        return _methods.TryGetValue(name, out method);
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
