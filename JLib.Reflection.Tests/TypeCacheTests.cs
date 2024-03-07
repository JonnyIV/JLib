using System.Collections.Immutable;
using FluentAssertions;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Testing;
using Microsoft.Extensions.Logging;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JLib.Reflection.Tests;
/// <summary>
/// tests for multiple constellations of inputTypes for the typeCache<br/>
/// to add a testcase, create a subclass with the expected types.<br/>
/// the result will be validated via snapshot<br/>
/// to run only a single test for debug purposes, add the <see cref="FocusTestAttribute"/> to the container class
/// </summary>
public class TypeCacheTests : IDisposable
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<IDisposable> _disposables = new();

    public static class TypeValueTypeWithoutFactoryAttribute
    {
        public record InvalidTypeValueType(Type Value) : TypeValueType(Value);
    }
    public static class InconclusiveFactory
    {
        public interface IDemoTypeA { }
        public interface IDemoTypeB { }

        [TvtFactoryAttribute.Implements(typeof(IDemoTypeA))]
        public record DemoTypeValueTypeA(Type Value) : TypeValueType(Value);
        [TvtFactoryAttribute.Implements(typeof(IDemoTypeB))]
        public record DemoTypeValueTypeB(Type Value) : TypeValueType(Value);
        public class InconclusiveType : IDemoTypeA, IDemoTypeB { }
    }

    public TypeCacheTests(ITestOutputHelper testOutputHelper)
    {
        _loggerFactory = new LoggerFactory().AddXunit(testOutputHelper);
        _disposables.Add(_loggerFactory);
    }

    /// <summary>
    /// a parent class with this attribute will be the only test executed and cause the <see cref="TypeCacheTests.FocusNotSet"/> test to fail
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    // ReSharper disable once ClassNeverInstantiated.Local
    private class FocusTestAttribute : Attribute { }
    public static TheoryData<Type> GetTestTypes()
    {

        var anyFocus = typeof(TypeCacheTests).GetNestedTypes().Any(t => t.HasCustomAttribute<FocusTestAttribute>());

        var d = new TheoryData<Type>();
        foreach (var type in typeof(TypeCacheTests)
                     .GetNestedTypes()
                     .Where(t => !anyFocus || t.HasCustomAttribute<FocusTestAttribute>())
                )
        {
            d.Add(type);
        }
        return d;
    }

    [Theory]
    [MemberData(nameof(GetTestTypes))]
    public void Test(Type sut)
    {
        var testName = sut.Name;
        var package = TypePackage.GetNested(sut);
        var exceptions = new ExceptionBuilder(testName);
        var cache = new TypeCache(package, exceptions, _loggerFactory);
        new Dictionary<string, object?>()
            {
                { "TestName",testName },
                {
                    "Environment",
                    new Dictionary<string, object>()
                    {
                        {
                            "Types",
                            sut.GetNestedTypes().ToImmutableSortedDictionary(t=>t.Name,
                                t => new Dictionary<string, object?>()
                                    {
                                        { "Kind", t.IsClass?"Class":t.IsValueType?"struct":t.IsInterface?"Interface":"other"},
                                        { "Attributes", t.GetCustomAttributes<Attribute>().Select(a=>a.GetType().FullName()).OrderBy(x=>x) },
                                        { "ImplementedInterfaces", t.GetInterfaces().Select(t2=>t2.FullName()).OrderBy(i=>i)},
                                    }
                                )
                        }
                    }
                },
                {
                    "Exceptions",
                    exceptions?.GetException()?.PrepareSnapshot()
                },
                {
                    "CachedTypes",
                    cache.All<ITypeValueType>().PrepareSnapshot()
                },
                {
                    "KnownTypeValueTypes",
                    cache.KnownTypeValueTypes.Select(t=>t.Name).OrderBy(x=>x)
                }
            }.MatchSnapshot(new SnapshotNameExtension(testName));
    }

    [Fact]
    public void FocusNotSet()
    {
        typeof(TypeCacheTests)
            .GetNestedTypes()
            .Should()
            .OnlyContain(t => !t.HasCustomAttribute<FocusTestAttribute>(true), "Some tests have the Focus attribute set");
    }

    public void Dispose() => _disposables.DisposeAll();
}
