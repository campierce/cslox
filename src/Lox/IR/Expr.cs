using cslox.lox.scanner;

namespace cslox.lox.ir;

internal abstract class Expr
{
    public abstract R Accept<R>(Visitor<R> visitor);
    
    internal interface Visitor<R>
    {
        R VisitBinaryExpr(Binary expr);
            
        R VisitGroupingExpr(Grouping expr);
            
        R VisitLiteralExpr(Literal expr);
            
        R VisitUnaryExpr(Unary expr);
            
    }
    
    internal class Binary : Expr
    {
        public Expr Left { get; }
        public Token Operator { get; }
        public Expr Right { get; }
        
        public Binary(Expr left, Token _operator, Expr right)
        {
            Left = left;
            Operator = _operator;
            Right = right;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitBinaryExpr(this);
        }
    }
    
    internal class Grouping : Expr
    {
        public Expr Expression { get; }
        
        public Grouping(Expr expression)
        {
            Expression = expression;
        }

        public override R Accept<R>(Visitor<R> visitor)
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

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitLiteralExpr(this);
        }
    }
    
    internal class Unary : Expr
    {
        public Token Operator { get; }
        public Expr Right { get; }
        
        public Unary(Token _operator, Expr right)
        {
            Operator = _operator;
            Right = right;
        }

        public override R Accept<R>(Visitor<R> visitor)
        {
            return visitor.VisitUnaryExpr(this);
        }
    }
}
