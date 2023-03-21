using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Denomica.Cosmos.Extensions.Model
{
    /// <summary>
    /// A base class for documents where you want to use the synthetic partition key design pattern.
    /// </summary>
    /// <remarks>
    /// For details on the synthetic partition key pattern, see https://learn.microsoft.com/azure/cosmos-db/nosql/synthetic-partition-keys
    /// </remarks>
    public class SyntheticPartitionKeyDocument : TimestampedDocumentBase
    {

        /// <summary>
        /// Sets or reutrns the partition key for the document.
        /// </summary>
        public string Partition
        {
            get { return this.GetProperty<string>(nameof(Partition), () => this.Type); }
            set { this.SetProperty(nameof(Partition), value); }
        }


        /// <summary>
        /// The separator string to use when constructing the synthetic partition key stored in <see cref="Partition"/>.
        /// </summary>
        protected virtual string PartitionKeyPropertySeparator
        {
            get { return "/"; }
        }

        /// <summary>
        /// Handles the value on the <see cref="Partition"/> property using other property values.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPropertyValueChanged(PropertyValueChangedEventArgs e)
        {
            var changedProp = this.GetType().GetProperty(e.Name);
            if(null != changedProp)
            {
                var partitionProps = from x in this.GetType().GetProperties()
                                     where null != x.GetCustomAttribute<PartitionKeyPropertyAttribute>(false)
                                     orderby x.GetCustomAttribute<PartitionKeyPropertyAttribute>().Index
                                     select x;

                if(partitionProps?.Count() > 0)
                {
                    this.SetPartition(partitionProps);
                }
            }

            base.OnPropertyValueChanged(e);
        }

        /// <summary>
        /// Responsible for setting the value
        /// </summary>
        /// <param name="partitionProperties"></param>
        protected virtual void SetPartition(IEnumerable<PropertyInfo> partitionProperties)
        {
            this.Partition = string.Join(this.PartitionKeyPropertySeparator, from x in partitionProperties select x.GetValue(this));
        }
    }
}
