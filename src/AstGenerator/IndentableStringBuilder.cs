using System.Text;

namespace Lox.Tools;

public class IndentableStringBuilder
{
    /// <summary>
    /// The backing StringBuilder.
    /// </summary>
    private readonly StringBuilder _sb = new();

    /// <summary>
    /// The number of spaces per indent.
    /// </summary>
    private readonly int _indentWidth = 4;

    /// <summary>
    /// The number of spaces in the active indent.
    /// </summary>
    private int _activeIndent = 0;

    /// <summary>
    /// Whether to indent at the next opportunity.
    /// </summary>
    private bool _needsIndent = true;

    /// <summary>
    /// Increments the indent.
    /// </summary>
    public void Indent()
    {
        _activeIndent += _indentWidth;
    }

    /// <summary>
    /// Decrements the indent.
    /// </summary>
    public void Outdent()
    {
        if (_activeIndent >= _indentWidth)
        {
            _activeIndent -= _indentWidth;
        }
    }

    /// <summary>
    /// Appends the indent if needed (i.e., if this is the first append on the current line), and
    /// then appends the given string.
    /// </summary>
    /// <param name="value">The string to append.</param>
    public void Append(string value)
    {
        if (_needsIndent)
        {
            _sb.Append(new string(' ', _activeIndent));
            _needsIndent = false;
        }
        _sb.Append(value);
    }

    /// <summary>
    /// Appends the given string, and then appends a new line.
    /// </summary>
    /// <param name="value">The string to append.</param>
    public void AppendLine(string value)
    {
        Append(value);
        AppendLine();
    }

    /// <summary>
    /// Appends a new line.
    /// </summary>
    public void AppendLine()
    {
        _sb.AppendLine();
        _needsIndent = true;
    }

    public override string ToString() => _sb.ToString();
}
