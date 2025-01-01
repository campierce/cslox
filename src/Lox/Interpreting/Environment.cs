namespace Lox;

internal class Environment
{
    #region State
    /// <summary>
    /// Maps variable names to their values.
    /// </summary>
    private readonly Dictionary<string, object> _valueOf = [];

    /// <summary>
    /// The enclosing environment; only null for the global environment.
    /// </summary>
    public Environment? Enclosing { get; }
    #endregion

    #region Constructors
    /// <summary>
    /// Creates a global Environment.
    /// </summary>
    public Environment()
    {
        Enclosing = null;
    }

    /// <summary>
    /// Creates an enclosed Environment.
    /// </summary>
    /// <param name="enclosing">The enclosing environment.</param>
    public Environment(Environment enclosing)
    {
        Enclosing = enclosing;
    }
    #endregion

    #region API
    /// <summary>
    /// Defines a new variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value to assign.</param>
    public void Define(string name, object value)
    {
        _valueOf[name] = value;
    }

    /// <summary>
    /// Assigns a new value to an existing variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value to assign.</param>
    /// <exception cref="RuntimeError">Thrown if the variable does not exist.</exception>
    public void Assign(Token name, object value)
    {
        // try to use this scope
        if (_valueOf.ContainsKey(name.Lexeme))
        {
            _valueOf[name.Lexeme] = value;
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
    /// Assigns a new value to an existing variable, at the nth ancestor.
    /// </summary>
    /// <param name="distance">The ancestor number.</param>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value to assign.</param>
    public void AssignAt(int distance, Token name, object value)
    {
        Ancestor(distance)._valueOf[name.Lexeme] = value;
    }

    /// <summary>
    /// Gets the value of a variable.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The variable's value.</returns>
    /// <exception cref="RuntimeError">Thrown if the variable does not exist.</exception>
    public object Get(Token name)
    {
        // try to use this scope
        if (_valueOf.TryGetValue(name.Lexeme, out object? value))
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
    /// <param name="name">The variable name.</param>
    /// <returns>The variable's value.</returns>
    public object GetAt(int distance, string name)
    {
        return Ancestor(distance)._valueOf[name];
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
            // safe to assume enclosing is non-null b/c the resolver found it earlier
            environment = environment.Enclosing!;
        }

        return environment;
    }
    #endregion
}
