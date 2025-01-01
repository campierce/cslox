namespace Lox;

/// <summary>
/// Runtime representation of a function (wraps the AST node and adds things only the interpreter
/// cares about).
/// </summary>
internal class LoxFunction : ILoxCallable
{
    /// <summary>
    /// The parser representation of this function.
    /// </summary>
    private readonly Stmt.Function _declaration;

    /// <summary>
    /// The enclosing environment, at the time this function was _declared_ (by the time it's
    /// _called_ that environment may be long since gone, unless we hold onto it here).
    /// </summary>
    private readonly Environment _closure;

    /// <summary>
    /// Whether this is a class constructor (affects how we handle return values).
    /// </summary>
    private readonly bool _isInitializer;

    public int Arity => _declaration.Params.Count;

    /// <summary>
    /// Creates a LoxFunction.
    /// </summary>
    /// <param name="declaration">The parser representation of this function.</param>
    /// <param name="closure">The enclosing environment.</param>
    /// <param name="isInitializer">Whether this is a class constructor.</param>
    public LoxFunction(Stmt.Function declaration, Environment closure, bool isInitializer)
    {
        _declaration = declaration;
        _closure = closure;
        _isInitializer = isInitializer;
    }

    /// <summary>
    /// Copies this function and splices a new environment into its closure that binds <c>this</c>
    /// to the given instance.
    /// </summary>
    /// <param name="instance">The instance to which <c>this</c> refers.</param>
    /// <returns>A copy of this function whose closure binds <c>this</c>.</returns>
    public LoxFunction Bind(LoxInstance instance)
    {
        var newClosure = new Environment(_closure);
        newClosure.Define("this", instance);
        return new LoxFunction(_declaration, newClosure, _isInitializer);
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        // create block scope
        Environment environment = new(_closure);

        // bind parameters to their arguments
        foreach ((int i, Token param) in _declaration.Params.Enumerate())
        {
            environment.Define(param.Lexeme, arguments[i]);
        }

        // execute the function body
        try
        {
            interpreter.ExecuteBlock(_declaration.Body, environment);
        }
        catch (Return returnValue)
        {
            if (_isInitializer)
            {
                // if we're here, the resolver verified this had no explicit return value
                return _closure.GetAt(0, "this");
            }
            return returnValue.Value;
        }

        // ctor always returns `this`
        if (_isInitializer)
        {
            return _closure.GetAt(0, "this");
        }

        // if there was no explicit return, use nil
        return Nil.Instance;
    }

    public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
}
