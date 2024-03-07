using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using JLib.ValueTypes;

namespace JLib.DataGeneration.Abstractions;

/// <summary>
/// Represents an interface for generating unique identifiers.
/// </summary>
public interface IIdGenerator
{
    /// <summary>
    /// Creates a new <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="Guid"/>.</returns>
    Guid CreateGuid();

    /// <summary>
    /// Creates a new <see cref="Guid"/> of type <typeparamref name="TVt"/>.
    /// </summary>
    /// <typeparam name="TVt">The type of the <see cref="GuidValueType"/> to create.</typeparam>
    /// <returns>A new <see cref="Guid"/> of type <typeparamref name="TVt"/>.</returns>
    TVt CreateGuid<TVt>()
        where TVt : GuidValueType;
}

/// <summary>
/// Represents an implementation of the <see cref="IIdGenerator"/> interface.
/// </summary>
public sealed class IdGenerator : IIdGenerator
{
    private readonly IMapper _mapper;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdGenerator"/> class.
    /// </summary>
    /// <param name="mapper">The <see cref="IMapper"/> instance used for mapping.</param>
    public IdGenerator(IMapper mapper)
    {
        _mapper = mapper;
    }
    /// <summary>
    /// Creates a new <see cref="Guid"/>.
    /// </summary>
    /// <returns>A new <see cref="Guid"/>.</returns>
    public Guid CreateGuid() 
        => Guid.NewGuid();

    /// <summary>
    /// Creates a new <see cref="Guid"/> of type <typeparamref name="TVt"/>.
    /// </summary>
    /// <typeparam name="TVt">The type of the <see cref="GuidValueType"/> to create.</typeparam>
    /// <returns>A new <see cref="Guid"/> of type <typeparamref name="TVt"/>.</returns>
    public TVt CreateGuid<TVt>() where TVt : GuidValueType
        => _mapper.Map<TVt>(CreateGuid());
}
