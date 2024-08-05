using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JLib.ValueTypes;
/// <summary>
/// Contains validation methods for ValidationContext&lt;char&gt;.
/// </summary>
/// <seealso cref="ValidationContext{TValue}"/>
public static class CharValidationContextExtensions
{
    /// <summary>
    /// Validates if the character is a digit.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation context.</returns>
    public static ValidationContext<char> BeDigit(this ValidationContext<char> context)
    {
        if (!char.IsDigit(context.Value))
            context.Validate("must be a digit");
        return context;
    }

    /// <summary>
    /// Validates if the character is a letter.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation context.</returns>
    public static ValidationContext<char> BeLetter(this ValidationContext<char> context)
    {
        if (!char.IsLetter(context.Value))
            context.Validate("must be a letter");
        return context;
    }
    /// <summary>
    /// Validates if the character is ascii.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation context.</returns>
    public static ValidationContext<char> BeAscii(this ValidationContext<char> context)
    {
        if (!char.IsAscii(context.Value))
            context.Validate("must be ascii");
        return context;
    }
    /// <summary>
    /// Validates if the character is ascii.
    /// </summary>
    /// <param name="context">The validation context.</param>
    /// <returns>The validation context.</returns>
    public static ValidationContext<char> BeAsciiLetter(this ValidationContext<char> context)
    {
        // this should not be used, since it is not supported by .net 6.
        // a compiler switch should not be used, since this might introduce a breaking change between .net versions.
        //   if (!char.IsAsciiLetter(context.Value))
        //       context.Validate("must be an ascii letter");
        context.BeAscii();
        context.BeLetter();
        return context;
    }
}
