using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.Model
{
    /// <summary>
    /// An attribute that is used to decorate those properties whose values are used when constructing a synthetic partition key in the <see cref="SyntheticPartitionKeyDocument"/> class.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PartitionKeyPropertyAttribute : Attribute
    {
        /// <summary>
        /// Creates a new instance of the class.
        /// </summary>
        /// <param name="index">The index that defines the order in which properties are used for constructing a synthetic partition key.</param>
        /// <param name="formatString"></param>
        /// <param name="culture"></param>
        public PartitionKeyPropertyAttribute(int index, string? formatString = null, string? culture = null)
        {
            this.Index = index;
            this.FormatString = formatString;
            this.Culture = culture;
        }

        /// <summary>
        /// The index for the property.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// A format string that is used to format the value of the property when uses as part of the partition key.
        /// </summary>
        public string? FormatString { get; private set; }

        /// <summary>
        /// The culture to use when formatting the value of the property for the partition key.
        /// </summary>
        public string? Culture { get; private set; }

    }
}
