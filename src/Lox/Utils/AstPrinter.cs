using System.Text;
using Lox.IR;

namespace Lox;

internal class AstPrinter : Expr.Visitor<string>, Stmt.Visitor<string>
{
    #region API
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }

    public string Print(Stmt stmt)
    {
        return stmt.Accept(this);
    }
    #endregion

    #region Expr visitor
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
        return expr.Value.ToString() ?? string.Empty;
    }

    public string VisitUnaryExpr(Expr.Unary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Right);
    }

    public string VisitVariableExpr(Expr.Variable expr)
    {
        return expr.Name.Lexeme;
    }
    #endregion

    #region Stmt visitor
    public string VisitExpressionStmt(Stmt.Expression stmt)
    {
        return Parenthesize(";", stmt.InnerExpression);
    }

    public string VisitPrintStmt(Stmt.Print stmt)
    {
        return Parenthesize("print", stmt.Content);
    }

    public string VisitVarStmt(Stmt.Var stmt)
    {
        throw new NotImplementedException();
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
