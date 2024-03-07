using System.Text;

namespace JLib.Helper;

/// <summary>
/// Provides helper methods for string manipulation.
/// </summary>
public static class StringHelper
{
    /// <summary>
    /// Determines whether the specified string is null, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <returns>true if the value parameter is null or <see cref="string.Empty"/>, or if value consists exclusively of white-space characters; otherwise, false.</returns>
    public static bool IsNullOrWhitespace(this string? value)
        => string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Determines whether the specified string is null or an empty string.
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <returns>true if the value parameter is null or an empty string (""); otherwise, false.</returns>
    public static bool IsNullOrEmpty(this string? value)
        => string.IsNullOrEmpty(value);

    /// <summary>
    /// Concatenates the specified string the specified number of times.
    /// </summary>
    /// <param name="str">The string to repeat.</param>
    /// <param name="count">The number of times to repeat the string.</param>
    /// <returns>A new string that consists of the specified string repeated the specified number of times.</returns>
    public static string Repeat(this string str, int count)
        => new StringBuilder()
            .AppendMultiple(str, count)
            .ToString();

    /// <summary>
    /// Appends the specified string to the <see cref="StringBuilder"/> the specified number of times.
    /// </summary>
    /// <param name="sb">The <see cref="StringBuilder"/> to append to.</param>
    /// <param name="value">The string to append.</param>
    /// <param name="count">The number of times to append the string.</param>
    /// <returns>The <see cref="StringBuilder"/> after the strings have been appended.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the count is less than zero.</exception>
    public static StringBuilder AppendMultiple(this StringBuilder sb, string value, int count)
    {
        if (count < 0)
            throw new ArgumentOutOfRangeException(nameof(count));

        for (var i = 0; i < count; i++)
            sb.Append(value);

        return sb;
    }
}
