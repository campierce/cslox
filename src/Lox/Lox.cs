using System.CommandLine;
using System.CommandLine.Parsing;
using System.Text;

namespace Lox;

public class Lox
{
    #region State
    /// <summary>
    /// Whether an error was reported (this is reset between REPL prompts).
    /// </summary>
    private static bool s_hadError = false;

    /// <summary>
    /// Whether a runtime error was reported.
    /// </summary>
    private static bool s_hadRuntimeError = false;

    /// <summary>
    /// The interpreter.
    /// </summary>
    private static readonly Interpreter s_interpreter = new();

    /// <summary>
    /// Whether the user wants to print the AST instead of executing it.
    /// </summary>
    private static bool s_isPrintMode;

    /// <summary>
    /// The AST printer.
    /// </summary>
    private static readonly AstPrinter s_printer = new();
    #endregion

    /// <summary>
    /// Program entry point.
    /// </summary>
    /// <param name="args">Program arguments.</param>
    /// <returns>A task that completes when the program exits.</returns>
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

        rootCommand.SetHandler(async (script, isPrintMode) =>
            {
                try
                {
                    s_isPrintMode = isPrintMode;
                    if (script is null)
                    {
                        RunPrompt();
                    }
                    else
                    {
                        await RunFile(script.FullName);
                    }
                }
                catch (Exception e)
                {
                    s_hadRuntimeError = true;
                    Console.Error.WriteLine($"Unhandled error:{System.Environment.NewLine}{e}");
                }
            },
            scriptArgument,
            isPrintModeOption
        );

        await rootCommand.InvokeAsync(args);
    }

    #region Internal API
    /// <summary>
    /// Reports an error. (Call this overload when you don't have a token.)
    /// </summary>
    /// <param name="line">The error line.</param>
    /// <param name="message">The error message.</param>
    internal static void Error(int line, string message)
    {
        Report(line, "", message);
    }

    /// <summary>
    /// Reports an error.
    /// </summary>
    /// <param name="token">The token where the error occurred.</param>
    /// <param name="message">The error message.</param>
    internal static void Error(Token token, string message)
    {
        if (token.Type == TokenType.Eof)
        {
            Report(token.Line, " at end", message);
        }
        else
        {
            Report(token.Line, $" at '{token.Lexeme}'", message);
        }
    }

    /// <summary>
    /// Reports a runtime error.
    /// </summary>
    /// <param name="error">The error.</param>
    internal static void RuntimeError(RuntimeError error)
    {
        Console.Error.WriteLine(
            $"{error.Message}{System.Environment.NewLine}[line {error.Token.Line}]"
        );
        s_hadRuntimeError = true;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Formats an error message and writes it to stderr.
    /// </summary>
    /// <param name="line">The error line.</param>
    /// <param name="where">The error location.</param>
    /// <param name="message">The error message.</param>
    private static void Report(int line, string where, string message)
    {
        Console.Error.WriteLine($"[line {line}] Error{where}: {message}");
        s_hadError = true;
    }

    /// <summary>
    /// Parses the script argument.
    /// </summary>
    /// <param name="result">The argument to parse.</param>
    /// <returns>A FileInfo for the script; null if argument was omitted, logs error if script does
    /// not exist.</returns>
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

    /// <summary>
    /// Runs the REPL prompt.
    /// </summary>
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
            s_hadError = false;
        }
    }

    /// <summary>
    /// Runs a file.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>A task that completes when the method exits.</returns>
    private static async Task RunFile(string path)
    {
        string source = await File.ReadAllTextAsync(path, Encoding.UTF8);
        Run(source);

        if (s_hadError) { System.Environment.Exit(64); } // EX_USAGE
        if (s_hadRuntimeError) { System.Environment.Exit(70); } // EX_SOFTWARE
    }

    /// <summary>
    /// Runs a string of Lox source code.
    /// </summary>
    /// <param name="source">The source code to run.</param>
    private static void Run(string source)
    {
        Scanner scanner = new(source);
        List<Token> tokens = scanner.ScanTokens();

        Parser parser = new(tokens);
        List<Stmt> statements = parser.Parse();

        if (s_hadError) { return; }

        Resolver resolver = new(s_interpreter);
        resolver.Resolve(statements);

        if (s_hadError) { return; }

        if (s_isPrintMode)
        {
            s_printer.Print(statements);
            return;
        }

        s_interpreter.Interpret(statements);
    }
    #endregion
}
