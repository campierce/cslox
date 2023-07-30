using Lox.IR;

namespace Lox.Interpreting;

internal class CallableFunction : ICallable
{
    private readonly Stmt.Function _declaration;
    
    public int Arity => _declaration.Params.Count;

    public CallableFunction(Stmt.Function declaration)
    {
        _declaration = declaration;
    }

    public object Call(Interpreter interpreter, List<object> arguments)
    {
        Environment environment = new Environment(interpreter.Globals);
        for (int i = 0; i < _declaration.Params.Count; i++)
        {
            // bind the parameter to its argument
            environment.Define(_declaration.Params[i].Lexeme, arguments[i]);
        }
        interpreter.ExecuteBlock(_declaration.Body, environment);
        return Nil.Literal;
    }

    public override string ToString() => $"<fn {_declaration.Name.Lexeme}>";
}
