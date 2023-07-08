using Lox.Scanning;

namespace Lox.IR;

internal abstract class Stmt
{
    public abstract T Accept<T>(Visitor<T> visitor);

    internal interface Visitor<T>
    {
        T VisitBlockStmt(Block stmt);

        T VisitExpressionStmt(Expression stmt);

        T VisitPrintStmt(Print stmt);

        T VisitVarStmt(Var stmt);
    }

    internal class Block : Stmt
    {
        public List<Stmt> Statements { get; }

        public Block(List<Stmt> statements)
        {
            Statements = statements;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitBlockStmt(this);
        }
    }

    internal class Expression : Stmt
    {
        public Expr InnerExpression { get; }

        public Expression(Expr innerExpression)
        {
            InnerExpression = innerExpression;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitExpressionStmt(this);
        }
    }

    internal class Print : Stmt
    {
        public Expr Content { get; }

        public Print(Expr content)
        {
            Content = content;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitPrintStmt(this);
        }
    }

    internal class Var : Stmt
    {
        public Token Name { get; }
        public Expr Initializer { get; }

        public Var(Token name, Expr initializer)
        {
            Name = name;
            Initializer = initializer;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitVarStmt(this);
        }
    }
}
