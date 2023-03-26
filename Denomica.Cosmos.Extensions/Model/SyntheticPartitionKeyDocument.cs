using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <remarks>
        /// Default is <c>"/"</c>.
        /// </remarks>
        protected virtual string PartitionKeyPropertySeparator
        {
            get { return "/"; }
        }

        /// <summary>
        /// Defines whether to use partition key properties from base classes too.
        /// </summary>
        /// <remarks>
        /// Default is <c>true</c>.
        /// </remarks>
        protected virtual bool InheritPartitionKeyProperties
        {
            get { return true; }
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
                var propFlags = this.InheritPartitionKeyProperties
                    ? BindingFlags.Instance | BindingFlags.Public
                    : BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;

                var partitionProps = from x in this.GetType().GetProperties(propFlags)
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
            var elements = new List<object>();

            foreach(var property in partitionProperties)
            {
                var a = property.GetCustomAttribute<PartitionKeyPropertyAttribute>();
                var val = property.GetValue(this);
                var ci = a?.Culture?.Length > 0 ? new CultureInfo(a.Culture) : CultureInfo.InvariantCulture;

                if (a?.FormatString?.Length > 0)
                {
                    elements.Add(string.Format(ci, $"{{0:{a.FormatString}}}", property.GetValue(this)));
                }
                else
                {
                    elements.Add($"{val}");
                }
            }

            this.Partition = string.Join(this.PartitionKeyPropertySeparator, elements);
        }
    }
}
