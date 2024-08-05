using FluentAssertions;
using Xunit;

namespace JLib.ValueTypes.Tests;

/// <summary>
/// tests whether the validator is executed correctly in all possible derivation scenarios
/// </summary>
public class ValidatorTests
{

    public record DemoNumericString(string Value) : StringValueType(Value)
    {
        [Validation]
        private static void Validate(IValidationContext<string?> must)
            => must.BeNumeric();
    }
    public record DemoAsciiString(string Value) : StringValueType(Value)
    {
        [Validation]
        private static void Validate(ValidationContext<string?> must)
            => must.BeAscii();
    }

    public record DemoAlphanumericString(string Value) : DemoAsciiString(Value)
    {
        [Validation]
        private static void Validate(ValidationContext<string?> must)
            => must.BeAlphanumeric();
    }

    [Fact]
    public void T1_1()
        => ValueType.GetErrors<DemoAsciiString, string>(null!)
            .GetException().Should().NotBeNull();
    [Fact]
    public void T1_2()
        => ValueType.GetErrors<DemoAsciiString, string>("abc")
            .GetException().Should().BeNull();
    [Fact]
    public void T2_1()
        => ValueType.GetErrors<DemoAsciiString, string>("!")
            .GetException().Should().BeNull();
    [Fact]
    public void T2_2()
        => ValueType.GetErrors<DemoAlphanumericString, string>("!")
            .GetException().Should().NotBeNull();

    [Fact]
    public void T2_3()
        => ValueType.GetErrors<DemoAlphanumericString, string>("\u0868")
            .GetException().Should().NotBeNull();
    [Fact]
    public void T2_4()
        => ValueType.GetErrors<DemoAsciiString, string>("\u0868")
            .GetException().Should().NotBeNull();

    [Fact]
    public void T3_0()
        => ValueType.GetErrors<DemoNumericString, string>("123")
            .HasErrors().Should().BeFalse();
    [Fact]
    public void T3_1()
        => ValueType.GetErrors<DemoNumericString, string>("123a")
            .HasErrors().Should().BeTrue();
}