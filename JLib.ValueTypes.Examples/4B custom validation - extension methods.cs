﻿using FluentAssertions;
using JLib.Exceptions;
using JLib.ValueTypes;
using Xunit;
using ValueType = JLib.ValueTypes.ValueType;

// ReSharper disable all

namespace Examples.ExtensionMethods
{
    /// <summary>
    /// this method is recommended if you reuse the same validation logic in multiple places and only use the built-in validator
    /// </summary>
    public static class Custom_Validation_using_Extension_methods_Helper
    {
        /// <summary>
        /// recommended, when you want to add generic validation logic to the existing validator.
        /// </summary>
        /// <param name="validator"></param>
        /// <returns></returns>
        public static StringValidator ContainAt(this StringValidator validator)
        {
            var testString = "@";
            if (!validator.Value?.Contains(testString) != true)
                validator.AddError($"'{validator.Value}' must contain '{testString}'");
            return validator;
        }

        /// <summary>
        /// this method is recommended if you reuse the same validation logic in multiple places and only use the built-in validator
        /// </summary>
        public static ValueValidator<string?> ContainDot(this ValueValidator<string?> validator)
        {
            var testString = ".";
            if (validator.Value?.Contains(testString) != true)
                validator.AddError($"'{validator.Value}' must contain with '{testString}'");
            return validator;
        }

        public static IValueValidator<string?> NotEndWithDot(this IValueValidator<string?> validator)
        {
            var testString = ".";
            if (validator.Value?.EndsWith(testString) != true)
                validator.AddError($"'{validator.Value}' must not end with '{testString}'");
            return validator;
        }
    }

    // ReSharper disable once InconsistentNaming
    public class Custom_Validation_using_Extension_methods
    {

        public record EmailAddress(string Value) : StringValueType(Value)
        {
            /// <summary>
            /// registers this method as validator for this value type.
            /// it will be called when this value type or any derivation of it is created.
            /// </summary>
            [JLib.ValueTypes.Validation]
            private static void Validate(StringValidator must)
                => must.ContainAt().ContainDot().NotEndWithDot();
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
            var sut = () => new EmailAddress("invalid mail");
            sut.Should().Throw<AggregateException>();
        }

        [Fact]
        public void ExceptionContent()
        {
            ValueType.TryCreate<EmailAddress, string>("invalid mail", out var errors);
            errors.GetException()?.GetHierarchyInfoJson().Should().Be(@"{
  ""Message"": ""Examples.ExtensionMethods.Custom_Validation_using_Extension_methods.EmailAddress"",
  ""JLibAggregateException"": {
    ""Message"": ""Examples.ExtensionMethods.Custom_Validation_using_Extension_methods.EmailAddress validation failed: \u0027invalid mail\u0027 is not a valid Value."",
    ""JLibAggregateException"": {
      ""Message"": ""Value Validation Failed"",
      ""2 ValidationException"": [
        ""\u0027invalid mail\u0027 must contain with \u0027.\u0027"",
        ""\u0027invalid mail\u0027 must not end with \u0027.\u0027""
      ]
    }
  }
}");
        }

    }
}




