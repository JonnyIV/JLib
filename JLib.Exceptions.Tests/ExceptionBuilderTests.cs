using FluentAssertions;
using Xunit;

namespace JLib.Exceptions.Tests;

public class ExceptionBuilderTests
{
    private readonly ExceptionBuilder _builder = new("test");
    [Fact]
    public void Test1()
        => _builder.HasErrors().Should().BeFalse();

    [Fact]
    public void Test2()
    {
        _builder.CreateChild("child");
        _builder.HasErrors().Should().BeFalse();
    }

    [Fact]
    public void Test3()
    {
        var child = _builder.CreateChild("child");
        child.Add(new Exception("error"));
        _builder.HasErrors().Should().BeTrue();
    }
    [Fact]
    public void Test4()
    {
        var child = _builder.CreateChild("child");
        _builder.Add(new Exception("error"));
        _builder.HasErrors().Should().BeTrue();
    }
}