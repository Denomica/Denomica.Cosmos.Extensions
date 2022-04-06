using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Denomica.Cosmos.Extensions
{

    /// <summary>
    /// Extension methods for working with data in a Cosmos DB <see cref="Container"/>.
    /// </summary>
    public static class ExtensionMethods
    {

        public static async Task DeleteItemAsync(this Container container, string id, PartitionKey partition, bool throwIfNotfound = true, int defaultRetryAfterMilliseconds = 5000)
        {
            TimeSpan delay = TimeSpan.Zero;

            do
            {
                if (delay > TimeSpan.Zero) await Task.Delay(delay);

                try
                {
                    await container.DeleteItemStreamAsync(id, partition);
                }
                catch(CosmosException ex)
                {
                    if(ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        delay = ex.RetryAfter ?? TimeSpan.FromSeconds(5);
                    }
                    else if(ex.StatusCode == HttpStatusCode.NotFound && throwIfNotfound)
                    {
                        throw new Exception($"Item with ID: {id} and partition: {partition} was not found. Cannot delete.");
                    }
                    else
                    {
                        throw new Exception($"Unexpected exception when trying to delete item. ID: {id}, partition: {partition}. Status code: {ex.StatusCode}", ex);
                    }
                }
            } while(delay > TimeSpan.Zero);
        }

        /// <summary>
        /// Deletes the item with the given <paramref name="id"/> and <paramref name="partitionKey"/>.
        /// </summary>
        /// <param name="container">The container to delete the item from.</param>
        /// <param name="id">The ID of the item to delete.</param>
        /// <param name="partitionKey">The partition key of the item to delete.</param>
        /// <param name="throwIfNotFound">Specifies whether to throw an exception if the specified item is not found.</param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The default amount of milliseconds to wait in case the server responds with HTTP status 492 (too many requests) 
        /// and that response does not contain a specified interwal to wait before retrying the operation.
        /// </param>
        public static Task DeleteItemAsync(this Container container, string id, string partitionKey, bool throwIfNotFound = true, int defaultRetryAfterMilliseconds = 5000)
        {
            return container.DeleteItemAsync(id, new PartitionKey(partitionKey), throwIfNotfound: throwIfNotFound, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
        }

        public static async Task<ResponseMessage> GetResponseAsync(this Container container, Func<Task<ResponseMessage?>> responseProvider, int defaultRetryIntervalMilliseconds = 5000)
        {
            ResponseMessage? response = null!;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    response = await responseProvider();
                }
                catch(CosmosException ex)
                {
                    if(ex.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(ex.RetryAfter ?? TimeSpan.FromMilliseconds(defaultRetryIntervalMilliseconds));
                        retry = true;
                    }
                    else
                    {
                        throw;
                    }
                }

                if(null != response)
                {
                    if (response.StatusCode == HttpStatusCode.TooManyRequests)
                    {
                        TimeSpan delay = TimeSpan.FromMilliseconds(defaultRetryIntervalMilliseconds);
                        var retryAfter = response.Headers["Retry-After"];
                        if (RetryConditionHeaderValue.TryParse(retryAfter, out RetryConditionHeaderValue h))
                        {
                            delay = h.Delta ?? delay;
                        }
                        retry = true;
                    }
                    else if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Unexpected status code when querying Cosmos DB. Status code: {response.StatusCode}");
                    }
                }

            } while (retry);

            if(null == response)
            {
                throw new NullReferenceException("Unable to resolve response.");
            }
            return response;
        }

        public static async IAsyncEnumerable<JsonNode> QueryAsync(this Container container, QueryDefinition query)
        {
            string? continuationToken = null;

            do
            {
                var response = await container.GetResponseAsync(async () =>
                {
                    var iterator = container.GetItemQueryStreamIterator(query, continuationToken: continuationToken);
                    if(iterator.HasMoreResults)
                    {
                        return await iterator.ReadNextAsync();
                    }

                    return null;
                });
                continuationToken = response.ContinuationToken;

                if(null != response?.Content)
                {
                    var json = await JsonDocument.ParseAsync(response.Content);
                    if(json.RootElement.TryGetProperty("Documents", out var docs) && docs.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var item in docs.EnumerateArray())
                        {
                            var itemJson = JsonSerializer.Serialize(item);
                            var node = JsonNode.Parse(itemJson);
                            if(null != node)
                            {
                                yield return node;
                            }
                        }
                    }
                }
            } while (continuationToken?.Length > 0);

            yield break;
        }

        public static async IAsyncEnumerable<TItem> QueryAsync<TItem>(this Container container, QueryDefinition query, JsonSerializerOptions? serializationOptions = null)
        {
            var options = serializationOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            await foreach(var item in container.QueryAsync(query))
            {
                var resultItem = JsonSerializer.Deserialize<TItem>(item, options: options);
                if(null != resultItem)
                {
                    yield return resultItem;
                }
            }
        }

        public static async Task<IList<TItem>> ToListAsync<TItem>(this IAsyncEnumerable<TItem> items)
        {
            var list = new List<TItem>();

            await foreach(var item in items)
            {
                list.Add(item); 
            }

            return list;
        }
    }

}
