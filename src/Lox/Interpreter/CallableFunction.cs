using Lox.IR;

namespace Lox.Interpreting;

internal class CallableFunction : ICallable
{
    private readonly Stmt.Function _declaration;

    private readonly Environment _closure;

    public int Arity => _declaration.Params.Count;

    public CallableFunction(Stmt.Function declaration, Environment closure)
    {
        _declaration = declaration;
        _closure = closure;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        // create block scope
        Environment environment = new(_closure);

        // bind parameters to their arguments
        for (int i = 0; i < _declaration.Params.Count; i++)
        {
            environment.Define(_declaration.Params[i].Lexeme, arguments[i]);
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
