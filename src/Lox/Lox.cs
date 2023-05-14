using System.Text;

namespace cslox.lox;

public class Lox
{
    private static bool hadError = false;

    public static void Main(string[] args)
    {
        if (args.Length > 1)
        {
            Console.WriteLine("Usage: cslox [script]");
            Environment.Exit(64);
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

        if (hadError)
        {
            Environment.Exit(64);
        }
    }

    private static void RunPrompt()
    {
        using StreamReader reader = new StreamReader(Console.OpenStandardInput());

        while (true)
        {
            Console.Write("> ");
            string? line = reader.ReadLine();
            if (line == null)
            {
                break;
            }
            Run(line);
            hadError = false;
        }
    }

    private static void Run(string source)
    {
        Scanner scanner = new Scanner(source);
        List<Token> tokens = scanner.ScanTokens();

        // for now, just print
        foreach (Token token in tokens)
        {
            Console.WriteLine(token);
        }
    }

    internal static void Error(int line, string message)
    {
        Report(line, string.Empty, message);
    }

    private static void Report(int line, string where, string message)
    {
        Console.WriteLine($"[line {line}] Error{where}: {message}");
        hadError = true;
    }
}
