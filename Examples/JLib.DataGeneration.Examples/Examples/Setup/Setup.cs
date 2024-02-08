﻿using FluentAssertions;
using JLib.DataGeneration.Examples.Setup.Models;
using JLib.DataGeneration.Examples.Setup.SystemUnderTest;
using JLib.Exceptions;
using JLib.Helper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JLib.DataGeneration.Examples.Setup;

public sealed class Setup : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly ShoppingServiceMock _shoppingService;

    public Setup()
    {
        // collects exceptions to be thrown later as aggregate
        var exceptions = new ExceptionManager("setup");

        var services = new ServiceCollection()
            // executes and caches reflection on all given types
            .AddTypeCache(out var typeCache, exceptions, JLibDataGenerationExamplesTypePackage.Instance)
            // add all type packages
            .AddDataPackages(typeCache)
            // add all automapper profiles (required profile: JLib.AutoMapper.ValueTypeProfile)
            .AddAutoMapper(m => m.AddProfiles(typeCache))
            // the service we want to test
            .AddSingleton<ShoppingServiceMock>()
            // provides the same instance as above when you inject the interface
            .AddAlias<IShoppingService, ShoppingServiceMock>(ServiceLifetime.Singleton);

        _provider = services.BuildServiceProvider();
        _shoppingService = _provider.GetRequiredService<ShoppingServiceMock>();

        // if something went wrong during the setup, this will throw an aggregate exception containing all errors
        exceptions.ThrowIfNotEmpty();
    }

    [Fact]
    public void Test()
    {
        // define which packages should be loaded and apply their data
        // references will be resolved automatically
        _provider.IncludeDataPackages<ArticleDp>();

        _shoppingService.Articles.Should().HaveCount(2);
    }

    public void Dispose() => _provider.Dispose();
}

public sealed class ArticleDp : DataPackage
{
    // the properties will be filled when the base constructor is called
    // they must be public and have the get and init accessors.
    // valid types are 
    // - int, Guid, string
    // - any derivations of IntValueType, GuidValueType and StringValueType 
    //     they can be found in the JLib package under JLib.ValueTypes
    public ArticleId FirstArticle { get; init; } = default!;
    public ArticleId SecondArticle { get; init; } = default!;

    public ArticleDp(
        // required for base class
        IDataPackageManager packageManager,
        // to add data to services, inject them directly into your constructor and add data using the id-properties 
        ShoppingServiceMock shoppingService
        ) : base(packageManager)
    {
        shoppingService.AddArticles(
            new(nameof(FirstArticle), 10)
            {
                Id = FirstArticle
            },
            new(nameof(SecondArticle), 20.20)
            {
                Id = SecondArticle
            }
        );
    }
}