namespace Lox.Tests;

public class LoxTests : IDisposable
{
    private readonly TextWriter originalStderr;
    private readonly TextWriter originalStdout;
    private readonly StringWriter stderr;
    private readonly StringWriter stdout;
    private string Stderr => stderr.ToString();
    private string Stdout => stdout.ToString();

    public LoxTests()
    {
        originalStdout = Console.Out;
        originalStderr = Console.Error;
        stdout = new StringWriter();
        stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
        Lox.Reset();
    }

    public void Dispose()
    {
        Console.SetOut(originalStdout);
        Console.SetError(originalStderr);
        stdout.Dispose();
        stderr.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void NonAlphaNumericCharIsUnexpectedOutsideString()
    {
        var code = "var ñ;";
        Lox.Run(code);
        Assert.Contains("Unexpected character.", Stderr);
    }

    [Fact]
    public void NonAlphaNumericCharIsFineInsideString()
    {
        var code = "print \"ñ\";";
        Lox.Run(code);
        var expected = "ñ\n";
        Assert.Equal(expected, Stdout);
        Assert.Empty(stderr.ToString());
    }

    [Fact]
    public void FailsToScanUnterminatedString()
    {
        var code = "print \"hello";
        Lox.Run(code);
        Assert.Contains("Unterminated string.", Stderr);
    }
}
