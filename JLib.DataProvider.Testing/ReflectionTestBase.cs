using System.Runtime.CompilerServices;
using FluentAssertions;
using JLib.Cqrs;
using JLib.DependencyInjection;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using JLib.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snapshooter.Xunit;
using Xunit.Abstractions;

namespace JLib.DataProvider.Testing;

public interface ITestEntity : ICommandEntity
{

}


public abstract class ReflectionTestBase
{
    private readonly ITestOutputHelper _testOutput;
    private readonly ITypePackage _typePackage;
    private readonly ExceptionBuilder _exceptions;
    private readonly ILoggerFactory _loggerFactory;

    protected ReflectionTestBase(ITestOutputHelper testOutput, ITypePackage typePackage)
    {
        _exceptions = new ExceptionBuilder(GetType().FullName());
        _testOutput = testOutput;
        _typePackage = typePackage;
        _loggerFactory = new LoggerFactory()
            .AddXunit(testOutput);
    }

    protected virtual void AddServices(IServiceCollection services, ITypeCache cache, ExceptionBuilder exceptions)
    {

    }
    protected void Test(string[] expectedBehavior, IReadOnlyCollection<Type> includedTypes,
        Action<IServiceCollection, ITypeCache, ILoggerFactory, ExceptionBuilder> serviceFactory, bool expectException = true,
        bool testCache = true,
        bool testServices = true,
        [CallerMemberName] string testName = "")
        => Test(new(testName, expectedBehavior, includedTypes, serviceFactory, expectException, testCache, testServices));
    protected virtual void Test(ReflectionTestOptions options)
    {
        _testOutput.WriteLine("TestName: {0}", options.TestName);
        var (testName, expectedBehavior, content, serviceFactory, expectException, testCache, testServices) = options;

        IServiceCollection services = new ServiceCollection()
            .AddTypeCache(
                out var cache, _exceptions, _loggerFactory, TypePackage.Get(content), JLibDataProviderTp.Instance, JLibCqrsTp.Instance, _typePackage);

        // group by namespace, then by typeValueType and use json objects for the grouping
        object cacheValidator;
        try
        {
            cacheValidator = cache.All<ITypeValueType>()
                .Where(tvt => tvt.Value.Assembly != typeof(ITypeCache).Assembly)
                .PrepareSnapshot();
        }
        catch (Exception e)
        {
            cacheValidator = e.PrepareSnapshot() as object ?? "evaluation failed";
        }

        AddServices(services, cache, _exceptions.CreateChild(nameof(AddServices)));
        serviceFactory(services, cache, _loggerFactory, _exceptions.CreateChild("Service Factory"));

        object serviceValidator;
        try
        {
            serviceValidator = services.PrepareSnapshot();
        }
        catch (Exception e)
        {
            serviceValidator = e.PrepareSnapshot() as object ?? "evaluation failed";
        }

        var maxDescriptionLength = expectedBehavior.Max(x => x.Length);
        new Dictionary<string, object?>
        {
            {
                "Parameters",
                new Dictionary<string,object>()
                {
                    {
                        "TestName",
                        testName
                    },
                    {
                        "ExpectedBehavior",
                        expectedBehavior.Select(d=>"  "+d.PadRight(maxDescriptionLength+4))
                    },
                    {
                        "includedTypes",
                        content.Select(c=>c.FullName(true))
                    }
                }
            },
            {
                "exception",
                _exceptions.GetException().PrepareSnapshot()
            },
            {
                "cache",
                testCache? cacheValidator:"disabled"
            },
            {
                "services",
                testServices? serviceValidator:"disabled"
            }
        }.MatchSnapshot(b=>b.IgnoreField("cache"));
        if (expectException)
            _exceptions.GetException().Should().NotBeNull();
        else
            _exceptions.GetException().Should().BeNull();
    }
}