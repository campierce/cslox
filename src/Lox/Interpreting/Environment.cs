namespace Lox;

internal class Environment
{
    #region State
    /// <summary>
    /// Maps variable names to their values.
    /// </summary>
    private readonly Dictionary<string, object> _values;

    /// <summary>
    /// The enclosing environment, if applicable.
    /// </summary>
    public Environment? Enclosing { get; }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a global Environment.
    /// </summary>
    public Environment()
    {
        _values = [];
        Enclosing = null;
    }

    /// <summary>
    /// Creates an enclosed Environment.
    /// </summary>
    /// <param name="enclosing">The enclosing environment.</param>
    public Environment(Environment enclosing)
    {
        _values = [];
        Enclosing = enclosing;
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
        if (Enclosing is not null)
        {
            Enclosing.Assign(name, value);
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
        if (_values.TryGetValue(name.Lexeme, out object? value))
        {
            return value;
        }

        // otherwise try the enclosing scope
        if (Enclosing is not null)
        {
            return Enclosing.Get(name);
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
            // safe to assume the enclosing env is non-null b/c the resolver found it earlier
            environment = environment.Enclosing!;
        }

        return environment;
    }
    #endregion
}
