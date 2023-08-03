namespace Lox.Interpreting;

internal class Return : Exception
{
    public object Value { get; }

    public Return(object value) : base(string.Empty, null)
    {
        Value = value;
    }

    public override string StackTrace => string.Empty;
}
