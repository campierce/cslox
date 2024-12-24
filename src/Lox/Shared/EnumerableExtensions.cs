namespace Lox;

internal static class EnumerableExtensions
{
    /// <summary>
    /// Enumerates (index, item) pairs of an <see cref="IEnumerable{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of items in the input enumerable.</typeparam>
    /// <param name="input">The input enumerable.</param>
    /// <returns>A new enumerable of (index, item) pairs.</returns>
    public static IEnumerable<(int, T)> Enumerate<T>(this IEnumerable<T> input)
    {
        int i = 0;
        foreach (T item in input)
        {
            yield return (i++, item);
        }
    }
}
