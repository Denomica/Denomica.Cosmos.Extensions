using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions
{
    public static class CosmosExtensions
    {
        public static async Task<T> FirstOrDefaultAsync<T>(this Container container, Expression<Func<T, bool>> predicate)
        {
            var iterator = container
                .GetItemLinqQueryable<T>()
                .Where(predicate)
                .ToFeedIterator();

            if(iterator.HasMoreResults)
            {
                var next = await iterator.ReadNextAsync();
                return next.Resource.FirstOrDefault();
            }

            return default;
        }
    }
}
