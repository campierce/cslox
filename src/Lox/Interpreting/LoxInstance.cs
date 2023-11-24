namespace Lox.Interpreting;

internal class LoxInstance
{
    private readonly LoxClass _class;

    private readonly Dictionary<string, object> _fields;

    public LoxInstance(LoxClass @class)
    {
        _class = @class;
        _fields = new();
    }

    public object Get(Token name)
    {
        if (_fields.ContainsKey(name.Lexeme))
        {
            return _fields[name.Lexeme];
        }

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object value)
    {
        _fields[name.Lexeme] = value;
    }

    public override string ToString()
    {
        return _class.Name + " instance";
    }
}
