using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JLib.ValueTypes.Tests;
public class FactoryTests
{
    public abstract record UserIdentity(string Value) : StringValueType(Value)
    {
        [Factory]
        private static Expression<Func<string, UserIdentity>> Create { get; }
            = value => value.Contains("@") 
                ? new UserEmailAddress(value)
                : new UserName(value);
    }

    public record UserName(string Value) : UserIdentity(Value)
    {
        [Validation, Factory]
        private static void Validate(IValidationContext<string?> must)
            => must.NotContain("@");
    }

    public record UserEmailAddress(string Value) : UserIdentity(Value)
    {
        [Validation]
        private static void Validate(IValidationContext<string?> must)
            => must.Contain("@");
    }

}
