using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Denomica.Cosmos.Extensions.Internal;

namespace Denomica.Cosmos.Extensions
{
    public class ContainerProxy
    {
        public ContainerProxy(Container container, JsonSerializerOptions? serializationOptions = null, int defaultRetryAfterMilliseconds = 5000)
        {
            this.Container = container ?? throw new ArgumentNullException(nameof(container));
            this.SerializationOptions = serializationOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            this.DefaultRetryAfterMilliseconds = defaultRetryAfterMilliseconds;
        }

        public Container Container { get; private set; }

        public JsonSerializerOptions SerializationOptions { get; private set; }

        public int DefaultRetryAfterMilliseconds { get; private set; }



        public Task<ResponseMessage> DeleteItemAsync(string id, PartitionKey partition, bool throwIfNotfound = true)
        {
            return this.Container.DeleteItemAsync(id, partition, throwIfNotfound: throwIfNotfound, this.DefaultRetryAfterMilliseconds);
        }

        public Task<ResponseMessage> DeleteItemAsync(string id, string? partition, bool throwIfNotfound = true)
        {
            return this.DeleteItemAsync(id, partition?.Length > 0 ? new PartitionKey(partition) : PartitionKey.None, throwIfNotfound: throwIfNotfound);
        }

        public IAsyncEnumerable<JsonElement> QueryItemsAsync(QueryDefinition query)
        {
            return this.Container.QueryItemsAsync(query, defaultRetryAfterMilliseconds: this.DefaultRetryAfterMilliseconds);
        }

        public async IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(QueryDefinition query)
        {
            await foreach (var item in this.QueryItemsAsync(query))
            {
                var resultItem = JsonSerializer.Deserialize<TItem>(item, options: this.SerializationOptions);
                if (null != resultItem)
                {
                    yield return resultItem;
                }
            }

        }

        public async Task<ItemResponse<object>> UpsertItemAsync(object item, PartitionKey? partitionKey = null)
        {
            return await this.UpsertItemAsync<object>(item, partitionKey: partitionKey);
        }

        public async Task<ItemResponse<TItem>> UpsertItemAsync<TItem>(TItem item, PartitionKey? partitionKey = null)
        {
            return await this.Container.UpsertItemAsync<TItem>(item, partitionKey: partitionKey, defaultRetryAfterMilliseconds: this.DefaultRetryAfterMilliseconds);
        }

    }
}
