namespace Lox.Tools;

using System.Text;

public class IndentableStringBuilder
{
    private readonly StringBuilder _sb = new();
    private readonly int _newlineLength = Environment.NewLine.Length;
    private readonly int _width = 4;
    private int _spaces = 0;

    public void Indent()
    {
        _spaces += _width;
    }

    public void Outdent()
    {
        if (_spaces >= _width)
        {
            _spaces -= _width;
        }
    }

    public void RetractLastLine()
    {
        if (_sb.Length >= _newlineLength)
        {
            _sb.Length -= _newlineLength;
        }
    }

    public void Append(string value)
    {
        _sb.Append(value);
    }

    public void AppendLine(string value)
    {
        _sb.AppendLine(SpacePrefixed(value));
    }

    public void AppendLine()
    {
        _sb.AppendLine();
    }

    public override string ToString() => _sb.ToString();

    private string SpacePrefixed(string value)
    {
        return new string(' ', _spaces) + value;
    }
}
