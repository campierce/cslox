namespace Lox;

static class EnumerableExtensions
{
    public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> input)
    {
        int i = 0;
        foreach (T item in input)
        {
            yield return (i++, item);
        }
    }
}
