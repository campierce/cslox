using System.Globalization;

namespace Lox.Tools;

public class AstGenerator
{
    /// <summary>
    /// If a parameter has this prefix, we remove it from the derived property name. Allows you,
    /// e.g., to define an `@operator` parameter that becomes an `Operator` property.
    /// </summary>
    private const char verbatimPrefix = '@';

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: AstGenerator <output directory>");
            Environment.Exit(64);
        }

        string outputDir = args[0];
        if (!Directory.Exists(outputDir))
        {
            Console.Error.WriteLine($"Directory '{outputDir}' does not exist.");
            Environment.Exit(64);
        }

        // define expression types
        DefineAst(
            outputDir,
            "Expr",
            [
                "Assign   : Token name, Expr value",
                $"Binary  : Expr left, Token {verbatimPrefix}operator, Expr right",
                "Call     : Expr callee, Token paren, List<Expr> arguments",
                $"Get     : Expr {verbatimPrefix}object, Token name",
                "Grouping : Expr expression",
                "Literal  : object value",
                $"Logical : Expr left, Token {verbatimPrefix}operator, Expr right",
                $"Set     : Expr {verbatimPrefix}object, Token name, Expr value",
                "Super    : Token keyword, Token method",
                "This     : Token keyword",
                $"Unary   : Token {verbatimPrefix}operator, Expr right",
                "Variable : Token name"
            ]
        );

        // define statement types
        DefineAst(
            outputDir,
            "Stmt",
            [
                "Block      : List<Stmt> statements",
                "Class      : Token name, Expr.Variable? superclass, List<Function> methods",
                "Expression : Expr expr",
                $"Function  : Token name, List<Token> {verbatimPrefix}params, List<Stmt> body",
                "If         : Expr condition, Stmt thenBranch, Stmt? elseBranch",
                "Print      : Expr expr",
                "Return     : Token keyword, Expr? value",
                "Var        : Token name, Expr? initializer",
                "While      : Expr condition, Stmt body"
            ]
        );
    }

    #region String building
    private static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        IndentableStringBuilder sb = new();

        // top of file
        sb.AppendLine("namespace Lox;");
        sb.AppendLine();

        // abstract base class
        sb.AppendLine("// Generated code; see AstGenerator to make changes.");
        sb.AppendLine($"internal abstract class {baseName}");
        sb.AppendLine("{");
        sb.Indent();

        // abstract methods
        sb.AppendLine("public abstract T Accept<T>(IVisitor<T> visitor);");
        sb.AppendLine();

        // visitor interface
        DefineVisitor(sb, baseName, types);

        // concrete subclasses
        foreach (string type in types)
        {
            sb.AppendLine();
            string className = type.Split(':')[0].Trim();
            string paramList = type.Split(':')[1].Trim();
            DefineType(sb, baseName, className, paramList);
        }

        // done
        sb.Outdent();
        sb.AppendLine("}");

        // write it out
        string path = Path.Combine(outputDir, baseName + ".cs");
        File.WriteAllText(path, sb.ToString());
    }

    private static void DefineType(
        IndentableStringBuilder sb, string baseName, string className, string paramList)
    {
        string[] attrs = paramList.Split(", ");

        // nested class
        sb.AppendLine($"internal class {className} : {baseName}");
        sb.AppendLine("{");
        sb.Indent();

        // properties
        foreach (string attr in attrs)
        {
            string type = attr.Split(' ')[0];
            string name = attr.Split(' ')[1];
            sb.AppendLine($"public {type} {GetPropertyName(name)} {{ get; }}");
        }
        sb.AppendLine();

        // constructor
        sb.AppendLine($"public {className}({paramList})");
        sb.AppendLine("{");
        sb.Indent();
        foreach (string attr in attrs)
        {
            string name = attr.Split(' ')[1];
            sb.AppendLine($"{GetPropertyName(name)} = {name};");
        }
        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();

        // implementations
        sb.AppendLine("public override T Accept<T>(IVisitor<T> visitor)");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"return visitor.Visit{className}{baseName}(this);");
        sb.Outdent();
        sb.AppendLine("}");

        // done
        sb.Outdent();
        sb.AppendLine("}");
    }

    private static void DefineVisitor(
        IndentableStringBuilder sb, string baseName, List<string> types)
    {
        // nested interface
        sb.AppendLine("internal interface IVisitor<T>");
        sb.Append("{");
        sb.Indent();

        // methods
        foreach (string type in types)
        {
            sb.AppendLine();
            string className = type.Split(':')[0].Trim();
            string paramName = baseName.ToLower(CultureInfo.InvariantCulture);
            sb.AppendLine("/// <summary>");
            sb.AppendLine($"/// Visits the given {className}.");
            sb.AppendLine("/// </summary>");
            sb.AppendLine($"/// <param name=\"{paramName}\">The {className} to visit.</param>");
            sb.AppendLine("/// <returns>A value of type <typeparamref name=\"T\"/>.</returns>");
            sb.AppendLine($"T Visit{className}{baseName}({className} {paramName});");
        }

        // done
        sb.Outdent();
        sb.AppendLine("}");
    }

    private static string GetPropertyName(string source)
    {
        if (source.StartsWith(verbatimPrefix))
        {
            source = source[1..];
        }
        string first = source[0].ToString().ToUpper(CultureInfo.InvariantCulture);
        return $"{first}{source[1..]}";
    }
    #endregion
}
