using Lox.Scanning;

namespace Lox.Interpreting;

internal class Environment
{
    #region Fields
    private readonly Dictionary<string, object> _values = new();
    #endregion

    #region API
    public void Define(string name, object value)
    {
        _values[name] = value;
    }

    public void Assign(Token name, object value)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    public object Get(Token name)
    {
        if (_values.ContainsKey(name.Lexeme))
        {
            return _values[name.Lexeme];
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }
    #endregion
}
