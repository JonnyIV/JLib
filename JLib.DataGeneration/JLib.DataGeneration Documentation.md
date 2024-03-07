
# Project Description
This library allows you to manage your testing data using packages and simplifies the use of Ids.

## The Concept
### Persistent IDs
A Data Package can define public Id-Properties which will be automatically set to the same id each time the test is run.
The Id can be either a int, Guid, or a derivative of IntValueType or GuidValueType.
The int and Guid Ids are persisted in a file stored next to the project file, containing all ids for the entire project.
there are two variants of ids: property and named ids.
#### Property IDs
A property id is defined by adding a property of one of the types noted above to the data package.
The getter should be public and the setter must be present and protected.
To get the property id of another package, inject the package and access the property.
#### Runtime IDs
A runtime id is required when Ids are created at runtime, for example when creating a new entity in the database.
To resolve the issue of being unable to resolve this id, an abstraction layer is used.
When running production code, the `JLib.DataGeneration.Abstractions` package can be used to create IDs using the `IIdGenerator`. It can be added to the service collection via `IdGeneratorServiceCollectionExtensions.AddIdGenerator`.
When running tests, another `IIdGenerator` will be added automatically when calling `DataPackageExtensions.AddDataPackages`. 
By injecting the implementation `TestingidGenerator` additional method for creating int and string ids are porovided.
This IdGenerator uses the Stacktrace, specifically the identity (class + method name + generic argument count / generic parameters + parameters) of the caller of the `TestingIdGenerator`
Only the generic type arguments of the method are included in the identity due to technical limitations.
#### Named IDs
A named id is defined by getting it from the datapackage base class. the name must be unique per dataPackage.
NamedIDs should be used when it is not neccessary to reference this entity from another entity like when creating a n:m reference entity.
The name of the id should contain the propertynames of the related ids (for example when you create orderItems for the order named 'CompletedOrder', the name should be 'CompletedOrder_Item_1')
A named Id can not be accessed from another package. 
#### Manual ID
A manual Id is a id which can be defined at runtime. It is composed of a groupName and a IdName.
You **MUST NOT** select the classname of any DataPackage. Doing so might result in duplicate IDs.


### Data Packages
#### Extentions
#### References


## Requirements
- automapper (if you want to use the recommended typed IDs)
- JLib TypeCache 
    - references to all used typed ids
    - reference to the IntValueType / GuidValueType records


## How to
### get the identifier of an id value
during debugging, you can simply use the .Info/
### Create a class Derived from DataPackage and implement the constructor
```cs
using JLib.DataGeneration;
// ...
public class MyDataPackage : DataPackage
{
    public MyDataPackage(IDataPackageStore dataPackages) : base(dataPackages)
    {
    }
}
```
### Get Deterministic Ids
To create deterministic ids, add a public property of type `guid` or a derivative of `GuidValueType` to your `DataPackage`
```cs
using JLib.DataGeneration;
using JLib.ValueTypes;
// ...

#pragma warning disable CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace MyNamespace;

#region referenced types
// id valuetype
public record ArticleId(Guid Value) : GuidValueType(Value);

// efcore entity
public class Article
{
    [Key]
    Guid Id { get; init; }
    string Name { get; init; }
}

public class ShopDbContext : DbContext
#endregion

// DataPackage
public class MyDataPackage : DataPackage
{
    public ArticleId ArticleId { get; init; }
    public MyDataPackage(IDataPackageManager packageManager, MyDbContext dbContext):base(packageManager)
    {
        dbContext.Articles.Add(new()
        {
            Id = ArticleId.Value,
            Name = GetInfoText(nameof(ArticleId))
        });
    }
}

public class UnitTest : IDisposable
{
    [Fact] public async Task ImportLocationsOnly() => await RunTest(new[] { typeof(NotImportedLocationsDp) });
   
  

    private readonly CancellationToken _cancellationToken;
    private readonly IServiceProvider _provider;
    private readonly List<IDisposable> _disposables = new();
    private readonly ITypeCache _typeCache;

    public UnitTest()
    {
        var id = Guid.NewGuid();
        var exceptions = new ExceptionBuilder("test setup");
        var services = new ServiceCollection()
            .AddTypeCache(out _typeCache, exceptions,
                BackendTestsTypePackage.Instance, JLibDataGenerationTypePackage.Instance)
            .AddAutoMapper(p => p.AddProfiles(_typeCache))
            .AddDataPackages(_typeCache)
    }
}
```

## How it works
### start-time
- ServiceCollection.AddDataPackages
    - the types are pulled from the typecache
    - then they get added to the serviceCollection
    - the dataPackageManager is added
- ServiceProvider.IncludeDataPackages
    - DataPackageManager.InitState uninitialized => initializing
    - execute delegate to get a list of dataPackages to load
    - load said data packages
        - data package constructor
            - iterate over all properties which contain a id
            - pull the id from the IdRegistry
                - if the id does not exist, create a new one
            - idProperty = pulled id
    - DataPackageManager.InitState initializing => initialized
    - IdRegistry.SaveToFile



### classes
- DataPackage
- DataPackageManager
    - information about the init state (uninitialized/initializing/initialized)


-------------------------------
    

# todo
- speichern von int ids
- persistieren von ids die zur test-laufzeit erstellt werden
    - probleme
        - die laufzeit dienste brauchen einen weg die IDs zu generieren, werden jedoch als fertiger provider dem DataPackageManager übergeben
    - ansatz 1: testIdIdentifier = testklasse+methode+entität+inkrementnummer/entität
        - probleme: 
            - race-conditions
            - wird das anlegen einer neuen entität nötig, werden alle folgenden IDs invalide
                - dies könnte reduziert werden, indem man nach jedem methodenaufruf einen neuen inkrement-bereich erstellt
            - 
    - ansatz 2: testIdentifier = callstack 
        - ressourcen
            - https://stackoverflow.com/questions/6624326/programmatically-get-c-sharp-stack-trace
        - probleme: 
            - extrem anfällig für private anpassungen/umbenennungen von internen methoden
            - der callstacktext ändert sich nach debug/release build
        - vorteile
            - jeder aufruf kann eindeutig zugeordnet werden

# Evaluieren
- zusammenführen des data providers für services und datenpakete
    - vorteile
        - würde das problem vom übergeben des ID-generators elegant lösen
        - es würde den code vereinfachen
    - nachteile
        - die initalisierung könnte komplizierter werden, da man nach dem hinzufügen der pakete diese exakt ein mal initalisieren muss
            - ServiceCollection.addDataPackages()
            - ServiceProvider.LoadDataPackages()
                - nach dem laden müssen die neuen IDs gespeichert werden
    - ergebnisse
        - man könnte sich in den tests die datenpakete injecten.
        - man könnte sich den DataPackageStore Injecten
            - das nachträgliche Injecten von Paketen, z.B. durch referenzierungen in diensten, könnte zur laufzeit daten hinzufügen und so die Datenbaser intransparenter machen
                - Lösung: 
                    - beim instantiieren eines pakets wird geprüft, ob das laden bereits abgeschlossen ist. wenn ja, würde eine exception geworfen werden
                      da tests deterministisch sein sollen, würde die exception auch zuverlässig den test direkt am durchlaufen hindern.
                    - über reflection prüfen, ob eine klasse die datenpakete unerlaubt nutzt.
                      **nachteil:** das anfragen von datenpaketen via serviceProvider.GetRequiredService würde nicht gefunden werden
        - datenpakete können direkt den relevanten service injecten
    - **alternative**
        - die IdRegistry wird als singleton vom serviceProvider bereitgestellt und vom packageProvider als singleton-instanz referenziert