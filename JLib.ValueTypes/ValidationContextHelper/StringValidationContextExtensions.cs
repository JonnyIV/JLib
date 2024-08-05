using System.Text.RegularExpressions;
using JLib.Helper;

namespace JLib.ValueTypes;

/// <summary>
/// Represents a validator for string Values.
/// </summary>
public static class StringValidationContextExtensions
{
    /// <summary>
    /// Validates that the context.Value is not null.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotBeNull(this IValidationContext<string?> context)
    {
        if (context.Value is null)
            context.Validate("context.Value must not be null");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value is not null or empty.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotBeNullOrEmpty(this IValidationContext<string?> context)
    {
        if (context.Value.IsNullOrEmpty())
            context.Validate("context.Value must neither be null nor empty");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value is one of the specified valid context.Values.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="validValues">The collection of valid context.Values.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeOneOf(this IValidationContext<string?> context, IReadOnlyCollection<string> validValues)
    {
        if (!validValues.Contains(context.Value))
            context.Validate("context.Value must be one of the following: " + string.Join(", ", validValues));
        return context;
    }

    /// <summary>
    /// Validates that the context.Value is alphanumeric.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeAlphanumeric(this IValidationContext<string?> context)
        => context.SatisfyCondition(char.IsLetterOrDigit, "context.Value must be alphanumeric");

    /// <summary>
    /// Validates that the context.Value satisfies the specified condition.
    /// </summary>
    /// <param name="validator">The condition to satisfy.</param>
    /// <param name="name">The name of the condition.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> SatisfyCondition(this IValidationContext<string?> context, Func<char, bool> validator, string name)
    {
        if (context.Value is null)
        {
            context.Validate(name + " failed: string is null");
            return context;
        }

        var errorIndices = context.Value
            .AddIndex()
            .Where(x => !validator(x.Item1))
            .Select(x => x.Item2)
            .ToArray();
        foreach (var errorIndex in errorIndices)
        {
            int minLength = 10;

            int start = Math.Max(0, errorIndex - minLength / 2);
            int end = Math.Min(context.Value.Length, errorIndex + minLength / 2);

            string errorPart = context.Value.Substring(start, end - start);
            context.Validate($"{name} failed at index {errorIndex}. \"{errorPart}\"");
        }

        if (context.Value.All(validator))
            return context;

        context.Validate(name + " failed");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value is not null or whitespace.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotBeNullOrWhitespace(this IValidationContext<string?> context)
    {
        if (context.Value.IsNullOrEmpty())
            context.Validate("context.Value must neither be null nor whitespace");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value starts with the specified prefix.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> StartWith(this IValidationContext<string?> context, string prefix)
    {
        if (context.Value == null || !context.Value.StartsWith(prefix))
            context.Validate($"context.Value must start with {prefix}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value does not start with the specified prefix.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotStartWith(this IValidationContext<string?> context, string prefix)
    {
        if (context.Value == null || context.Value.StartsWith(prefix))
            context.Validate($"context.Value must not start with {prefix}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value starts with the specified prefix.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> StartWith(this IValidationContext<string?> context, char prefix)
    {
        if (context.Value == null || !context.Value.StartsWith(prefix))
            context.Validate($"context.Value must start with {prefix}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value does not start with the specified prefix.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="prefix">The prefix to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotStartWith(this IValidationContext<string?> context, char prefix)
    {
        if (context.Value == null || context.Value.StartsWith(prefix))
            context.Validate($"context.Value must not start with {prefix}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> Contain(this IValidationContext<string?> context, string value)
    {
        if (context.Value != null && context.Value.Contains(value) == false)
            context.Validate($"context.Value must contain {value}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> Contain(this IValidationContext<string?> context, char value)
    {
        if (context.Value != null && context.Value.Contains(value) == false)
            context.Validate($"context.Value must contain {value}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotContain(this IValidationContext<string?> context, string value)
    {
        if (context.Value != null && context.Value.Contains(value))
            context.Validate($"context.Value must not contain {value}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="values">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotContain(this IValidationContext<string?> context, IReadOnlyCollection<string> values)
    {
        foreach (var value in values)
            context.NotContain(value);
        return context;
    }

    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotContain(this IValidationContext<string?> context, char value)
    {
        if (context.Value != null && context.Value.Contains(value))
            context.Validate($"context.Value must not contain {value}");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value does not contain the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="values">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotContain(this IValidationContext<string?> context, IReadOnlyCollection<char> values)
    {
        foreach (var value in values)
            context.NotContain(value);
        return context;
    }

    /// <summary>
    /// Validates that the context.Value is a URL of the specified kind.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="kind">The kind of URL to validate.</param>
    /// <param name="uriValidator">An optional validator for the created Uri object.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeUrl(this IValidationContext<string?> context, UriKind kind, Action<Uri>? uriValidator = null)
    {
        context.NotBeNullOrWhitespace()
            .NotContainWhitespace()
            .MatchRegex(new(@"^[A-Za-z0-9-._~:/?#@\[\]!$&'()*+,;=%]*$"));

        if (!Uri.TryCreate(context.Value, kind, out var uriResult))
            context.Validate($"context.Value must be a valid {kind} URL");
        else
            uriValidator?.Invoke(uriResult);
        return context;
    }

    /// <summary>
    /// must be an absolute url without query parameters
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns></returns>
    public static IValidationContext<string?> BeBaseUrl(this IValidationContext<string?> context)
        => context.BeUrl(UriKind.Absolute)
            .NotContain('?');

    /// <summary>
    /// Checks whether the context.Value is an absolute URL and has one of the specified schemes.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="scheme">The supported schemes.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeUrlWithScheme(this IValidationContext<string?> context, params string[] scheme)
        => context.BeUrl(UriKind.Absolute, uri =>
        {
            if (uri is null)
                context.Validate("Uri must not be null");
            else if (scheme.Contains(uri.Scheme) == false)
                context.Validate($"Url has scheme {uri.Scheme} but must have one of the following: " + string.Join(", ", scheme));
        });

    /// <summary>
    /// Validates that the context.Value is a valid HTTP URL.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeRelativeUrl(this IValidationContext<string?> context)
        => context.BeUrl(UriKind.Relative);

    /// <summary>
    /// Validates that the context.Value matches the specified regular expression.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="expression">The regular expression to match.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> MatchRegex(this IValidationContext<string?> context, Regex expression)
    {
        context.NotBeNull();
        if (context.Value is null)
            return context;
        if (expression.IsMatch(context.Value) == false)
            context.Validate($"context.Value must match regex {expression}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value contains only ASCII characters.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeAscii(this IValidationContext<string?> context)
        => context.SatisfyCondition(char.IsAscii, nameof(BeAscii));

    /// <summary>
    /// Validates that the context.Value contains only alphanumeric characters.
    /// </summary>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> OnlyContainAlphaNumericCharacters(this IValidationContext<string?> context)
        => context.SatisfyCondition(char.IsLetterOrDigit, nameof(OnlyContainAlphaNumericCharacters));

    /// <summary>
    /// Validates that the context.Value does not contain whitespace characters.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotContainWhitespace(this IValidationContext<string?> context)
    {
        return context.Value is null
            ? context
            : context.SatisfyCondition(c => !char.IsWhiteSpace(c), nameof(NotContainWhitespace));
    }

    /// <summary>
    /// Validates that the context.Value contains only numeric characters.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeNumeric(this IValidationContext<string?> context)
        => context.SatisfyCondition(char.IsNumber, nameof(BeNumeric));

    /// <summary>
    /// Validates that the context.Value has a minimum length.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="length">The minimum length.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> MinimumLength(this IValidationContext<string?> context, int length)
    {
        context.NotBeNull();
        if (context.Value?.Length < length)
            context.Validate($"the context.Value must be at least {length} characters long but has a length of {context.Value.Length}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value has a maximum length.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="length">The maximum length.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> MaximumLength(this IValidationContext<string?> context, int length)
    {
        context.NotBeNull();
        if (context.Value?.Length > length)
            context.Validate($"the context.Value must be at most {length} characters long but has a length of {context.Value.Length}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value has a specific length.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="length">The expected length.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> BeOfLength(this IValidationContext<string?> context, int length)
    {
        if (context.Value?.Length != length)
            context.Validate($"the context.Value must be exactly {length} characters long but has a length of {context.Value?.Length}");
        return context;
    }

    /// <summary>
    /// Validates that the context.Value ends with the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> EndWith(this IValidationContext<string?> context, string value)
    {
        if (context.Value?.EndsWith(value) != true)
            context.Validate($"the context.Value must end with '{value}'");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value ends with the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> EndWith(this IValidationContext<string?> context, char value)
    {
        if (context.Value?.EndsWith(value) != true)
            context.Validate($"the context.Value must end with '{value}'");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value ends with the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotEndWith(this IValidationContext<string?> context, string value)
    {
        if (context.Value?.EndsWith(value) != false)
            context.Validate($"the context.Value must not end with '{value}'");
        return context;
    }
    /// <summary>
    /// Validates that the context.Value ends with the specified context.Value.
    /// </summary>
    /// <param name="context">the context which will be validated.</param>
    /// <param name="value">The context.Value to check.</param>
    /// <returns>The string validator instance.</returns>
    public static IValidationContext<string?> NotEndWith(this IValidationContext<string?> context, char value)
    {
        if (context.Value?.EndsWith(value) != false)
            context.Validate($"the context.Value must not end with '{value}'");
        return context;
    }
}