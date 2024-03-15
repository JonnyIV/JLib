using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace JLib.DataProvider.EfCore;
/// <summary>
/// Provides extension methods for queryable collections of data objects.
/// </summary>
public static class DataObjectQueryableExtensions
{
    /// <summary>
    /// Converts the queryable collection to a dictionary asynchronously, using the <see cref="IDataObject.Id"/> as the key.
    /// </summary>
    /// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
    /// <param name="queryable">The queryable collection.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A dictionary with the values mapped by their IDs.</returns>
    public static async Task<IReadOnlyDictionary<Guid, TValue>> ToDictionaryAsync<TValue>(this IQueryable<TValue> queryable, CancellationToken cancellationToken)
        where TValue : IDataObject
    {
        return await queryable.ToDictionaryAsync(x => x.Id, cancellationToken);
    }
}
