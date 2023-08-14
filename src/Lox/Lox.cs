using System.CommandLine;
using System.Text;
using Lox.Interpreting;
using Lox.IR;
using Lox.Parsing;
using Lox.Scanning;

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
                try
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
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine($"Unhandled error: {e}");
                    _hadError = true;
                }
            },
            debugOption,
            scriptArgument);

        await rootCommand.InvokeAsync(args);
    }

    private static void RunFile(string path)
    {
        try
        {
            string content = File.ReadAllText(path, Encoding.UTF8);
            Run(content);
        }
        catch (Exception e)
        {
            Error(new ScanningError(e.Message));
        }

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

    internal static void Error(Error error)
    {
        if (error is RuntimeError)
        {
            _hadRuntimeError = true;
        }
        else
        {
            _hadError = true;
        }

        Console.Error.WriteLine(
            $"{error.Line}{error.Name} error{error.Where}: {error.Message}"
        );
    }
}
