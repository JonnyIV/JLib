

















// disabled since the mapping is being reworked

















//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using AutoMapper;
//using JLib.Data;
//using JLib.FactoryAttributes;
//using JLib.Helper;
//using JLib.HotChocolate;
//using JLib.Tests.Reflection.ServiceCollection.AddDataProvider;
//using Moq;
//using Xunit.Abstractions;

//namespace JLib.Tests.Reflection.ServiceCollection.AddMapDataProvider;

//public class AddMapDataProviderTests : ReflectionTestBase
//{
//    public class TestArguments : ReflectionTestArguments
//    {
//        protected override string Filter { get; } = "";

//        protected override IEnumerable<ReflectionTestOptions> Options { get; } = new ReflectionTestOptions[]
//        {
//            new(
//                "srv_all_ce",
//                new []
//                {
//                    "input: xrm, ce",
//                    "overload: filter by tvt",
//                    "repos: none",
//                    "filter: MappedCe",
//                    "expected behavior: map from ce to xrm"
//                },
//                new []
//                {
//                    typeof(TestXrmEntity),
//                    typeof(TestCommandEntity),
//                },
//                (services, cache, exceptions)
//                    =>services.AddMapDataProvider<MappedCommandEntityType>(cache,_=>true,
//                        tvt=>tvt.SourceEntity,tvt=>!tvt.ReverseMap,exceptions)
//                ),
//            new(
//                "srv_all_gdo",
//                new []
//                {
//                    "input: xrm, gdo",
//                    "overload: filter by tvt",
//                    "repos: none",
//                    "filter: MappedGdo",
//                    "expected behavior: map from ce to xrm"
//                },
//                new []
//                {
//                    typeof(TestXrmEntity),
//                    typeof(TestCommandEntity),
//                    typeof(TestGraphQlDataObject),
//                },
//                (services, cache, exceptions)
//                    =>services.AddMapDataProvider<MappedGraphQlDataObjectType>(cache,_=>true,
//                        tvt=>tvt.SourceEntity,tvt=>!tvt.ReverseMap,exceptions)
//            ),
//            new(
//                "srv_all_allEntities_separateCall",
//                new []
//                {
//                    "input: all entities",
//                    "call: separate",
//                    "repos: none",
//                    "filter: MappedGdo",
//                    "expected behavior: map from ce to xrm"
//                },
//                new []
//                {
//                    typeof(TestXrmEntity),
//                    typeof(TestCommandEntity),
//                    typeof(TestGraphQlDataObject),
//                },
//                (services, cache, exceptions)
//                    =>services.AddMapDataProvider<MappedGraphQlDataObjectType>(cache,_=>true,
//                        tvt=>tvt.SourceEntity,tvt=>!tvt.ReverseMap,exceptions)
//                        .AddMapDataProvider<MappedCommandEntityType>(cache,_=>true,
//                            tvt=>tvt.SourceEntity,tvt=>!tvt.ReverseMap,exceptions)

