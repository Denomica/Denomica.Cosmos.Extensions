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

        /// <summary>
        /// Deletes the item with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="container">The container to delete from.</param>
        /// <param name="id">The ID of the item to delete.</param>
        /// <param name="partition">The partition of the item to delete.</param>
        /// <param name="throwIfNotfound">Specifies whether to throw an exception if the specified item is not found. Defaults to <c>true</c>.</param>
        /// <param name="defaultRetryAfterMilliseconds">The number of milliseconds to wait before retrying the operation in case the server responds with HTTP 429. This value is only used if no other information is received with the HTTP 429 response.</param>
        /// <exception cref="Exception">The exception that is thrown </exception>
        public static async Task DeleteItemAsync(this Container container, string id, PartitionKey partition, bool throwIfNotfound = true, int defaultRetryAfterMilliseconds = 5000)
        {
            TimeSpan delay = TimeSpan.Zero;
            bool retry = false;
            do
            {
                try
                {
                    var response = await container.DeleteItemStreamAsync(id, partition);
                    retry = await response.HandleResponseAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                }
                catch(CosmosException ex)
                {
                    retry = await ex.HandleExceptionAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds, throwIfNotFound: throwIfNotfound);
                }
            } while(retry);
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

        /// <summary>
        /// Handles the response received from <paramref name="responseProvider"/> and optionally retries the action in case the status returns
        /// HTTP 429 (too many requests).
        /// </summary>
        /// <param name="container"></param>
        /// <param name="responseProvider">A delegate that is used to get the response.</param>
        /// <param name="defaultRetryAfterMilliseconds">The default number of milliseconds to wait before retrying the request in case HTTP 429 is returned.</param>
        public static async Task<ResponseMessage> GetResponseAsync(this Container container, Func<Task<ResponseMessage?>> responseProvider, int defaultRetryAfterMilliseconds = 5000)
        {
            ResponseMessage? response = null!;
            bool retry = false;

            do
            {
                retry = false;
                try
                {
                    response = await responseProvider();
                    retry = await response.HandleResponseAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                }
                catch(CosmosException ex)
                {
                    retry = await ex.HandleExceptionAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                }
            } while (retry);

            if(null == response)
            {
                throw new Exception("Unable to resolve response.");
            }
            return response;
        }

        public static async IAsyncEnumerable<JsonElement> QueryAsync(this Container container, QueryDefinition query, int defaultRetryAfterMilliseconds = 5000)
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
                }, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                continuationToken = response.ContinuationToken;

                if(null != response?.Content)
                {
                    var json = await JsonDocument.ParseAsync(response.Content);
                    if(json.RootElement.TryGetProperty("Documents", out var docs) && docs.ValueKind == JsonValueKind.Array)
                    {
                        foreach(var item in docs.EnumerateArray())
                        {
                            yield return item;
                        }
                    }
                }
            } while (continuationToken?.Length > 0);

            yield break;
        }

        public static async IAsyncEnumerable<TItem> QueryAsync<TItem>(this Container container, QueryDefinition query, int defaultRetryAfterMilliseconds = 5000, JsonSerializerOptions? serializationOptions = null)
        {
            var options = serializationOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            await foreach(var item in container.QueryAsync(query, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds))
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



        /// <summary>
        /// Handles the response by checking for HTTP status 429.
        /// </summary>
        /// <param name="response">The response to handle.</param>
        /// <param name="defaultRetryAfterMilliseconds"></param>
        /// <returns>Returns <c>true</c> if the response was a HTTP 429 and if the operation should be retried after this method returns.</returns>
        /// <remarks>
        /// If the response was a HTTP 429, then this method will attempt to determine for how long to wait before retrying
        /// the operation that produced the response.
        /// </remarks>
        private static async Task<bool> HandleResponseAsync(this ResponseMessage? response, int defaultRetryAfterMilliseconds = 5000)
        {
            if(null != response)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    TimeSpan delay = TimeSpan.FromMilliseconds(defaultRetryAfterMilliseconds);
                    var retryAfter = response.Headers["Retry-After"];
                    if (RetryConditionHeaderValue.TryParse(retryAfter, out RetryConditionHeaderValue h))
                    {
                        delay = h.Delta ?? delay;
                    }
                    await Task.Delay(delay);

                    return true;
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code received from Cosmos DB. Status code: {response.StatusCode}");
                }
            }

            return false;
        }

        /// <summary>
        /// Handles the exception if it was thrown from HTTP status 429.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <returns>Returns <c>true</c> if the exception was associated with HTTP 429 and the operation that threw the exception needs to be retried.</returns>
        private static async Task<bool> HandleExceptionAsync(this CosmosException? ex, int defaultRetryAfterMilliseconds = 5000, bool throwIfNotFound = true)
        {
            if(null != ex)
            {
                if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await Task.Delay(ex.RetryAfter ?? TimeSpan.FromMilliseconds(defaultRetryAfterMilliseconds));
                    return true;
                }
                else if(ex.StatusCode == HttpStatusCode.NotFound)
                {
                    if(throwIfNotFound)
                    {
                        throw new Exception($"An item was not found. Status: {ex.StatusCode}.", ex);
                    }
                }
                else
                {
                    throw new Exception($"Unexpected status code from Cosmos DB. Status code: {ex.StatusCode}.", ex);
                }
            }

            return false;
        }
    }

}
