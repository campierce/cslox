using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;
using Lox.AST;
using Lox.Interpreting;
using Lox.Scanning;
using Lox.StaticAnalysis;
using Parser = Lox.Parsing.Parser;

namespace Lox;

public class Lox
{
    #region Fields
    private static readonly Interpreter _interpreter = new();

    private static bool _hadError = false;

    private static bool _hadRuntimeError = false;

    private static bool _isPrintMode;
    #endregion

    public static async Task Main(string[] args)
    {
        RootCommand rootCommand = new("Interpreter for the Lox programming language.");

        Argument<FileInfo?> scriptArgument = new(
            name: "script",
            parse: GetFileFromArgument,
            isDefault: false,
            description: "Script to run; if omitted, enter interactive mode.")
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> debugOption = new(
            aliases: new string[] { "-p", "--print" },
            description: "Print the syntax tree instead of executing it.");

        rootCommand.AddArgument(scriptArgument);
        rootCommand.AddOption(debugOption);

        rootCommand.SetHandler((script, debug) =>
            {
                try
                {
                    _isPrintMode = debug;
                    if (script is null)
                    {
                        RunPrompt();
                    }
                    else
                    {
                        RunFile(script.FullName);
                    }
                }
                catch (Exception e)
                {
                    _hadRuntimeError = true;
                    Console.Error.WriteLine($"Unhandled error:{System.Environment.NewLine}{e}");
                }
            },
            scriptArgument,
            debugOption);

        await rootCommand.InvokeAsync(args);
    }

    #region API
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

        Console.Error.WriteLine($"{error.Line}{error.Type} error{error.Where}: {error.Message}");
    }
    #endregion

    #region Helpers
    private static FileInfo? GetFileFromArgument(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return null;
        }

        string? path = result.Tokens.Single().Value;
        if (!File.Exists(path))
        {
            result.ErrorMessage = $"File '{path}' does not exist";
            return null;
        }

        return new FileInfo(path);
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

    private static void RunFile(string path)
    {
        Run(File.ReadAllText(path, Encoding.UTF8));

        if (_hadError) { System.Environment.Exit(64); } // EX_USAGE
        if (_hadRuntimeError) { System.Environment.Exit(70); } // EX_SOFTWARE
    }

    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if (_hadError) { return; }

        Resolver resolver = new(_interpreter);
        resolver.Resolve(statements);

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

        _interpreter.Interpret(statements);
    }
    #endregion
}
