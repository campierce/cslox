using System.Text;
using cslox.lox.ir;

namespace cslox.lox;

internal class AstPrinter : Expr.Visitor<string>
{
    #region API
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }
    #endregion

    #region Visitor
    public string VisitBinaryExpr(Expr.Binary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
    }

    public string VisitGroupingExpr(Expr.Grouping expr)
    {
        return Parenthesize("group", expr.Expression);
    }

    public string VisitLiteralExpr(Expr.Literal expr)
    {
        return expr.Value.ToString()!;
    }

    public string VisitUnaryExpr(Expr.Unary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Right);
    }
    #endregion

    #region Helpers
    private string Parenthesize(string name, params Expr[] exprs)
    {
        StringBuilder builder = new();

        builder.Append($"({name}");
        foreach (Expr expr in exprs)
        {
            builder.Append(" ");
            // which method to call? ask the expression...
            builder.Append(expr.Accept(this));
        }
        builder.Append(")");

        return builder.ToString();
    }
    #endregion
}
