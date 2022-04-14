using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions
{
    /// <summary>
    /// Defines options for how to execute a query.
    /// </summary>
    public class QueryOptions
    {
        /// <summary>
        /// The continuation token that allows for paging through results.
        /// </summary>
        public string? ContinuationToken { get; set; }

        /// <summary>
        /// The maximum number of items to include with each page of results.
        /// </summary>
        public int? MaxItemCount { get; set; }
    }
}
