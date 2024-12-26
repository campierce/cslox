namespace Lox;

/// <summary>
/// Runtime representation of a function (wraps the AST node and adds things only the interpreter
/// cares about).
/// </summary>
internal class LoxFunction : ILoxCallable
{
    private readonly Stmt.Function _declaration;

    private readonly Environment _closure;

    public int Arity => _declaration.Params.Count;

    public LoxFunction(Stmt.Function declaration, Environment closure)
    {
        _declaration = declaration;
        _closure = closure;
    }

    /// <summary>
    /// Copies this function and splices a new environment into its closure that binds `this` to the
    /// given instance.
    /// </summary>
    /// <param name="instance">The instance to which `this` refers.</param>
    /// <returns>A copy of this function whose closure binds `this`.</returns>
    public LoxFunction Bind(LoxInstance instance)
    {
        var environment = new Environment(_closure);
        environment.Define("this", instance);
        return new LoxFunction(_declaration, environment);
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
            return returnValue.Value;
        }

        // if there was no explicit return, use nil
        return Nil.Instance;
    }

    public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
}
