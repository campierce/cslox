using cslox.lox.ir;
using static cslox.lox.ir.Expr;

namespace lox.cslox.interpeter;

internal class Interpeter : Expr.Visitor<object>
{
    #region Implementations
    public object VisitBinaryExpr(Binary expr)
    {
        throw new NotImplementedException();
    }
            
    public object VisitGroupingExpr(Grouping expr)
    {
        throw new NotImplementedException();
    }
            
    public object VisitLiteralExpr(Literal expr)
    {
        return expr.Value;
    }
            
    public object VisitUnaryExpr(Unary expr)
    {
        throw new NotImplementedException();
    }
    #endregion
}
