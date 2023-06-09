using Lox.Scanning;

namespace Lox.IR;

// Generated code; see AstGenerator to make changes.
internal abstract class Expr
{
    public abstract T Accept<T>(Visitor<T> visitor);

    internal interface Visitor<T>
    {
        T VisitAssignExpr(Assign expr);

        T VisitBinaryExpr(Binary expr);

        T VisitCallExpr(Call expr);

        T VisitGroupingExpr(Grouping expr);

        T VisitLiteralExpr(Literal expr);

        T VisitLogicalExpr(Logical expr);

        T VisitUnaryExpr(Unary expr);

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

        public override T Accept<T>(Visitor<T> visitor)
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

        public override T Accept<T>(Visitor<T> visitor)
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

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitCallExpr(this);
        }
    }

    internal class Grouping : Expr
    {
        public Expr Expression { get; }

        public Grouping(Expr expression)
        {
            Expression = expression;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitGroupingExpr(this);
        }
    }

    internal class Literal : Expr
    {
        public Object Value { get; }

        public Literal(Object value)
        {
            Value = value;
        }

        public override T Accept<T>(Visitor<T> visitor)
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

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitLogicalExpr(this);
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

        public override T Accept<T>(Visitor<T> visitor)
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

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitVariableExpr(this);
        }
    }
}
