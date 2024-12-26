namespace Lox;

internal class LoxClass : ILoxCallable
{
    private readonly Dictionary<string, LoxFunction> _methods;

    public string Name { get; }

    public int Arity
    {
        get
        {
            if (TryGetMethod("init", out LoxFunction? initializer))
            {
                return initializer!.Arity;
            }
            return 0;
        }
    }

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

        // if a ctor is defined...
        if (TryGetMethod("init", out LoxFunction? initializer))
        {
            // give it access to `this`
            LoxFunction ctor = initializer!.Bind(instance);
            // and call it
            ctor.Call(interpreter, arguments);
        }

        return instance;
    }

    public override string ToString()
    {
        return $"{Name} class";
    }
}
