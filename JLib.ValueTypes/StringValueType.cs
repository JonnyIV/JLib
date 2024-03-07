using System.Text.RegularExpressions;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.ValueTypes;

/// <summary>
/// Represents a base class for string value types.
/// </summary>
/// <typeparam name="T">The type of the string value.</typeparam>
public abstract record StringValueType(string Value) : ValueType<string>(Value)
{
    /// <summary>
    /// <inheritdoc cref="StringValueType"/>
    /// </summary>
    /// <param name="value">The value.</param>
    /// <param name="validate">a validator which is executed when the value is created</param>
    protected StringValueType(string value, Action<StringValidator>? validate) : this(value)
    {
        var validator = new StringValidator(Value, GetType().FullName());
        validate?.Invoke(validator);
        validator.CastTo<IExceptionProvider>().GetException()?.Throw();
    }
}

/// <summary>
/// Represents a validator for string values.
/// </summary>
public class StringValidator : ValueValidator<string?>
{
    public StringValidator(string? value, string valueTypeName) : base(value, valueTypeName)
    {
    }

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotBeNull()
    {
        if (Value is null)
            AddError("Value is null");
        return this;
    }

    /// <summary>
    /// Validates that the value is not null or empty.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotBeNullOrEmpty()
    {
        if (Value.IsNullOrEmpty())
            AddError("Value is null or empty");
        return this;
    }

    /// <summary>
    /// Validates that the value is one of the specified valid values.
    /// </summary>
    /// <param name="validValues">The collection of valid values.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeOneOf(IReadOnlyCollection<string> validValues)
    {
        if (!validValues.Contains(Value))
            AddError("Value is not one of the following: " + string.Join(", ", validValues));
        return this;
    }

    /// <summary>
    /// Validates that the value is alphanumeric.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeAlphanumeric()
        => SatisfyCondition(char.IsLetterOrDigit, nameof(BeAlphanumeric));

    /// <summary>
    /// Validates that the value satisfies the specified condition.
    /// </summary>
    /// <param name="validator">The condition to satisfy.</param>
    /// <param name="name">The name of the condition.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator SatisfyCondition(Func<char, bool> validator, string name)
    {
        if (Value is null)
        {
            AddError(name + " failed: string is null");
            return this;
        }

        var errorIndices = Value
            .AddIndex()
            .Where(x => !validator(x.Item1))
            .Select(x => x.Item2)
            .ToArray();
        if (errorIndices.Any())
            AddError($"{name} failed at index [{string.Join(", ", errorIndices)}]");

        if (Value.All(validator))
            return this;

        AddError(name + " failed");
        return this;
    }

    /// <summary>
    /// Validates that the value is not null or whitespace.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotBeNullOrWhitespace()
    {
        if (Value.IsNullOrEmpty())
            AddError("Value is null or whitespace");
        return this;
    }

    /// <summary>
    /// Validates that the value starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator StartWith(string prefix)
    {
        NotBeNull();
        if (Value == null || !Value.StartsWith(prefix))
            AddError($"Value does not start with {prefix}");
        return this;
    }

    /// <summary>
    /// Validates that the value does not contain the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotContain(string value)
    {
        if (Value != null && Value.Contains(value))
            AddError($"Value does not contain {value}");
        return this;
    }

    /// <summary>
    /// Validates that the value is a valid HTTPS URL.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeHttpsUrl()
    {
        NotBeNullOrWhitespace();
        NotContainWhitespace();
        NotContain("<");
        NotContain(">");
        StartWith("https://");
        if (!Uri.TryCreate(Value, UriKind.Absolute, out var uriResult)
            || uriResult.Scheme != Uri.UriSchemeHttps)
            AddError("Value is not a valid https uri");

        return this;
    }

    /// <summary>
    /// Validates that the value matches the specified regular expression.
    /// </summary>
    /// <param name="expression">The regular expression to match.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator MatchRegex(Regex expression)
    {
        NotBeNull();
        if (Value is null)
            return this;
        if (expression.IsMatch(Value) == false)
            AddError($"value does not match regex {expression}");
        return this;
    }

    /// <summary>
    /// Validates that the value contains only ASCII characters.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeAscii()
        => SatisfyCondition(char.IsAscii, nameof(BeAscii));

    /// <summary>
    /// Validates that the value contains only alphanumeric characters.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator OnlyContainAlphaNumericCharacters()
        => SatisfyCondition(char.IsLetterOrDigit, nameof(OnlyContainAlphaNumericCharacters));

    /// <summary>
    /// Validates that the value does not contain whitespace characters.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotContainWhitespace()
        => Value is null ? this : SatisfyCondition(c => !char.IsWhiteSpace(c), nameof(NotContainWhitespace));

    /// <summary>
    /// Validates that the value contains only numeric characters.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeNumeric()
        => SatisfyCondition(char.IsNumber, nameof(BeNumeric));

    /// <summary>
    /// Validates that the value has a minimum length.
    /// </summary>
    /// <param name="length">The minimum length.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator MinimumLength(int length)
    {
        NotBeNull();
        if (Value?.Length < length)
            AddError($"the value has to be at least {length} characters long but got length {Value.Length}");
        return this;
    }

    /// <summary>
    /// Validates that the value has a maximum length.
    /// </summary>
    /// <param name="length">The maximum length.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator MaximumLength(int length)
    {
        NotBeNull();
        if (Value?.Length > length)
            AddError($"the value has to be at most {length} characters long but got length {Value.Length}");
        return this;
    }

    /// <summary>
    /// Validates that the value has a specific length.
    /// </summary>
    /// <param name="length">The expected length.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeOfLength(int length)
    {
        NotBeNull();
        if (Value?.Length != length)
            AddError($"the value has to be exactly {length} characters long but got length {Value?.Length}");
        return this;
    }

    /// <summary>
    /// Validates that the value ends with the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator EndWith(string value)
    {
        if (Value?.EndsWith(value) != true)
            AddError($"the value has to end with '{value}'");
        return this;
    }
}
