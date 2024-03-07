using Snapshooter.Xunit;
using Xunit;

namespace JLib.Exceptions.Tests;

public class JLibAggregateExceptionTests
{
    [Fact]
    public void MessageGenerator()
    {
        var ex = new ExceptionBuilder("test");
        ex.Add(new Exception("ex"));
        ex.Add(new Exception("ex2", new("inner", new("inner2"))));
        var child = ex.CreateChild("child");
        child.Add(new Exception("child ex"));
        child.Add(new Exception("child ex 2", new("child ex inner")));
        child.CreateChild("inner3").Add(new Exception("sub child" + Environment.NewLine + "second line"));
        ex.Add(new Exception("last"));
        ex.CreateChild("last child").Add(new Exception("last child exception"));

        ex.GetException()?.Message.MatchSnapshot();
    }
}