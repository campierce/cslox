using System.Collections;
using System.Text;

namespace Lox;

internal class AstPrinter : Expr.IVisitor<string>, Stmt.IVisitor<string>
{
    #region API
    public void Print(List<Stmt> statements)
    {
        foreach (Stmt statement in statements)
        {
            Console.WriteLine(statement.Accept(this));
        }
    }
    #endregion

    #region Expr visitor
    public string VisitAssignExpr(Expr.Assign expr)
    {
        return Parenthesize("=", expr.Name, expr.Value);
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
        return Parenthesize(".", expr.Object, expr.Name);
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
        return Parenthesize("=", expr.Object, expr.Name, expr.Value);
    }

    public string VisitThisExpr(Expr.This expr)
    {
        return "this";
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
        if (stmt.Superclass is null)
        {
            return Parenthesize("class", stmt.Name, stmt.Methods);
        }
        return Parenthesize("class", stmt.Name, "<", stmt.Superclass, stmt.Methods);
    }

    public string VisitExpressionStmt(Stmt.Expression stmt)
    {
        return Parenthesize(";", stmt.Expr);
    }

    public string VisitFunctionStmt(Stmt.Function stmt)
    {
        return Parenthesize("fun", stmt.Name, "(", stmt.Params, ")", stmt.Body);
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
        return Parenthesize("print", stmt.Expr);
    }

    public string VisitReturnStmt(Stmt.Return stmt)
    {
        if (stmt.Value is null)
        {
            return Parenthesize("return");
        }
        return Parenthesize("return", stmt.Value);
    }

    public string VisitVarStmt(Stmt.Var stmt)
    {
        if (stmt.Initializer is null)
        {
            return Parenthesize("var", stmt.Name);
        }
        return Parenthesize("var", stmt.Name, "=", stmt.Initializer);
    }

    public string VisitWhileStmt(Stmt.While stmt)
    {
        return Parenthesize("while", stmt.Condition, stmt.Body);
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Transforms the given parts into a parenthesized string whose contents start with the given
    /// label. If those parts are themselves AST nodes with defined handling, that will be
    /// respected.
    /// </summary>
    /// <param name="label">The label.</param>
    /// <param name="parts">The parts to transform.</param>
    /// <returns>A string representation of the parts.</returns>
    private string Parenthesize(string label, params object[] parts)
    {
        StringBuilder sb = new();

        sb.Append('(');
        sb.Append(label);
        Transform(sb, parts);
        sb.Append(')');

        return sb.ToString();
    }

    /// <summary>
    /// Transforms the given parts into strings and appends them.
    /// </summary>
    /// <param name="sb">The StringBuilder to which to append.</param>
    /// <param name="parts">The parts to transform.</param>
    private void Transform(StringBuilder sb, params object[] parts)
    {
        foreach (object part in parts)
        {
            // recurse on lists before appending, to avoid double-spaces
            if (part is IList list)
            {
                Transform(sb, [.. list.Cast<object>()]);
                continue;
            }

            sb.Append(' ');

            switch (part)
            {
                case Token token:
                    sb.Append(token.Lexeme);
                    break;
                case Expr expr:
                    sb.Append(expr.Accept(this));
                    break;
                case Stmt stmt:
                    sb.Append(stmt.Accept(this));
                    break;
                default:
                    sb.Append(part.ToString());
                    break;
            }
        }
    }
    #endregion
}
