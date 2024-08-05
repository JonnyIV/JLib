using JLib.Exceptions;
using JLib.Helper;
using JLib.Reflection;
using static JLib.Reflection.TvtFactoryAttribute;

namespace JLib.DataProvider;

public abstract record DataProviderType(Type Value) : NavigatingTypeValueType(Value),
    IPostNavigationInitializedType, IValidatedType
{
    public bool CanWrite { get; private set; }

    void IPostNavigationInitializedType.Initialize(ITypeCache _, ExceptionBuilder exceptions)
    {
        CanWrite = Value.ImplementsAny<IDataProviderRw<IEntity>>();
    }

    public virtual void Validate(ITypeCache cache, TypeValidationContext value)
    {
    }
}


[ImplementsAny(typeof(IDataProviderR<>)), BeGeneric, NotAbstract, IsClass]
public record SourceDataProviderType(Type Value) : DataProviderType(Value)
{
    public override void Validate(ITypeCache cache, TypeValidationContext value)
    {
        base.Validate(cache, value);
        if (Value.ImplementsAny<IDataProviderRw<IEntity>>())
            value.ShouldImplementAny<ISourceDataProviderRw<IEntity>>();
        else
            value.ShouldImplementAny<ISourceDataProviderR<IDataObject>>();
    }
}
[ImplementsAny(typeof(IDataProviderR<>)), NotGeneric, NotAbstract, IsClass]
public record RepositoryType(Type Value) : DataProviderType(Value)
{
    public DataObjectType ProvidedDataObject
        => Navigate(cache => Value.GetAnyInterface<IDataProviderR<IDataObject>>()?.GenericTypeArguments.First()
            .AsValueType<DataObjectType>(cache)!);

    public override void Validate(ITypeCache cache, TypeValidationContext value)
    {
        base.Validate(cache, value);
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (ProvidedDataObject is null)
            value.Validate("The Data Object type could not be resolved");
        value.ShouldNotBeGeneric(
            Environment.NewLine + $"if you tried to build a generic data provider, you have to implement {nameof(ISourceDataProviderR<IDataObject>)} or {nameof(ISourceDataProviderRw<IEntity>)}"
                                + Environment.NewLine +
                                $"If you tried to build a Repository, you have to implement {nameof(IDataProviderR<IDataObject>)} or {nameof(IDataProviderRw<IEntity>)}");
        value.ShouldNotImplementAny<ISourceDataProviderR<IDataObject>>();
    }
}