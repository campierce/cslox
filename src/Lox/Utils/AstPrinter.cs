using System.Text;
using cslox.lox.ir;
// using cslox.lox.scanner;

namespace cslox.lox;

internal class AstPrinter : Expr.Visitor<string>
{
    #region API
    public string Print(Expr expr)
    {
        return expr.Accept(this);
    }
    #endregion

    #region Helpers
    private string Parenthesize(string name, params Expr[] exprs)
    {
        StringBuilder builder = new StringBuilder();

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

    #region Implementations
    string Expr.Visitor<string>.VisitBinaryExpr(Expr.Binary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Left, expr.Right);
    }

    string Expr.Visitor<string>.VisitGroupingExpr(Expr.Grouping expr)
    {
        return Parenthesize("group", expr.Expression);
    }

    string Expr.Visitor<string>.VisitLiteralExpr(Expr.Literal expr)
    {
        if (expr.Value is null)
        {
            return "nil";
        }
        return expr.Value.ToString()!;
    }

    string Expr.Visitor<string>.VisitUnaryExpr(Expr.Unary expr)
    {
        return Parenthesize(expr.Operator.Lexeme, expr.Right);
    }
    #endregion

    /*
    public static void TestMe()
    {
        // -123 * (45.67)
        Expr expression = new Expr.Binary(
            new Expr.Unary(
                new Token(TokenType.MINUS, "-", null, 1),
                new Expr.Literal(123)),
            new Token(TokenType.STAR, "*", null, 1),
            new Expr.Grouping(new Expr.Literal(45.67)));

        Console.WriteLine(new AstPrinter().Print(expression));
    }
    */
}
