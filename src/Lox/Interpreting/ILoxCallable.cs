namespace Lox;

internal interface ILoxCallable
{
    /// <summary>
    /// The number of expected arguments.
    /// </summary>
    int Arity { get; }

    /// <summary>
    /// Does whatever it means to call this object. For classes, that means creating an instance.
    /// For user-defined functions, that means executing the function body in the interpreter. For
    /// native callables, that means running some C#.
    /// </summary>
    /// <param name="interpreter">The interpreter in which to execute AST nodes.</param>
    /// <param name="arguments">The arguments passed in the call.</param>
    /// <returns>An object.</returns>
    object Call(Interpreter interpreter, List<object> arguments);
}
