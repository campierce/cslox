using System.Collections;
using System.Text;

namespace Lox;

internal class AstPrinter : Expr.IVisitor<string>, Stmt.IVisitor<string>
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
        return Parenthesize("=", expr.Target.Name.Lexeme, expr.Value);
    }

    public string VisitBinaryExpr(Expr.Binary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
    }

    public string VisitCallExpr(Expr.Call expr)
    {
        return Parenthesize("call", expr.Callee, expr.Arguments);
    }

    public string VisitGetExpr(Expr.Get expr)
    {
        return Parenthesize(".", expr.Object, expr.Name.Lexeme);
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

    public string VisitSetExpr(Expr.Set expr)
    {
        return Parenthesize("=", expr.Object, expr.Name.Lexeme, expr.Value);
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
        return Parenthesize("block", stmt.Statements);
    }

    public string VisitClassStmt(Stmt.Class stmt)
    {
        return Parenthesize("class", stmt.Name.Lexeme, stmt.Methods);
    }

    public string VisitExpressionStmt(Stmt.Expression stmt)
    {
        return Parenthesize(";", stmt.InnerExpression);
    }

    public string VisitFunctionStmt(Stmt.Function stmt)
    {
        return Parenthesize("fun", stmt.Name.Lexeme, "(", stmt.Parameters, ")", stmt.Body);
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

    public string VisitReturnStmt(Stmt.Return stmt)
    {
        return Parenthesize("return", stmt.Value);
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
    private string Parenthesize(string label, params object[] parts)
    {
        StringBuilder sb = new();

        sb.Append($"({label}");
        Transform(sb, parts);
        sb.Append(')');

        return sb.ToString();
    }

    private void Transform(StringBuilder sb, params object[] parts)
    {
        foreach (object part in parts)
        {
            if (part is IList list)
            {
                if (list.Count > 0)
                {
                    Transform(sb, list.Cast<object>().ToArray());
                }
                continue;
            }

            sb.Append(' ');

            switch (part)
            {
                case Token token:
                    sb.Append(token.Lexeme);
                    break;
                case Expr expr:
                    sb.Append(Print(expr));
                    break;
                case Stmt stmt:
                    sb.Append(Print(stmt));
                    break;
                default:
                    sb.Append(part);
                    break;
            }
        }
    }
    #endregion
}
