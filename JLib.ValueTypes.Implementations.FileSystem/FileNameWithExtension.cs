namespace JLib.ValueTypes.Implementations.FileSystem;


/// <summary>
/// any valid filename without path information, but with and file extension<br/>
/// Validation may differ between operating systems due to the usage of <see cref="Path.GetInvalidFileNameChars"/><br/>
/// must not contain <see cref="Path.GetInvalidFileNameChars"/><br/>
/// must contain '.'
/// </summary>
/// <param name="Value">the filename</param>
public record FileNameWithExtension(string Value) : StringValueType(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> must)
        => must
            .NotContain(Path.GetInvalidFileNameChars())
            .Contain('.');
}
/// <summary> 
/// any valid filename with path information and file extension<br/>
/// Validation may differ between operating systems due to the usage of <see cref="Path.GetInvalidFileNameChars"/><br/>
/// must not contain <see cref="Path.GetInvalidFileNameChars"/><br/>
/// </summary>
/// <param name="Value">the filename</param>
public record FileNameWithoutExtension(string Value) : StringValueType(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> must)
        => must
            .NotContain(Path.GetInvalidFileNameChars());

    /// <summary>
    /// appends the <paramref name="extension"/> to the <paramref name="name"/>. If the extension is empty, the name is returned as is.
    /// </summary>
    public static FileNameWithExtension operator +(FileNameWithoutExtension name, FileExtension extension)
        => new(
            extension.Value == ""
            ? name.Value
            : $"{name.Value}.{extension.Value}"
        );
}

/// <summary>
/// any valid file extension<br/>
/// Validation may differ between operating systems due to the usage of <see cref="Path.GetInvalidFileNameChars"/><br/>
/// must not contain <see cref="Path.GetInvalidFileNameChars"/><br/>
/// must not start with '.'
/// </summary>
/// <param name="Value">the file extension</param>
public record FileExtension(string Value) : StringValueType(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> must)
        => must
            .NotStartWith('.')
            .NotContain(Path.GetInvalidFileNameChars());
}

/// <summary>
/// the name of a single directory in a path, not a <see cref="RelativeDirectoryPath"/>, <see cref="AbsoluteDirectoryPath"/> or <see cref="DriveLetter"/><br/><br/>
/// Validation may differ between operating systems due to the usage of <see cref="Path.GetInvalidPathChars"/><br/><br/>
/// must not contain <see cref="Path.GetInvalidPathChars"/><br/>
/// must not contain <see cref="Path.DirectorySeparatorChar"/><br/>
/// must not contain <see cref="Path.AltDirectorySeparatorChar"/><br/>
/// </summary>
/// <param name="Value">the directory name</param>
public record DirectoryName(string Value) : DirectoryPath(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> must)
        => must
            .NotContain(Path.DirectorySeparatorChar)
            .NotContain(Path.AltDirectorySeparatorChar);
}

/// <summary>
/// a complete or segment of a Directory Path.
/// </summary>
/// <seealso cref="Create"/>
/// <seealso cref="RelativeDirectoryPath"/>
/// <seealso cref="AbsoluteDirectoryPath"/>
/// <seealso cref="DirectoryName"/>
public abstract record DirectoryPath(string Value) : StringValueType(Value)
{
    /// <summary>
    /// Creates a <see cref="DirectoryPath"/> based on the <paramref name="value"/><br/>
    /// returns either a <see cref="AbsoluteDirectoryPath"/>, <see cref="RelativeDirectoryPath"/> or <see cref="DirectoryName"/>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static DirectoryPath Create(string value)
        => Path.IsPathRooted(value)
            ? new AbsoluteDirectoryPath(value)
            : value.Contains(Path.DirectorySeparatorChar) || value.Contains(Path.AltDirectorySeparatorChar)
                ? new DirectoryName(value)
                : new RelativeDirectoryPath(value);

    [Validation]
    private static void Validate(ValidationContext<string> must)
        => must.NotContain(Path.GetInvalidPathChars());
}
/// <summary>
/// a relative directory path <see cref="AbsoluteDirectoryPath"/>. It may contain only one directory and may use relative navigation<br/><br/>
/// Validation may differ between operating systems due to the usage of <see cref="Path.GetInvalidPathChars"/>
/// <remarks>
/// <br/><br/>
/// <see cref="Path.IsPathRooted(ReadOnlySpan{char})"/> must evaluate <paramref name="Value"/> to false
/// must not contain <see cref="Path.GetInvalidPathChars"/><br/>
/// may contain <see cref="Path.DirectorySeparatorChar"/><br/>
/// may contain <see cref="Path.AltDirectorySeparatorChar"/><br/>
/// </remarks>
/// </summary>
/// <param name="Value">the directory name</param>
public record RelativeDirectoryPath(string Value) : DirectoryPath(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> context)
    {
        if (Path.IsPathRooted(context.Value))
            context.Validate("The path must not be rooted");
    }
    /// <summary>
    /// 
    /// </summary>
    /// <returns><code>Path.Combine(path,relativePath)</code></returns>
    public static AbsoluteDirectoryPath operator +(RelativeDirectoryPath relativePath, RelativeDirectoryPath otherRelativePath)
        => new(Path.Combine(relativePath.Value, otherRelativePath.Value));
}
/// <summary>
/// A path including the <see cref="DriveLetter"/> and <see cref="RelativeDirectoryPath"/>
/// must not contain <see cref="Path.GetInvalidPathChars"/><br/>
/// <see cref="Path.IsPathRooted(ReadOnlySpan{char})"/> must evaluate to true
/// </summary>
/// <param name="Value"></param>
public record AbsoluteDirectoryPath(string Value) : DirectoryPath(Value)
{
    [Validation]
    private static void Validate(ValidationContext<string> must)
    {
        if (Path.IsPathRooted(must.Value) is not true)
            must.Validate("The Path must be rooted (System.IO.Path.IsPathRooted(Value)).");
    }

    /// <summary>
    /// appends the given <paramref name="relativePath"/> to the given <paramref name="absolutePath"/>, resulting in a new <see cref="AbsoluteDirectoryPath"/>
    /// </summary>
    /// <returns><code>Path.Combine(path,relativePath)</code></returns>
    public static AbsoluteDirectoryPath operator +(AbsoluteDirectoryPath absolutePath, RelativeDirectoryPath relativePath)
        => new(Path.Combine(absolutePath.Value, relativePath.Value));
}
/// <summary>
/// <paramref name="Value"/> must be an ascii drive
/// </summary>
/// <param name="Value"></param>
public record DriveLetter(char Value) : CharValueType(Value)
{
    [Validation]
    private static void Validate(ValidationContext<char> must)
        => must.BeAsciiLetter();

    /// <summary>
    /// combines the given <paramref cref="drive"/> with the given <paramref name="relativePath"/> to create a new <see cref="AbsoluteDirectoryPath"/>
    /// </summary>
    /// <returns><code>@$"{<paramref name="drive"/>}:\{<paramref name="relativePath"/>}"</code></returns>
    public static AbsoluteDirectoryPath operator +(DriveLetter drive, RelativeDirectoryPath relativePath)
        => new($"{drive.Value}:{Path.DirectorySeparatorChar}{relativePath.Value}");
}