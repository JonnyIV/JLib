using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using JLib.DataGeneration.Abstractions;
using JLib.Helper;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace JLib.DataGeneration.Tests;
public class TestingIdGeneratorTests : IDisposable
{
    class Nested<T>
    {
        private readonly TestingIdGenerator _idGenerator;

        public Nested(TestingIdGenerator idGenerator)
        {
            _idGenerator = idGenerator;
        }

        public Guid CreateId<T2, T3>()
            => CreateId<T2>(1);
        public Guid CreateId<T2>(int stackTraceFrameIndex)
            => _idGenerator.CreateGuid(stackTraceFrameIndex);
        public Guid CreateId<T2>(string stringParam)
            => CreateId<T2>(1);
    }

    private readonly List<IDisposable> _disposables = new();
    private readonly TestingIdGenerator _idGenerator;
    private readonly IServiceProvider _provider;


    public TestingIdGeneratorTests()
    {
        var provider = new ServiceCollection()
            .AddAutoMapper(cfg => { })
            .AddTestingIdGenerator()
            .AddIdRegistry("JLib.DataGeneration.Tests")
            .AddSingleton(typeof(Nested<>))
            .BuildServiceProvider();
        _disposables.Add(provider);
        _idGenerator = provider.GetRequiredService<TestingIdGenerator>();
        _provider = provider;

    }
    [Fact]
    public void CreateGuid_ShouldReturnNewGuid()
    {
        _idGenerator.CreateGuid().MatchSnapshot();
    }
    [Fact]
    public void CreateGuid_ShouldReturnNewGuid2()
    {
        _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0)
            .MatchSnapshot();
    }
    [Fact]
    public void CreateGuid_ShouldReturnNewGuid3()
    {
        var a = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0);
        var b = _provider.GetRequiredService<Nested<int>>()
                .CreateId<double>(0);
        new[] { a, b }.MatchSnapshot();
        a.Should().NotBe(b);
    }
    [Fact]
    public void CreateGuid_ShouldReturnNewGuid4()
    {
        var a = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0);
        var b = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0);

        _idGenerator.SetIdScope(new("otherScope"));

        var c = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0);

        new[] { a, b, c }.MatchSnapshot();
        a.Should().NotBe(b).And.NotBe(c);
    }
    [Fact]
    public void CreateGuid_ShouldReturnNewGuid5()
    {
        var a = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double>(0);
        var b = _provider.GetRequiredService<Nested<int>>()
            .CreateId<double, int>();
        new[] { a, b }.MatchSnapshot();
        a.Should().NotBe(b);
    }

    public void Dispose()
        => _disposables.DisposeAll();
}
