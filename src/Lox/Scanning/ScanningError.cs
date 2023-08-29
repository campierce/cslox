namespace Lox.Scanning;

internal class ScanningError : Error
{
    public override string Type => "Scanning";

    public ScanningError(int line, string message) : base(line, message)
    {
    }
}
