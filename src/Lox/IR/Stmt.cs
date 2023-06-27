namespace Lox.IR;

internal abstract class Stmt
{
    public abstract T Accept<T>(Visitor<T> visitor);

    internal interface Visitor<T>
    {
        T VisitExpressionStmt(Expression stmt);

        T VisitPrintStmt(Print stmt);
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
}