//            ),
//            new(
//                "srv_all_allEntities_combinedCall",
//                new []
//                {
//                    "input: all entities",
//                    "repos: none",
//                    "filter: MappedGdo",
//                    "expected behavior: map from ce to xrm"
//                },
//                new []
//                {
//                    typeof(TestXrmEntity),
//                    typeof(TestCommandEntity),
//                    typeof(TestGraphQlDataObject),
//                },
//                (services, cache, exceptions)
//                    =>services.AddMapDataProvider(cache, exceptions)
//            ),
//            new(
//                "srv_all_allEntities_CombCall_allRep",
//                new []
//                {
//                    "input: all entities",
//                    "repos: none",
//                    "filter: MappedGdo",
//                    "Expected Implementations:",
//                    $"  {nameof(TestXrmEntity)}",
//                    $"    IDataProviderR       : {typeof(TestDataProvider<TestXrmEntity>).FullName()}",
//                    $"    IDataProviderRw      : {typeof(TestDataProvider<TestXrmEntity>).FullName()}",
//                    $"    ISourceDataProviderR : {typeof(TestDataProvider<TestXrmEntity>).FullName()}",
//                    $"    ISourceDataProviderRw: {typeof(TestDataProvider<TestXrmEntity>).FullName()}",
//                    $"  {nameof(TestCommandEntity)}",
//                    $"    IDataProviderR       : {nameof(TestCeRepository)}",
//                    $"    IDataProviderRw      : {nameof(TestCeRepository)}",
//                    $"    ISourceDataProviderR : {typeof(WritableMapDataProvider<TestXrmEntity,TestCommandEntity>).FullName()}",
//                    $"    ISourceDataProviderRw: {typeof(WritableMapDataProvider<TestXrmEntity,TestCommandEntity>).FullName()}",
//                    $"  {nameof(TestGraphQlDataObject)}",
//                    $"    IDataProviderR       : {nameof(TestGdoRepository)}",
//                    $"    IDataProviderRw      : -",
//                    $"    ISourceDataProviderR : {typeof(MapDataProvider<TestXrmEntity,TestGraphQlDataObject>).FullName()}",
//                    $"    ISourceDataProviderRw: -",
//                },
//                new []
//                {
//                    typeof(TestXrmEntity),
//                    typeof(TestCommandEntity),
//                    typeof(TestGraphQlDataObject),
//                    typeof(TestCeRepository),
//                    typeof(TestGdoRepository),
//                },
//                (services, cache, exceptions)
//                    =>services.AddMapDataProvider(cache, exceptions)
//                )
//        };
//    }
//    public AddMapDataProviderTests(ITestOutputHelper testOutput)
//        : base(testOutput)
//    {
//    }
//    [Theory, ClassData(typeof(TestArguments))]
//    public override void Test(ReflectionTestOptions options, bool skipTest)
//        => base.Test(options, skipTest);

//    public override void AddServices(IServiceCollection services, ITypeCache cache, ExceptionBuilder exceptions)
//        => services
//            .AddSingleton(new Mock<IMapper>().Object)
//            .AddSingleton(new Mock<IConfigurationProvider>().Object)
//            .AddRepositories(cache, exceptions)
//            .AddDataProvider<CommandEntityType, TestDataProvider<TestXrmEntity>, TestXrmEntity>(
//                cache, x => x.Value == typeof(TestXrmEntity), null, null, exceptions);

//    #region TestedClasses

//    public class TestDataProvider<T> : ISourceDataProviderRw<T>
//        where T : TestXrmEntity
//    {
//        public IQueryable<T> Get() => throw new NotImplementedException();

//        public void Add(T item) => throw new NotImplementedException();

//        public void Add(IEnumerable<T> items) => throw new NotImplementedException();

//        public void Remove(Guid itemId) => throw new NotImplementedException();

//        public void Remove(IEnumerable<Guid> itemIds) => throw new NotImplementedException();
//    }

//    public class TestXrmEntity : ICommandEntity
//    {
//        public Guid Id { get; }
//    }
//    public class TestCommandEntity : IMappedCommandEntity<TestXrmEntity>
//    {
//        public Guid Id { get; }
//    }
//    public class TestGraphQlDataObject : IMappedGraphQlDataObject<TestXrmEntity>
//    {
//        public Guid Id { get; }
//    }
//    public class TestCeRepository : IDataProviderRw<TestCommandEntity>
//    {
//        public IQueryable<TestCommandEntity> Get() => throw new NotImplementedException();

//        public void Add(TestCommandEntity item) => throw new NotImplementedException();

//        public void Add(IEnumerable<TestCommandEntity> items) => throw new NotImplementedException();

//        public void Remove(Guid itemId) => throw new NotImplementedException();

//        public void Remove(IEnumerable<Guid> itemIds) => throw new NotImplementedException();
//    }
//    public class TestGdoRepository : IDataProviderR<TestGraphQlDataObject>
//    {
//        public IQueryable<TestGraphQlDataObject> Get() => throw new NotImplementedException();
//    }
//    #endregion
//}
