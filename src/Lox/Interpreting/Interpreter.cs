using System.Globalization;

namespace Lox;

internal class Interpreter : Expr.IVisitor<object>, Stmt.IVisitor<Void>
{
    #region State
    /// <summary>
    /// Maps an expression to the environment distance of the variable it references.
    /// </summary>
    private readonly Dictionary<Expr, int> _distanceOf = [];

    /// <summary>
    /// The current environment.
    /// </summary>
    private Environment _environment;

    /// <summary>
    /// The global environment.
    /// </summary>
    private readonly Environment _globals = new();
    #endregion

    #region Constructor
    /// <summary>
    /// Creates an Interpreter.
    /// </summary>
    public Interpreter()
    {
        _environment = _globals;

        // define native classes/functions
        _globals.Define("list", new LoxList());
        _globals.Define("clock", new Clock());
    }
    #endregion

    #region API
    /// <summary>
    /// Interprets a list of statements.
    /// </summary>
    /// <param name="statements">The statements to interpret.</param>
    public void Interpret(List<Stmt> statements)
    {
        try
        {
            statements.ForEach(Execute);
        }
        catch (RuntimeError error)
        {
            Lox.RuntimeError(error);
        }
    }

    /// <summary>
    /// Executes a list of statements in the given environment.
    /// </summary>
    /// <param name="statements">The statements to execute.</param>
    /// <param name="environment">The surrounding environment.</param>
    public void ExecuteBlock(List<Stmt> statements, Environment environment)
    {
        Environment previous = _environment;
        try
        {
            _environment = environment;
            statements.ForEach(Execute);
        }
        finally
        {
            _environment = previous; // discard block scope
        }
    }

    /// <summary>
    /// Stores the environment distance for a variable contained in the given expression.
    /// </summary>
    /// <param name="expr">The expression in which the variable is used.</param>
    /// <param name="distance">The environment distance of the variable.</param>
    public void Resolve(Expr expr, int distance)
    {
        _distanceOf[expr] = distance;
    }
    #endregion

    #region Expr visitor
    public object VisitAssignExpr(Expr.Assign expr)
    {
        object value = Evaluate(expr.Value);

        if (_distanceOf.TryGetValue(expr, out int distance))
        {
            _environment.AssignAt(distance, expr.Name, value);
        }
        else
        {
            _globals.Assign(expr.Name, value);
        }

        return value;
    }

    public object VisitBinaryExpr(Expr.Binary expr)
    {
        object left = Evaluate(expr.Left);
        object right = Evaluate(expr.Right);

        if (expr.Operator.Type == TokenType.Plus)
        {
            if (left is double d1 && right is double d2)
            {
                return d1 + d2;
            }
            if (left is string s1 && right is string s2)
            {
                return s1 + s2;
            }
            throw new RuntimeError(expr.Operator, "Operands must be two numbers or two strings.");
        }

        if (expr.Operator.Type == TokenType.BangEqual)
        {
            // We must use Equals because the operands are typed as object (it dispatches to the
            // runtime type). If we used == then we'd always compare references and, e.g., `1 == 1`
            // would be false.
            return !left.Equals(right);
        }
        if (expr.Operator.Type == TokenType.EqualEqual)
        {
            // And we're fine using .NET's default equality semantics:
            // - value comparison for booleans/numbers/strings
            // - referential comparison otherwise
            return left.Equals(right);
        }

        if (left is not double a || right is not double b)
        {
            throw new RuntimeError(expr.Operator, "Operands must be numbers.");
        }
        
        #pragma warning disable format
        return expr.Operator.Type switch
        {
            TokenType.Greater      => a > b,
            TokenType.GreaterEqual => a >= b,
            TokenType.Less         => a < b,
            TokenType.LessEqual    => a <= b,
            TokenType.Minus        => a - b,
            TokenType.Slash        => a / b,
            TokenType.Star         => a * b,
            _ => throw new RuntimeError(expr.Operator, "Unrecognized operator.") // unreachable
        };
        #pragma warning restore format
    }

