using FluentAssertions;
using Snapshooter.Xunit;
using Xunit;

namespace JLib.Helper.Tests;

public class UnitTest1
{
    public class SubClassA<T>
    {
        public class SubClassB<T2>
        {

        }
    }

    public class OtherClass
    {
        public class OtherSubClass{}
    }
    public class OtherGenericClass<T> { }
    [Fact]
    public void Test1()
    {
        typeof(SubClassA<OtherGenericClass<OtherClass>>
            .SubClassB<OtherGenericClass<OtherClass>>
            ).FullName().Should().Be(
            "UnitTest1.SubClassA<UnitTest1.OtherGenericClass<UnitTest1.OtherClass>, UnitTest1.OtherGenericClass<UnitTest1.OtherClass>>");
    }
    [Fact]
    public void Test2()
    {
        typeof(SubClassA<int>)
            .FullName().Should().Be("UnitTest1.SubClassA<Int32>");
    }
    [Fact]
    public void Test3()
    {
        typeof(OtherClass.OtherSubClass)
            .FullName().Should().Be("UnitTest1.OtherClass.OtherSubClass");
    }
}