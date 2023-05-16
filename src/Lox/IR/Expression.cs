using cslox.lox.scanner;

namespace cslox.lox.ir;

internal abstract class Expression
{
    internal class Binary : Expression
    {
        internal Expression Left { get; }
        internal Token Operator { get; }
        internal Expression Right { get; }
        internal Binary(Expression left, Token _operator, Expression right)
        {
            Left = left;
            Operator = _operator;
            Right = right;
        }
    }
    internal class Grouping : Expression
    {
        internal Expression Expression { get; }
        internal Grouping(Expression expression)
        {
            Expression = expression;
        }
    }
    internal class Literal : Expression
    {
        internal Object Value { get; }
        internal Literal(Object value)
        {
            Value = value;
        }
    }
    internal class Unary : Expression
    {
        internal Token Operator { get; }
        internal Expression Right { get; }
        internal Unary(Token _operator, Expression right)
        {
            Operator = _operator;
            Right = right;
        }
    }
}
