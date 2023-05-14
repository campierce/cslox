using System.Text;

namespace cslox.tool;

public class AstGenerator
{
    public static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Usage: generate_ast <output directory>");
            Environment.Exit(64);
        }
        string outputDir = args[0];
        defineAst(
            outputDir,
            "Expr",
            new List<string> {
                "Binary   : Expr left, Token operator, Expr right",
                "Grouping : Expr expression",
                "Literal  : Object value",
                "Unary    : Token operator, Expr right"
            }
        );
    }

    private static void defineAst(string outputDir, string baseName, List<string> types)
    {
        string path = Path.Combine(outputDir, baseName + ".cs");
        using StreamWriter writer = new(path, false, Encoding.UTF8);
        writer.WriteLine(
$@"namespace cslox.lox;

public abstract class {baseName}
{{
}}");
    }
}
