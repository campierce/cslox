namespace Lox;

internal class LoxClass : ILoxCallable
{
    /// <summary>
    /// Maps method names to their runtime representations.
    /// </summary>
    private readonly Dictionary<string, LoxFunction> _methods;

    /// <summary>
    /// The class name.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The superclass.
    /// </summary>
    public LoxClass? Superclass { get; }

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

    /// <summary>
    /// Creates a LoxClass.
    /// </summary>
    /// <param name="name">The class name.</param>
    /// <param name="superclass">The superclass.</param>
    /// <param name="methods">Maps method names to their runtime representations.</param>
    public LoxClass(string name, LoxClass? superclass, Dictionary<string, LoxFunction> methods)
    {
        Name = name;
        Superclass = superclass;
        _methods = methods;
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

    /// <summary>
    /// Tries to find the method with the given name. Looks here first, and then in the superclass.
    /// </summary>
    /// <param name="name">The method name.</param>
    /// <param name="method">The method with the given name, if found.</param>
    /// <returns>Whether the method was found.</returns>
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

    public override string ToString()
    {
        return $"{Name} class";
    }
}
