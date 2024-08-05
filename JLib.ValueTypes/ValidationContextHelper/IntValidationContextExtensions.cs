namespace JLib.ValueTypes;

/// <summary>
/// validates values of type <see cref="int"/>
/// </summary>
public static class IntValidationContextExtensions
{
    /// <summary>
    /// Validates that the value is greater than or equal to the specified minimum.
    /// </summary>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BeGreaterThanOrEqualTo(this ValidationContext<int> context,int minimum)
    {
        if (context.Value < minimum)
            context.Validate($"value must be greater than or equal to {minimum}");
        return context;
    }

    /// <summary>
    /// Validates that the value is less than or equal to the specified maximum.
    /// </summary>
    /// <param name="maximum">The maximum value.</param>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BeLessThanOrEqualTo(this ValidationContext<int> context, int maximum)
    {
        if (context.Value > maximum)
            context.Validate($"value must be less than or equal to {maximum}");
        return context;
    }

    /// <summary>
    /// Validates that the value is greater than the specified minimum.
    /// </summary>
    /// <param name="minimum">The minimum value.</param>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BeGreaterThan(this ValidationContext<int> context, int minimum)
    {
        if (context.Value <= minimum)
            context.Validate($"value must be greater than {minimum}");
        return context;
    }

    /// <summary>
    /// Validates that the value is less than the specified maximum.
    /// </summary>
    /// <param name="maximum">The maximum value.</param>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BeLessThan(this ValidationContext<int> context, int maximum)
    {
        if (context.Value >= maximum)
            context.Validate($"value must be less than {maximum}");
        return context;
    }

    /// <summary>
    /// Validates that the value is not negative (greater than or equal to 0).
    /// </summary>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> NotBeNegative(this ValidationContext<int> context)
        => context.BeGreaterThanOrEqualTo(0);

    /// <summary>
    /// Validates that the value is positive (greater than 0).
    /// </summary>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BePositive(this ValidationContext<int> context)
        => context.BeGreaterThan(0);

    /// <summary>
    /// Validates that the value is within the specified range (between and including the minimum and maximum).
    /// </summary>
    /// <param name="context">The current Validation Context</param>
    /// <param name="minimum">The minimum value.</param>
    /// <param name="maximum">The maximum value.</param>
    /// <returns>The current instance of the <see cref="ValidationContext{T}"/> class.</returns>
    public static ValidationContext<int> BeInBounds(this ValidationContext<int> context, int minimum, int maximum)
        => context.BeGreaterThanOrEqualTo(minimum)
            .BeLessThanOrEqualTo(maximum);
}
