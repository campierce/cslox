using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;

namespace Lox;

public class Lox
{
    #region State
    private static readonly Interpreter _interpreter = new();

    private static readonly AstPrinter _printer = new();

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
            description: "Script to run; if omitted, enter interactive mode."
        )
        {
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> isPrintModeOption = new(
            aliases: ["-p", "--print"],
            description: "Print the syntax tree instead of executing it."
        );

        rootCommand.AddArgument(scriptArgument);
        rootCommand.AddOption(isPrintModeOption);

        rootCommand.SetHandler((script, isPrintMode) =>
            {
                try
                {
                    _isPrintMode = isPrintMode;
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
            isPrintModeOption
        );

        await rootCommand.InvokeAsync(args);
    }

    #region API
    public static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    internal static void Error(Token token, string message)
    {
        if (token.Type == TokenType.EOF)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, $" at '{token.Lexeme}'", message);
        }
    }

    internal static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine(
            $"{error.Message}{System.Environment.NewLine}[line {error.Token.Line}]"
        );
        _hadRuntimeError = true;
    }
    #endregion

    #region Helpers
    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        _hadError = true;
    }

    private static FileInfo? GetFileFromArgument(ArgumentResult result)
    {
        if (result.Tokens.Count == 0)
        {
            return null;
        }

        string? path = result.Tokens.Single().Value;
        if (!File.Exists(path))
        {
            // hook into the library's error handling
            result.ErrorMessage = $"File '{path}' does not exist";
            return null;
        }

        return new FileInfo(path);
    }

    private static void RunPrompt()
    {
        TextReader reader = Console.In;

        while (true)
        {
            Console.Write("> ");
            string? line = reader.ReadLine();
            if (line is null) // ctrl + d
            {
                break;
            }
            Run(line);
            _hadError = false;
        }
    }

    private static void RunFile(string path)
    {
        string source = File.ReadAllText(path, Encoding.UTF8);
        Run(source);

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
            _printer.Print(statements);
            return;
        }

        _interpreter.Interpret(statements);
    }
    #endregion
}
