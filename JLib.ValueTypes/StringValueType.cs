﻿using System.Text.RegularExpressions;
using JLib.Exceptions;
using JLib.Helper;

namespace JLib.ValueTypes;

/// <summary>
/// Represents a base class for string value types.
/// </summary>
/// <typeparam name="T">The type of the string value.</typeparam>
public abstract record StringValueType(string Value) : ValueType<string>(Value)
{
    [Validation]
    private static void Validate(StringValidator v)
        => v.NotBeNull();
}

/// <summary>
/// Represents a validator for string values.
/// </summary>
public class StringValidator : ValidationContext<string?>
{
    /// <summary>
    /// creates a validator for <paramref name="targetType"/> and the specified <paramref name="value"/> of type <see cref="string"/>
    /// </summary>
    /// <param name="value">the value which should be validated</param>
    /// <param name="targetType">the <see cref="Type"/> this <paramref name="value"/> is being validated for</param>
    public StringValidator(string? value, Type targetType) : base(value, targetType)
    {
    }

    /// <summary>
    /// Validates that the value is not null.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotBeNull()
    {
        if (Value is null)
            AddError("Value must not be null");
        return this;
    }

    /// <summary>
    /// Validates that the value is not null or empty.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotBeNullOrEmpty()
    {
        if (Value.IsNullOrEmpty())
            AddError("Value must neither be null nor empty");
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
            AddError("Value must be one of the following: " + string.Join(", ", validValues));
        return this;
    }

    /// <summary>
    /// Validates that the value is alphanumeric.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeAlphanumeric()
        => SatisfyCondition(char.IsLetterOrDigit, "value must be alphanumeric");

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
        foreach (var errorIndex in errorIndices)
        {
            int minLength = 10;

            int start = Math.Max(0, errorIndex - minLength / 2);
            int end = Math.Min(Value.Length, errorIndex + minLength / 2);

            string errorPart = Value.Substring(start, end - start);
            AddError($"{name} failed at index {errorIndex}. \"{errorPart}\"");
        }

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
            AddError("Value must neither be null nor whitespace");
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
            AddError($"Value must start with {prefix}");
        return this;
    }
    /// <summary>
    /// Validates that the value does not start with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotStartWith(string prefix)
    {
        NotBeNull();
        if (Value == null || Value.StartsWith(prefix))
            AddError($"Value must not start with {prefix}");
        return this;
    }
    /// <summary>
    /// Validates that the value starts with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator StartWith(char prefix)
    {
        NotBeNull();
        if (Value == null || !Value.StartsWith(prefix))
            AddError($"Value must start with {prefix}");
        return this;
    }
    /// <summary>
    /// Validates that the value does not start with the specified prefix.
    /// </summary>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotStartWith(char prefix)
    {
        NotBeNull();
        if (Value == null || Value.StartsWith(prefix))
            AddError($"Value must not start with {prefix}");
        return this;
    }

    /// <summary>
    /// Validates that the value does not contain the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator Contain(string value)
    {
        if (Value != null && Value.Contains(value) == false)
            AddError($"Value must contain {value}");
        return this;
    }

    /// <summary>
    /// Validates that the value does not contain the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator Contain(char value)
    {
        if (Value != null && Value.Contains(value) == false)
            AddError($"Value must contain {value}");
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
            AddError($"Value must not contain {value}");
        return this;
    }

    /// <summary>
    /// Validates that the value does not contain the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotContain(char value)
    {
        if (Value != null && Value.Contains(value))
            AddError($"Value must not contain {value}");
        return this;
    }

    /// <summary>
    /// Validates that the value is a URL of the specified kind.
    /// </summary>
    /// <param name="kind">The kind of URL to validate.</param>
    /// <param name="uriValidator">An optional validator for the created Uri object.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeUrl(UriKind kind, Action<Uri>? uriValidator = null)
    {
        NotBeNullOrWhitespace()
        .NotContainWhitespace()
        .MatchRegex(new(@"^[A-Za-z0-9-._~:/?#@\[\]!$&'()*+,;=%]*$"));

        if (!Uri.TryCreate(Value, kind, out var uriResult))
            AddError($"Value must be a valid {kind} URL");
        else
            uriValidator?.Invoke(uriResult);
        return this;
    }

    /// <summary>
    /// must be an absolute url without query parameters
    /// </summary>
    /// <returns></returns>
    public StringValidator BeBaseUrl()
        => BeUrl(UriKind.Absolute)
            .NotContain('?');

    /// <summary>
    /// Checks whether the value is an absolute URL and has one of the specified schemes.
    /// </summary>
    /// <param name="scheme">The supported schemes.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeUrlWithScheme(params string[] scheme)
        => BeUrl(UriKind.Absolute, uri =>
        {
            if (uri is null)
                AddError("Uri must not be null");
            else if (scheme.Contains(uri.Scheme) == false)
                AddError($"Url has scheme {uri.Scheme} but must have one of the following: " + string.Join(", ", scheme));
        });

    /// <summary>
    /// Validates that the value is a valid HTTP URL.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public StringValidator BeRelativeUrl()
        => BeUrl(UriKind.Relative);

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
            AddError($"value must match regex {expression}");
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
            AddError($"the value must be at least {length} characters long but has a length of {Value.Length}");
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
            AddError($"the value must be at most {length} characters long but has a length of {Value.Length}");
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
            AddError($"the value must be exactly {length} characters long but has a length of {Value?.Length}");
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
            AddError($"the value must end with '{value}'");
        return this;
    }
    /// <summary>
    /// Validates that the value ends with the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator EndWith(char value)
    {
        if (Value?.EndsWith(value) != true)
            AddError($"the value must end with '{value}'");
        return this;
    }
    /// <summary>
    /// Validates that the value ends with the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotEndWith(string value)
    {
        if (Value?.EndsWith(value) != false)
            AddError($"the value must not end with '{value}'");
        return this;
    }
    /// <summary>
    /// Validates that the value ends with the specified value.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>The string validator instance.</returns>
    public StringValidator NotEndWith(char value)
    {
        if (Value?.EndsWith(value) != false)
            AddError($"the value must not end with '{value}'");
        return this;
    }
}
