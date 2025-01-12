namespace Lox.Tests;

public class LoxTests : IDisposable
{
    private readonly TextWriter originalStderr;
    private readonly TextWriter originalStdout;
    private readonly StringWriter stderr;
    private readonly StringWriter stdout;

    public LoxTests()
    {
        originalStdout = Console.Out;
        originalStderr = Console.Error;
        stdout = new StringWriter();
        stderr = new StringWriter();
        Console.SetOut(stdout);
        Console.SetError(stderr);
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
    public void HelloWorld()
    {
        Lox.Reset();
        Lox.Run("print \"hello world\";");
        Assert.Equal("hello world\n", stdout.ToString());
        Assert.Empty(stderr.ToString());
    }
}
