namespace Lox;

internal class LoxInstance
{
    private readonly LoxClass _class;

    private readonly Dictionary<string, object> _fields;

    public LoxInstance(LoxClass cls)
    {
        _class = cls;
        _fields = [];
    }

    public object Get(Token name)
    {
        if (_fields.TryGetValue(name.Lexeme, out object? field))
        {
            return field;
        }

        if (_class.TryFindMethod(name.Lexeme, out LoxFunction? method))
        {
            return method!.Bind(this);
        }

        throw new RuntimeError(name, $"Undefined property '{name.Lexeme}'.");
    }

    public void Set(Token name, object value)
    {
        _fields[name.Lexeme] = value;
    }

    public override string ToString()
    {
        return $"{_class.Name} instance";
    }
}
