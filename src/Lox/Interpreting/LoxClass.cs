namespace Lox;

internal class LoxClass : ILoxCallable
{
    private readonly Dictionary<string, LoxFunction> _methods;

    public string Name { get; }

    public LoxClass Superclass { get; }

    public int Arity
    {
        get
        {
            if (TryFindMethod("init", out LoxFunction? initializer))
            {
                return initializer!.Arity;
            }
            return 0;
        }
    }

    public LoxClass(string name, LoxClass superclass, Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        Superclass = superclass;
        _methods = methods;
    }

    public bool TryFindMethod(string name, out LoxFunction? method)
    {
        if (_methods.TryGetValue(name, out method))
        {
            return true;
        }

        if (Superclass is not null)
        {
            return Superclass.TryFindMethod(name, out method);
        }

        return false;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        LoxInstance instance = new(this);

        // if a ctor is defined...
        if (TryFindMethod("init", out LoxFunction? initializer))
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
