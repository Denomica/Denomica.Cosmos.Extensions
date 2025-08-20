using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Denomica.Cosmos.Extensions
{
    /// <summary>
    /// A service implementation that provides methods for working with queries to a Cosmos DB container instance.
    /// </summary>
    public class QueryService
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueryService"/> class,  providing access to query operations on
        /// a specified Cosmos DB container.
        /// </summary>
        /// <param name="client">The <see cref="CosmosClient"/> instance used to interact with the Cosmos DB account. Cannot be <see
        /// langword="null"/>.</param>
        /// <param name="container">The <see cref="Container"/> instance representing the target Cosmos DB container. Cannot be <see
        /// langword="null"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="client"/> or <paramref name="container"/> is <see langword="null"/>.</exception>
        public QueryService(CosmosClient client, Container container)
        {
            this.Client = client ?? throw new ArgumentNullException(nameof(client));
            this.Container = container ?? throw new ArgumentNullException(nameof(container));
        }


        /// <summary>
        /// Gets the instance of the <see cref="CosmosClient"/> used to interact with the Azure Cosmos DB service.
        /// </summary>
        public CosmosClient Client { get; private set; }

        /// <summary>
        /// Gets the container associated with the current instance.
        /// </summary>
        public Container Container { get; private set; }

        /// <summary>
        /// Creates a <see cref="PartitionKey"/> object from the given value.
        /// </summary>
        /// <param name="value">The value to create the <see cref="PartitionKey"/> from.</param>
        /// <exception cref="InvalidCastException">
        /// The exception that is found if <paramref name="value"/> cannot be converted into a 
        /// value that can be used to create a <see cref="PartitionKey"/> object from.
        /// </exception>
        public PartitionKey CreatePartitionKey(object? value)
        {
            PartitionKey key = PartitionKey.Null;
            if (null != value)
            {
                if (value is string)
                {
                    key = new PartitionKey((string)value);
                }
                else if (value is Guid)
                {
                    key = new PartitionKey($"{value}");
                }
                else if (value is double)
                {
                    key = new PartitionKey((double)value);
                }
                else if (double.TryParse($"{value}", out double d))
                {
                    key = new PartitionKey(d);
                }
                else if (value is bool)
                {
                    key = new PartitionKey((bool)value);
                }
                else if (bool.TryParse($"{value}", out bool b))
                {
                    key = new PartitionKey(b);
                }
                else if (value is JsonElement)
                {
                    var elem = (JsonElement)value;
                    switch (elem.ValueKind)
                    {
                        case JsonValueKind.String:
                            key = new PartitionKey(elem.GetString());
                            break;

                        case JsonValueKind.Number:
                            if (elem.TryGetInt64(out var i))
                            {
                                key = new PartitionKey(i);
                            }
                            else if (elem.TryGetDouble(out var dd))
                            {
                                key = new PartitionKey(dd);
                            }
                            else
                            {
                                throw new InvalidCastException($"Cannot convert numeric value '{elem.GetRawText()}' to either integer or double.");
                            }
                            break;

                        case JsonValueKind.True:
                        case JsonValueKind.False:
                            key = new PartitionKey(elem.GetBoolean());
                            break;

                        case JsonValueKind.Null:
                            key = PartitionKey.Null;
                            break;

                        default:
                            throw new InvalidCastException($"Cannot convert JsonElement whose value type is '{elem.ValueKind}' to a PartitionKey.");
                    }
                }
            }

            return key;
        }

    }
}
