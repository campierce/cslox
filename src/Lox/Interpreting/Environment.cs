namespace Lox.Interpreting;

internal class Environment
{
    #region Fields/Properties
    /// <summary>
    /// Maps variable names to their values.
    /// </summary>
    private readonly Dictionary<string, object> _values = new();

    /// <summary>
    /// The enclosing environment, if applicable.
    /// </summary>
    private readonly Environment? _enclosing;
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a new global Environment.
    /// </summary>
    public Environment()
    {
        _enclosing = null;
    }

    /// <summary>
    /// Creates a new enclosed Environment.
    /// </summary>
    /// <param name="enclosing">The enclosing Environment.</param>
    public Environment(Environment enclosing)
    {
        _enclosing = enclosing;
    }
    #endregion

    #region API
    /// <summary>
    /// Defines a new variable.
    /// </summary>
    /// <param name="name">The variable's name.</param>
    /// <param name="value">The variable's value.</param>
    public void Define(string name, object value)
    {
        _values[name] = value;
    }

    /// <summary>
    /// Assigns a value to an existing variable.
    /// </summary>
    /// <param name="name">The variable's name.</param>
    /// <param name="value">The variable's value.</param>
    /// <exception cref="RuntimeError">Thrown if the variable does not exist.</exception>
    public void Assign(Token name, object value)
    {
        // try to use this scope
        if (_values.ContainsKey(name.Lexeme))
        {
            _values[name.Lexeme] = value;
            return;
        }

        // otherwise try the enclosing scope
        if (_enclosing is not null)
        {
            _enclosing.Assign(name, value);
            return;
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    /// <summary>
    /// Assigns a value to an existing variable, at the nth ancestor.
    /// </summary>
    /// <param name="distance">The ancestor number.</param>
    /// <param name="name">The variable's name.</param>
    /// <param name="value">The variable's value.</param>
    public void AssignAt(int distance, Token name, object value)
    {
        Ancestor(distance)._values[name.Lexeme] = value;
    }

    /// <summary>
    /// Gets the value of a variable.
    /// </summary>
    /// <param name="name">The variable's name.</param>
    /// <returns>The variable's value.</returns>
    /// <exception cref="RuntimeError">Thrown if the variable does not exist.</exception>
    public object Get(Token name)
    {
        // try to use this scope
        if (_values.ContainsKey(name.Lexeme))
        {
            return _values[name.Lexeme];
        }

        // otherwise try the enclosing scope
        if (_enclosing is not null)
        {
            return _enclosing.Get(name);
        }

        throw new RuntimeError(name, $"Undefined variable '{name.Lexeme}'.");
    }

    /// <summary>
    /// Gets the value of a variable, at the nth ancestor.
    /// </summary>
    /// <param name="distance">The ancestor number.</param>
    /// <param name="name">The variable's name.</param>
    /// <returns>The variable's value.</returns>
    public object GetAt(int distance, string name)
    {
        return Ancestor(distance)._values[name];
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Finds the nth ancestor of this environment.
    /// </summary>
    /// <param name="distance">The ancestor number.</param>
    /// <returns>The nth ancestor.</returns>
    private Environment Ancestor(int distance)
    {
        Environment environment = this;
        for (int i = 0; i < distance; i++)
        {
            environment = environment._enclosing!;
        }

        return environment;
    }
    #endregion
}
