using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace Denomica.Cosmos.Extensions
{
    /// <summary>
    /// Represents a query result from a query excuted with the <see cref="ContainerProxy"/> class.
    /// </summary>
    /// <typeparam name="T">The type that query result items are returned as.</typeparam>
    public class QueryResult<T>
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        public QueryResult() { }

        internal QueryResult(ContainerProxy proxy, QueryDefinition query, QueryOptions options, Type? returnAs = null)
        {
            this.Proxy = proxy ?? throw new ArgumentNullException(nameof(proxy));
            this.Query = query ?? throw new ArgumentNullException(nameof(query));
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
            this.ReturnAs = returnAs;
        }

        private readonly ContainerProxy? Proxy;
        private readonly QueryDefinition? Query;
        private readonly QueryOptions? Options;
        private readonly Type? ReturnAs;

        /// <summary>
        /// The items that were produced by executing a query.
        /// </summary>
        public IEnumerable<T> Items { get; internal set; } = Enumerable.Empty<T>();

        /// <summary>
        /// The continuation token that is used to get the next set of items.
        /// </summary>
        public string? ContinuationToken { get; internal set; }

        /// <summary>
        /// The request charge for producing the items in <see cref="Items"/>.
        /// </summary>
        public double RequestCharge { get; internal set; }


        /// <summary>
        /// Returns the next set of results or <c>null</c> if there are no more results.
        /// </summary>
        public async Task<QueryResult<T>?> GetNextResultAsync()
        {
            if(null != this.Proxy && null != this.Query && this.ContinuationToken?.Length > 0 && null != this.Options)
            {
                return await this.Proxy.QueryItemsAsync<T>(this.Query, new QueryOptions
                { 
                    ContinuationToken = this.ContinuationToken, 
                    MaxItemCount = this.Options.MaxItemCount
                }, returnAs: this.ReturnAs);
            }

            return null;
        }
    }
}
