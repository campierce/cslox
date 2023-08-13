using System.CommandLine;
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
    private static bool _isPrintMode;

    public static async Task Main(string[] args)
    {
        RootCommand rootCommand = new("Interpreter for the Lox programming language.");

        Argument<string> scriptArgument = new(
            "script",
            "Script to run; if omitted, enter interactive mode.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> debugOption = new(
            new string[] { "-p", "--print" },
            "Print the syntax tree instead of executing it.");

        rootCommand.AddArgument(scriptArgument);
        rootCommand.AddOption(debugOption);

        rootCommand.SetHandler((debug, script) =>
            {
                _isPrintMode = debug;
                if (!string.IsNullOrEmpty(script))
                {
                    RunFile(script);
                }
                else
                {
                    RunPrompt();
                }
            },
            debugOption,
            scriptArgument);

        await rootCommand.InvokeAsync(args);
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
            if (line is null) // ctrl + d = end of input
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

        if (_isPrintMode)
        {
            AstPrinter printer = new();
            foreach (Stmt statement in statements)
            {
                Console.WriteLine(printer.Print(statement));
            }
            return;
        }

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
