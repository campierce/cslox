namespace Lox;

internal class Return : Exception
{
    public object Value { get; }

    public Return(object value) : base(null, null)
    {
        Value = value;
    }
}
