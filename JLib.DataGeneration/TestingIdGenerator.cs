using System.Collections.Concurrent;
using System.Diagnostics;
using AutoMapper;
using JLib.DataGeneration.Abstractions;
using JLib.Helper;
using JLib.ValueTypes;
using static JLib.DataGeneration.DataPackageValues;

namespace JLib.DataGeneration;
/// <summary>
/// Represents a testing ID generator.
/// </summary>
public sealed class TestingIdGenerator : IIdGenerator
{
    private class Counter
    {
        private int _value = 0;

        /// <summary>
        /// Gets the current value of the counter.
        /// </summary>
        public int Value => _value;

        /// <summary>
        /// Increments the counter value by 1 and returns the new value.
        /// </summary>
        /// <returns>The new value of the counter.</returns>
        public int Increment() => Interlocked.Increment(ref _value);
    }

    private readonly IIdRegistry _idRegistry;
    private readonly IMapper _mapper;

    private ConcurrentDictionary<string, Counter> _callCounter = new();
    private readonly Dictionary<IdScopeName, ConcurrentDictionary<string, Counter>> _scopeCounterStorage = new();
    /// <summary>
    /// The Name of the current id Scope.
    /// </summary>
    public IdScopeName CurrentIdScopeName { get; private set; }=new("Default");

    /// <summary>
    /// Initializes a new instance of the <see cref="TestingIdGenerator"/> class.
    /// </summary>
    /// <param name="idRegistry">The ID registry.</param>
    /// <param name="mapper">The mapper.</param>
    public TestingIdGenerator(IIdRegistry idRegistry, IMapper mapper)
    {
        _idRegistry = idRegistry;
        _mapper = mapper;
    }
    
    /// <summary>
    /// opens a new id scope. Ids are counted per scope and are used to reduce test vulnerability.
    /// </summary>
    /// <param name="name"></param>
    public void SetIdScope(IdScopeName name)
    {
        _scopeCounterStorage[CurrentIdScopeName] = _callCounter;
        _callCounter = _scopeCounterStorage.GetValueOrAdd(name, () => new());
        CurrentIdScopeName = name;
    }

    /// <summary>
    /// Gets the identifier for the specified stack trace frame index.
    /// </summary>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame.</param>
    /// <returns>The identifier for the stack trace frame.</returns>
    private IdIdentifier GetIdentifier(int stackTraceFrameIndex)
    {
        var stackTrace = new StackTrace();
        // index one is CreateGuid, therefor is index 2 it's caller
        var front = stackTrace.GetFrame(stackTraceFrameIndex + 2);
        var method = front?.GetMethod();

        var callCount = _callCounter.GetValueOrAdd(
            method?.FullName(true, true, false, true)
            ?? "", _ => new())
            .Increment();
        IdName idName = method is not null
            ? new(CurrentIdScopeName, method, callCount)
            : new($"non Method Access {callCount}");
        var idGroupName = new IdGroupName(
            method?.ReflectedType?.FullName(true)
            ?? front?.GetFileName()
            ?? "unknown source");

        return new(idGroupName, idName);
    }

    /// <summary>
    /// Creates a new string ID.
    /// </summary>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame used to identify this id where index 0 is the caller of this method.</param>
    /// <returns>The generated string ID.</returns>
    public string CreateStringId(int stackTraceFrameIndex = 0)
        => _idRegistry.GetStringId(GetIdentifier(stackTraceFrameIndex));

    /// <summary>
    /// Creates a new string ID of the specified value type.
    /// </summary>
    /// <typeparam name="TId">The value type of the string ID.</typeparam>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame used to identify this id where index 0 is the caller of this method.</param>
    /// <returns>The generated string ID of the specified value type.</returns>
    public TId CreateStringId<TId>(int stackTraceFrameIndex = 0)
        where TId : StringValueType
        => _mapper.Map<TId>(_idRegistry.GetStringId(GetIdentifier(stackTraceFrameIndex)));

    /// <summary>
    /// Creates a new integer ID.
    /// </summary>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame used to identify this id where index 0 is the caller of this method.</param>
    /// <returns>The generated integer ID.</returns>
    public int CreateIntId(int stackTraceFrameIndex = 0)
        => _idRegistry.GetIntId(GetIdentifier(stackTraceFrameIndex));

    /// <summary>
    /// Creates a new integer ID of the specified value type.
    /// </summary>
    /// <typeparam name="TId">The value type of the integer ID.</typeparam>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame where index 0 is the caller of this method.</param>
    /// <returns>The generated integer ID of the specified value type.</returns>
    public TId CreateIntId<TId>(int stackTraceFrameIndex = 0)
        where TId : IntValueType
        => _mapper.Map<TId>(_idRegistry.GetIntId(GetIdentifier(stackTraceFrameIndex)));

    /// <summary>
    /// Creates a new GUID.
    /// </summary>
    /// <returns>The generated GUID.</returns>
    public Guid CreateGuid()
        => _idRegistry.GetGuidId(GetIdentifier(0));

    /// <summary>
    /// Creates a new GUID of the specified value type.
    /// </summary>
    /// <typeparam name="TId">The value type of the GUID.</typeparam>
    /// <returns>The generated GUID of the specified value type.</returns>
    public TId CreateGuid<TId>() where TId : GuidValueType
        => _mapper.Map<TId>(_idRegistry.GetGuidId(GetIdentifier(0)));

    /// <summary>
    /// Creates a new GUID of the specified value type.
    /// </summary>
    /// <typeparam name="TId">The value type of the GUID.</typeparam>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame where index 0 is the caller of this method.</param>
    /// <returns>The generated GUID of the specified value type.</returns>
    public TId CreateGuid<TId>(int stackTraceFrameIndex) where TId : GuidValueType
        => _mapper.Map<TId>(_idRegistry.GetGuidId(GetIdentifier(stackTraceFrameIndex)));

    /// <summary>
    /// Creates a new GUID.
    /// </summary>
    /// <param name="stackTraceFrameIndex">The index of the stack trace frame where index 0 is the caller of this method.</param>
    /// <returns>The generated GUID.</returns>
    public Guid CreateGuid(int stackTraceFrameIndex)
        => _idRegistry.GetGuidId(GetIdentifier(stackTraceFrameIndex));
}
