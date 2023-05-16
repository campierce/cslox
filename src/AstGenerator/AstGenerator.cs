using System.Text;

namespace cslox.tool;

public class AstGenerator
{
    /// <summary>
    /// If the name of a parameter has this prefix, we strip it from
    /// the derived property name (allows you, e.g., to create an
    /// "Operator" prop despite "operator" being reserved.)
    /// </summary>
    private const char conditionalPrefix = '_';

    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: generate_ast <output directory>");
            Environment.Exit(64);
        }
        string outputDir = args[0];
        DefineAst(
            outputDir,
            "Expression",
            // "operator" is a keyword, so prefix with underscore
            new List<string> {
                "Binary   : Expression left, Token _operator, Expression right",
                "Grouping : Expression expression",
                "Literal  : Object value",
                "Unary    : Token _operator, Expression right"
            }
        );
    }

    private static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        string path = Path.Combine(outputDir, baseName + ".cs");
        using StreamWriter writer = new(path, false, Encoding.UTF8);
        writer.Write($@"using cslox.lox.scanner;

namespace cslox.lox.ir;");
        writer.Write($@"

internal abstract class {baseName}
{{");
        foreach (string type in types)
        {
            string className = type.Split(':')[0].Trim();
            string paramList = type.Split(':')[1].Trim();
            DefineType(writer, baseName, className, paramList);
        }
        writer.Write(@"
}
");
    }

    private static void DefineType(
        StreamWriter writer, string baseName, string className, string paramList)
    {
        string[] attrs = paramList.Split(", ");

        // nested class
        writer.Write($@"
    internal class {className} : {baseName}
    {{");
        // properties
        foreach (string attr in attrs)
        {
            string type = attr.Split(' ')[0];
            string name = attr.Split(' ')[1];
            writer.Write($@"
        internal {type} {GetPropName(name)} {{ get; }}");
        }
        // constructor
        writer.Write($@"
        internal {className}({paramList})
        {{");
        foreach (string attr in attrs)
        {
            string name = attr.Split(' ')[1];
            writer.Write($@"
            {GetPropName(name)} = {name};");
        }
        writer.Write(@"
        }
    }");
    }

    #region String helpers
    private static string GetPropName(string source)
    {
        if (source.StartsWith(conditionalPrefix))
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
