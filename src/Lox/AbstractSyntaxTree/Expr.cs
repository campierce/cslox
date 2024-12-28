namespace Lox;

// Generated code; see AstGenerator to make changes.
internal abstract class Expr
{
    public abstract T Accept<T>(IVisitor<T> visitor);

    internal interface IVisitor<T>
    {
        /// <summary>
        /// Visits the given Assign.
        /// </summary>
        /// <param name="expr">The Assign to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitAssignExpr(Assign expr);

        /// <summary>
        /// Visits the given Binary.
        /// </summary>
        /// <param name="expr">The Binary to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitBinaryExpr(Binary expr);

        /// <summary>
        /// Visits the given Call.
        /// </summary>
        /// <param name="expr">The Call to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitCallExpr(Call expr);

        /// <summary>
        /// Visits the given Get.
        /// </summary>
        /// <param name="expr">The Get to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitGetExpr(Get expr);

        /// <summary>
        /// Visits the given Grouping.
        /// </summary>
        /// <param name="expr">The Grouping to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitGroupingExpr(Grouping expr);

        /// <summary>
        /// Visits the given Literal.
        /// </summary>
        /// <param name="expr">The Literal to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitLiteralExpr(Literal expr);

        /// <summary>
        /// Visits the given Logical.
        /// </summary>
        /// <param name="expr">The Logical to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitLogicalExpr(Logical expr);

        /// <summary>
        /// Visits the given Set.
        /// </summary>
        /// <param name="expr">The Set to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitSetExpr(Set expr);

        /// <summary>
        /// Visits the given Super.
        /// </summary>
        /// <param name="expr">The Super to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitSuperExpr(Super expr);

        /// <summary>
        /// Visits the given This.
        /// </summary>
        /// <param name="expr">The This to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitThisExpr(This expr);

        /// <summary>
        /// Visits the given Unary.
        /// </summary>
        /// <param name="expr">The Unary to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitUnaryExpr(Unary expr);

        /// <summary>
        /// Visits the given Variable.
        /// </summary>
        /// <param name="expr">The Variable to visit.</param>
        /// <returns>A value of type <typeparamref name="T"/>.</returns>
        T VisitVariableExpr(Variable expr);
    }

    internal class Assign : Expr
    {
        public Token Name { get; }
        public Expr Value { get; }

        public Assign(Token name, Expr value)
        {
            Name = name;
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitAssignExpr(this);
        }
    }

    internal class Binary : Expr
    {
        public Expr Left { get; }
        public Token Operator { get; }
        public Expr Right { get; }

        public Binary(Expr left, Token @operator, Expr right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitBinaryExpr(this);
        }
    }

    internal class Call : Expr
    {
        public Expr Callee { get; }
        public Token Paren { get; }
        public List<Expr> Arguments { get; }

        public Call(Expr callee, Token paren, List<Expr> arguments)
        {
            Callee = callee;
            Paren = paren;
            Arguments = arguments;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitCallExpr(this);
        }
    }

    internal class Get : Expr
    {
        public Expr Object { get; }
        public Token Name { get; }

        public Get(Expr @object, Token name)
        {
            Object = @object;
            Name = name;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitGetExpr(this);
        }
    }

    internal class Grouping : Expr
    {
        public Expr Expression { get; }

        public Grouping(Expr expression)
        {
            Expression = expression;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitGroupingExpr(this);
        }
    }

    internal class Literal : Expr
    {
        public object Value { get; }

        public Literal(object value)
        {
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLiteralExpr(this);
        }
    }

    internal class Logical : Expr
    {
        public Expr Left { get; }
        public Token Operator { get; }
        public Expr Right { get; }

        public Logical(Expr left, Token @operator, Expr right)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitLogicalExpr(this);
        }
    }

    internal class Set : Expr
    {
        public Expr Object { get; }
        public Token Name { get; }
        public Expr Value { get; }

        public Set(Expr @object, Token name, Expr value)
        {
            Object = @object;
            Name = name;
            Value = value;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSetExpr(this);
        }
    }

    internal class Super : Expr
    {
        public Token Keyword { get; }
        public Token Method { get; }

        public Super(Token keyword, Token method)
        {
            Keyword = keyword;
            Method = method;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitSuperExpr(this);
        }
    }

    internal class This : Expr
    {
        public Token Keyword { get; }

        public This(Token keyword)
        {
            Keyword = keyword;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitThisExpr(this);
        }
    }

    internal class Unary : Expr
    {
        public Token Operator { get; }
        public Expr Right { get; }

        public Unary(Token @operator, Expr right)
        {
            Operator = @operator;
            Right = right;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitUnaryExpr(this);
        }
    }

    internal class Variable : Expr
    {
        public Token Name { get; }

        public Variable(Token name)
        {
            Name = name;
        }

        public override T Accept<T>(IVisitor<T> visitor)
        {
            return visitor.VisitVariableExpr(this);
        }
    }
}
