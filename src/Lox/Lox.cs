using System.Text;
using Lox.Interpreting;
using Lox.IR;
using Lox.Parsing;
using Lox.Scanning;
using static Lox.Scanning.TokenType;

namespace Lox;

public class Lox
{
    private static readonly Interpreter interpreter = new();
    private static bool _hadError = false;
    private static bool _hadRuntimeError = false;

    public static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.Error.WriteLine("Usage: cslox [script]");
            System.Environment.Exit(64);
        }
        else if (args.Length == 1)
        {
            RunFile(args[0]);
        }
        else
        {
            RunPrompt();
        }
    }

    private static void RunFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Run(Encoding.Default.GetString(bytes));

        if (_hadError) { System.Environment.Exit(64); } // EX_USAGE
        if (_hadRuntimeError) { System.Environment.Exit(70); } // EX_SOFTWARE
    }

    private static void RunPrompt()
    {
        using StreamReader reader = new(Console.OpenStandardInput());

        while (true)
        {
            Console.Write("> ");
            string? line = reader.ReadLine();
            if (line == null) // ctrl + d = end of input
            {
                break;
            }
            Run(line);
            _hadError = false;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if (_hadError) { return; }

        /*
        AstPrinter printer = new();
        foreach (Stmt statement in statements)
        {
            Console.WriteLine(printer.Print(statement));
        }
        */

        interpreter.Interpret(statements);
    }

    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        _hadError = true;
    }

    internal static void Error(int line, string message)
    {
        Report(line, string.Empty, message);
    }

    internal static void Error(Token token, string message)
    {
        if (token.Type == EOF)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, " at '" + token.Lexeme + "'", message);
        }
    }

    internal static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine($"{error.Message}\n[line {error.Token.Line}]");
        _hadRuntimeError = true;
    }
}
