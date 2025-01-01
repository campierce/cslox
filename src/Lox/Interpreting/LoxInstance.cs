namespace Lox;

internal class LoxInstance
{
    /// <summary>
    /// The class from which this instance was created.
    /// </summary>
    private readonly LoxClass _class;

    /// <summary>
    /// Maps field names to their values.
    /// </summary>
    private readonly Dictionary<string, object> _fields;

    /// <summary>
    /// Creates a LoxInstance.
    /// </summary>
    /// <param name="cls">The class from which this instance was created.</param>
    public LoxInstance(LoxClass cls)
    {
        _class = cls;
        _fields = [];
    }

    /// <summary>
    /// Looks up a property by name. Searches fields first, then methods.
    /// </summary>
    /// <param name="name">A token whose lexeme is the property name.</param>
    /// <returns>The property with the given name.</returns>
    /// <exception cref="RuntimeError">Thrown if the property does not exist.</exception>
    public virtual object Get(Token name)
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

    /// <summary>
    /// Sets a field.
    /// </summary>
    /// <param name="name">A token whose lexeme is the field name.</param>
    /// <param name="value">The value to set.</param>
    public virtual void Set(Token name, object value)
    {
        _fields[name.Lexeme] = value;
    }

    public override string ToString()
    {
        return $"{_class.Name} instance";
    }
}
