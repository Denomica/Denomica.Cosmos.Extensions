using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions
{
    /// <summary>
    /// Extension methods for working with data in a Cosmos DB <see cref="Container"/>.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns whether the given status code indicates a successful operation.
        /// </summary>
        public static bool IsSuccess(this HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            return code >= 200 && code < 300;
        }

        /// <summary>
        /// Enumerates the async enumerable collection and returns it as a list object.
        /// </summary>
        /// <typeparam name="TItem">The type of items in the resulting list.</typeparam>
        /// <param name="items">The items to produce a list from.</param>
        public static async Task<IList<TItem>> ToListAsync<TItem>(this IAsyncEnumerable<TItem> items)
        {
            var list = new List<TItem>();

            await foreach (var item in items)
            {
                list.Add(item);
            }

            return list;
        }

    }
}
