using System.Text;
using Lox.IR;
using Lox.Scanning;

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
    public string VisitAssignExpr(Expr.Assign expr)
    {
        return Parenthesize("=", expr.Name.Lexeme, expr.Value);
    }

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

    public string VisitLogicalExpr(Expr.Logical expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
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
    public string VisitBlockStmt(Stmt.Block stmt)
    {
        return Parenthesize("block", stmt.Statements.ToArray());
    }

    public string VisitExpressionStmt(Stmt.Expression stmt)
    {
        return Parenthesize(";", stmt.InnerExpression);
    }

    public string VisitIfStmt(Stmt.If stmt)
    {
        if (stmt.ElseBranch is null)
        {
            return Parenthesize("if", stmt.Condition, stmt.ThenBranch);
        }
        else
        {
            return Parenthesize("if-else", stmt.Condition, stmt.ThenBranch, stmt.ElseBranch);
        }
    }

    public string VisitPrintStmt(Stmt.Print stmt)
    {
        return Parenthesize("print", stmt.Content);
    }

    public string VisitVarStmt(Stmt.Var stmt)
    {
        return Parenthesize("var", stmt.Name, "=", stmt.Initializer);
    }

    public string VisitWhileStmt(Stmt.While stmt)
    {
        return Parenthesize("while", stmt.Condition, stmt.Body);
    }
    #endregion

    #region Helpers
    private string Parenthesize(string label, params object[] objects)
    {
        StringBuilder sb = new();

        sb.Append($"({label}");
        foreach (object obj in objects)
        {
            sb.Append(" ");
            switch (obj)
            {
                case Expr expr:
                    sb.Append(Print(expr));
                    break;
                case Stmt stmt:
                    sb.Append(Print(stmt));
                    break;
                case Token token:
                    sb.Append(token.Lexeme);
                    break;
                default:
                    sb.Append(obj);
                    break;
            }
        }
        sb.Append(")");

        return sb.ToString();
    }
    #endregion
}
