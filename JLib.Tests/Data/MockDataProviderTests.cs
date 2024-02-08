﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using JLib.Data;
using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using JLib.Reflection.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace JLib.Tests.Data;
public class InMemoryDataProviderTests
{
    private readonly IDataProviderRw<TestEntity> _dataProvider;

    [TvtFactoryAttribute.Implements(typeof(ITestEntity)), TvtFactoryAttribute.Priority(NextPriority - 1000)]
    public record TestEntityType(Type Value) : EntityType(Value);
    public interface ITestEntity : IEntity
    {

    }
    public class TestEntity : ITestEntity
    {
        public Guid Id { get; set; }

    }
    public InMemoryDataProviderTests()
    {
        IExceptionManager exceptions = new ExceptionManager("Test");
        IServiceCollection services = new ServiceCollection()
            .AddTypeCache(out var typeCache, exceptions,
                JLibTypePackage.Instance, TypePackage.GetNested<InMemoryDataProviderTests>())
            .AddDataProvider<TestEntityType, InMemoryDataProvider<ITestEntity>, ITestEntity>(typeCache, null, null, null, exceptions);
        ;
        exceptions.ThrowIfNotEmpty();
        var provider = services.BuildServiceProvider();
        _dataProvider = provider.GetRequiredService<IDataProviderRw<TestEntity>>();
    }

    [Fact]
    public void AddSingle()
    {
        var e = new TestEntity();
        _dataProvider.Add(e);
        _dataProvider.Get().Should().ContainSingle(t => t == e);
    }
}
