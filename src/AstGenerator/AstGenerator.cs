namespace Lox.Tools;

public class AstGenerator
{
    /// <summary>
    /// If the name of a parameter has this prefix, we strip it from
    /// the derived property name (allows you, e.g., to create an
    /// "Operator" prop despite "operator" being reserved.)
    /// </summary>
    private const char escapePrefix = '@';

    // {dir_with_assembly} > ./generate_ast ~/code/cslox/src/Lox/IR
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("Usage: generate_ast <output directory>");
            Environment.Exit(64);
        }
        string outputDir = args[0];

        // define expression types
        DefineAst(
            outputDir,
            "Expr",
            new List<string>
            {
                $"Binary  : Expr left, Token {escapePrefix}operator, Expr right",
                "Grouping : Expr expression",
                "Literal  : Object value",
                $"Unary   : Token {escapePrefix}operator, Expr right"
            }
        );

        // define statement types
        /*
        DefineAst(
            outputDir,
            "Stmt",
            new List<string>
            {
                // TODO rename Expression or its Expression property
                "Expression : Expr expression",
                "Print      : Expr expression"
            },
            false
        );
        */
    }

    #region StringBuilder helpers
    private static void DefineAst(
        string outputDir, string baseName, List<string> types, bool usesTokens = true)
    {
        IndentableStringBuilder sb = new();

        // top of file
        if (usesTokens)
        {
            sb.AppendLine("using Lox.Scanning;");
            sb.AppendLine();
        }
        sb.AppendLine("namespace Lox.IR;");
        sb.AppendLine();

        // abstract base class
        sb.AppendLine($"internal abstract class {baseName}");
        sb.AppendLine("{");
        sb.Indent();

        // abstract methods
        sb.AppendLine("public abstract T Accept<T>(Visitor<T> visitor);");
        sb.AppendLine();

        // visitor interface
        DefineVisitor(sb, baseName, types);
        sb.AppendLine();

        // concrete subclasses
        foreach (string type in types)
        {
            string className = type.Split(':')[0].Trim();
            string paramList = type.Split(':')[1].Trim();
            DefineType(sb, baseName, className, paramList);
        }
        sb.RetractLastLine(); // remove the last newline

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
        sb.AppendLine("public override T Accept<T>(Visitor<T> visitor)");
        sb.AppendLine("{");
        sb.Indent();
        sb.AppendLine($"return visitor.Visit{className}{baseName}(this);");
        sb.Outdent();
        sb.AppendLine("}");

        // done
        sb.Outdent();
        sb.AppendLine("}");
        sb.AppendLine();
    }

    private static void DefineVisitor(
        IndentableStringBuilder sb, string baseName, List<string> types)
    {
        // nested interface
        sb.AppendLine("internal interface Visitor<T>");
        sb.AppendLine("{");
        sb.Indent();

        // methods
        foreach (string type in types)
        {
            string className = type.Split(':')[0].Trim();
            sb.AppendLine($"T Visit{className}{baseName}({className} {baseName.ToLower()});");
            sb.AppendLine();
        }

        // done
        sb.RetractLastLine(); // remove the last newline
        sb.Outdent();
        sb.AppendLine("}");
    }
    #endregion

    #region Static helpers
    private static string GetPropertyName(string source)
    {
        if (source.StartsWith(escapePrefix))
        {
            source = source[1..];
        }
        return Capitalize(source);
    }

    private static string Capitalize(string source)
    {
        return source[0].ToString().ToUpper() + source[1..];
    }
    #endregion
}
