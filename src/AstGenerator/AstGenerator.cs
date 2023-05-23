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
            "Expr",
            // "operator" is a keyword, so prefix it
            new List<string> {
                $"Binary   : Expr left, Token {conditionalPrefix}operator, Expr right",
                "Grouping : Expr expression",
                "Literal  : Object value",
                $"Unary    : Token {conditionalPrefix}operator, Expr right"
            }
        );
    }

    #region Writer helpers
    private static void DefineAst(string outputDir, string baseName, List<string> types)
    {
        string path = Path.Combine(outputDir, baseName + ".cs");
        using StreamWriter writer = new(path, false, Encoding.UTF8);
        
        // top of file
        writer.Write($@"using cslox.lox.scanner;

namespace cslox.lox.ir;");

        // abc
        writer.Write($@"

internal abstract class {baseName}
{{");

        // abstract methods
        writer.Write(@"
    public abstract R Accept<R>(Visitor<R> visitor);");

        // visitor interface
        DefineVisitor(writer, baseName, types);

        // concrete subclasses
        foreach (string type in types)
        {
            string className = type.Split(':')[0].Trim();
            string paramList = type.Split(':')[1].Trim();
            DefineType(writer, baseName, className, paramList);
        }

        // done
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
        public {type} {GetPropertyName(name)} {{ get; }}");
        }
        
        // constructor
        writer.Write($@"
        
        public {className}({paramList})
        {{");
        foreach (string attr in attrs)
        {
            string name = attr.Split(' ')[1];
            writer.Write($@"
            {GetPropertyName(name)} = {name};");
        }
        writer.Write(@"
        }");

        // implementations
        writer.Write($@"

        public override R Accept<R>(Visitor<R> visitor)
        {{
            return visitor.Visit{className}{baseName}(this);
        }}");

        // done
        writer.Write(@"
    }");
    }

    private static void DefineVisitor(StreamWriter writer, string baseName, List<string> types)
    {
        // nested interface
        writer.Write(@"
    
    internal interface Visitor<R>
    {");

        // methods
        foreach (string type in types)
        {
            string className = type.Split(':')[0].Trim();
            writer.Write($@"
        R Visit{className}{baseName}({className} {baseName.ToLower()});
            ");
        }

        // done
        writer.Write(@"
    }");
    }
    #endregion

    #region String helpers
    private static string GetPropertyName(string source)
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
