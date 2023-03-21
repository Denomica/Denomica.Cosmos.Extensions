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
        public PartitionKeyPropertyAttribute(int index)
        {
            this.Index = index;
        }

        /// <summary>
        /// The index for the property.
        /// </summary>
        public int Index { get; private set; }

    }
}
