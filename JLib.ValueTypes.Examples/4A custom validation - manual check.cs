using FluentAssertions;
using JLib.Exceptions;
using JLib.ValueTypes;
using Xunit;
using ValueType = JLib.ValueTypes.ValueType;

namespace Examples;
// ReSharper disable once InconsistentNaming
public class Custom_Validation_using_manual_validation
{
    public record EmailAddress(string Value) : StringValueType(Value)
    {
        /// <summary>
        /// registers this method as validator for this value type.
        /// it will be called when this value type or any derivation of it is created.
        /// </summary>
        [Validation]
        private static void Validate(ValidationContext<string?> validator)
        {
            if (validator.Value?.Contains('.') == false)
                validator.Validate("email contains no .");
            if (validator.Value?.Contains('@') == false)
                validator.Validate("email contains no @");
        }
    }

    [Fact]
    public void ValidValue()
    {
        var sut = () => new EmailAddress("my@example.de");
        sut.Should().NotThrow();
    }

    [Fact]
    public void InValidValue()
    {
        var sut = () => new EmailAddress("not a mail");
        sut.Should().Throw<AggregateException>();
    }

}
