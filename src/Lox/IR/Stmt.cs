using Lox.Scanning;

namespace Lox.IR;

// Generated code; see AstGenerator to make changes.
internal abstract class Stmt
{
    public abstract T Accept<T>(Visitor<T> visitor);

    internal interface Visitor<T>
    {
        T VisitBlockStmt(Block stmt);

        T VisitExpressionStmt(Expression stmt);

        T VisitIfStmt(If stmt);

        T VisitPrintStmt(Print stmt);

        T VisitVarStmt(Var stmt);

        T VisitWhileStmt(While stmt);
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

    internal class If : Stmt
    {
        public Expr Condition { get; }
        public Stmt ThenBranch { get; }
        public Stmt? ElseBranch { get; }

        public If(Expr condition, Stmt thenBranch, Stmt? elseBranch)
        {
            Condition = condition;
            ThenBranch = thenBranch;
            ElseBranch = elseBranch;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitIfStmt(this);
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

    internal class While : Stmt
    {
        public Expr Condition { get; }
        public Stmt Body { get; }

        public While(Expr condition, Stmt body)
        {
            Condition = condition;
            Body = body;
        }

        public override T Accept<T>(Visitor<T> visitor)
        {
            return visitor.VisitWhileStmt(this);
        }
    }
}