    public object VisitCallExpr(Expr.Call expr)
    {
        object callee = Evaluate(expr.Callee);

        List<object> arguments = [];
        foreach (Expr argument in expr.Arguments)
        {
            arguments.Add(Evaluate(argument));
        }

        if (callee is ILoxCallable function)
        {
            if (arguments.Count != function.Arity)
            {
                throw new RuntimeError(
                    expr.Paren,
                    $"Expected {function.Arity} arguments but got {arguments.Count}."
                );
            }
            return function.Call(this, arguments);
        }

        throw new RuntimeError(expr.Paren, "Can only call functions and classes.");
    }

    public object VisitGetExpr(Expr.Get expr)
    {
        object obj = Evaluate(expr.Object);
        if (obj is LoxInstance instance)
        {
            return instance.Get(expr.Name);
        }

        throw new RuntimeError(expr.Name, "Only instances have properties.");
    }

    public object VisitGroupingExpr(Expr.Grouping expr)
    {
        return Evaluate(expr.Expression);
    }

    public object VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value;
    }

    public object VisitLogicalExpr(Expr.Logical expr)
    {
        object left = Evaluate(expr.Left);

        // attempt to short circuit
        if (expr.Operator.Type == TokenType.Or)
        {
            if (IsTruthy(left)) { return left; }
        }
        else
        {
            if (!IsTruthy(left)) { return left; }
        }

        return Evaluate(expr.Right);
    }

    public object VisitSetExpr(Expr.Set expr)
    {
        object obj = Evaluate(expr.Object);

        if (obj is LoxInstance instance)
        {
            object value = Evaluate(expr.Value);
            instance.Set(expr.Name, value);
            return value;
        }

        throw new RuntimeError(expr.Name, "Only instances have fields.");
    }

    public object VisitSuperExpr(Expr.Super expr)
    {
        // if we're here then we've visited the subclass declaration without issue
        // so the superclass in question must be defined, some distance away
        int distance = _distanceOf[expr];
        var superclass = (LoxClass)_environment.GetAt(distance, "super");

        if (superclass.TryFindMethod(expr.Method.Lexeme, out LoxFunction? method))
        {
            // we always splice in a closure that binds `this` when we access a method
            // we're in a method right now, so that must have already happened
            var instance = (LoxInstance)_environment.GetAt(distance - 1, "this");

            // and we're accessing a new method, so we must apply the same rule
            return method!.Bind(instance);
        }

        throw new RuntimeError(expr.Method, $"Undefined property '{expr.Method.Lexeme}'.");
    }

    public object VisitThisExpr(Expr.This expr)
    {
        return LookUpVariable(expr.Keyword, expr);
    }

    public object VisitUnaryExpr(Expr.Unary expr)
    {
        object right = Evaluate(expr.Right);

        switch (expr.Operator.Type)
        {
            case TokenType.Bang:
                return !IsTruthy(right);
            case TokenType.Minus:
                if (right is not double num)
                {
                    throw new RuntimeError(expr.Operator, "Operand must be a number.");
                }
                return -num;
        }

        throw new RuntimeError(expr.Operator, "Unrecognized operator."); // unreachable
    }

    public object VisitVariableExpr(Expr.Variable expr)
    {
        return LookUpVariable(expr.Name, expr);
    }
    #endregion

    #region Stmt visitor
    public Void VisitBlockStmt(Stmt.Block stmt)
    {
        ExecuteBlock(stmt.Statements, new Environment(_environment));
        return default;
    }

    public Void VisitClassStmt(Stmt.Class stmt)
    {
        // if there appears to be a superclass...
        object? superclass = null;
        if (stmt.Superclass is not null)
        {
            // make sure it's actually a class (must have been defined earlier)
            superclass = Evaluate(stmt.Superclass);
            if (superclass is not LoxClass)
            {
                throw new RuntimeError(stmt.Superclass.Name, "Superclass must be a class.");
            }

            // and then bind it to `super` (methods will form a closure over this)
            _environment = new Environment(_environment);
            _environment.Define("super", superclass);
        }

        Dictionary<string, LoxFunction> methods = [];
        foreach (Stmt.Function method in stmt.Methods)
        {
            bool isInitializer = method.Name.Lexeme == "init";
            LoxFunction function = new(method, _environment, isInitializer);
            // notice: no error on duplicate method names
            methods[method.Name.Lexeme] = function;
        }

        if (superclass is not null)
        {
            _environment = _environment.Enclosing!; // discard `super` scope
        }

        LoxClass cls = new(stmt.Name.Lexeme, (LoxClass?)superclass, methods);
        _environment.Define(stmt.Name.Lexeme, cls);
        return default;
    }

    public Void VisitExpressionStmt(Stmt.Expression stmt)
    {
        Evaluate(stmt.Expr);
        return default;
    }

    public Void VisitFunctionStmt(Stmt.Function stmt)
    {
        // methods are handled in VisitClassStmt, so we're assured this is not a ctor
        LoxFunction function = new(stmt, _environment, isInitializer: false);
        _environment.Define(stmt.Name.Lexeme, function);
        return default;
    }

    public Void VisitIfStmt(Stmt.If stmt)
    {
        if (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.ThenBranch);
        }
        else if (stmt.ElseBranch is not null)
        {
            Execute(stmt.ElseBranch);
        }
        return default;
    }

    public Void VisitPrintStmt(Stmt.Print stmt)
    {
        object value = Evaluate(stmt.Expr);
        string? str = value.ToString();
        if (value is bool)
        {
            // .NET wants to capitalize this, but Lox does not
            str = str?.ToLower(CultureInfo.InvariantCulture);
        }
        Console.WriteLine(str);
        return default;
    }

    public Void VisitReturnStmt(Stmt.Return stmt)
    {
        object value;
        if (stmt.Value is null)
        {
            value = Nil.Instance;
        }
        else
        {
            value = Evaluate(stmt.Value);
        }
        throw new Return(value);
    }

    public Void VisitVarStmt(Stmt.Var stmt)
    {
        object value;
        if (stmt.Initializer is null)
        {
            value = Nil.Instance;
        }
        else
        {
            value = Evaluate(stmt.Initializer);
        }
        _environment.Define(stmt.Name.Lexeme, value);
        return default;
    }

    public Void VisitWhileStmt(Stmt.While stmt)
    {
        while (IsTruthy(Evaluate(stmt.Condition)))
        {
            Execute(stmt.Body);
        }
        return default;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Evaluates an expression.
    /// </summary>
    /// <param name="expr">The expression to evaluate.</param>
    /// <returns>The result of evaluating the expression.</returns>
    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    /// <summary>
    /// Executes a statement.
    /// </summary>
    /// <param name="stmt">The statement to execute.</param>
    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    /// <summary>
    /// Looks up a variable by name. Tries to use the environment distance that was resolved
    /// earlier, otherwise searches the global environment.
    /// </summary>
    /// <param name="name">A token whose lexeme is the variable name.</param>
    /// <param name="expr">The expression in which the variable is used.</param>
    /// <returns>The variable's value.</returns>
    private object LookUpVariable(Token name, Expr expr)
    {
        if (_distanceOf.TryGetValue(expr, out int distance))
        {
            return _environment.GetAt(distance, name.Lexeme);
        }
        else
        {
            return _globals.Get(name);
        }
    }

    /// <summary>
    /// Decides whether a given object is truthy. In Lox, <c>nil</c> and <c>false</c> are falsy,
    /// while everything else is truthy (like Ruby).
    /// </summary>
    /// <param name="obj">The object to test for truthiness.</param>
    /// <returns>Whether the object is truthy.</returns>
    private static bool IsTruthy(object obj)
    {
        #pragma warning disable format
        return obj switch
        {
            Nil    => false,
            bool b => b,
            _      => true
        };
        #pragma warning disable format
    }
    #endregion
}
