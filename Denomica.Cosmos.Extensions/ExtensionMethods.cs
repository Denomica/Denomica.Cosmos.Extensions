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
        /// Deletes the document with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="container">The container to delete from.</param>
        /// <param name="id">The ID of the document to delete.</param>
        /// <param name="partition">The partition of the document to delete.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not found. Defaults to <c>true</c>.
        /// </param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The number of milliseconds to wait before retrying the operation in case the server responds with HTTP 429. This value 
        /// is only used if no other information is received with the HTTP 429 response.
        /// </param>
        public static async Task<ResponseMessage> DeleteDocumentAsync(this Container container, string id, PartitionKey partition, bool throwIfNotfound = true, int defaultRetryAfterMilliseconds = 5000)
        {
            TimeSpan delay = TimeSpan.Zero;
            bool retry = false;
            ResponseMessage response = null!;

            do
            {
                try
                {
                    response = await container.DeleteItemStreamAsync(id, partition);
                    retry = await response.HandleResponseAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds, throwIfNotFound: throwIfNotfound);
                }
                catch(CosmosException ex)
                {
                    retry = await ex.HandleExceptionAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds, throwIfNotFound: throwIfNotfound);
                }
            } while(retry);

            return response;
        }

        /// <summary>
        /// Deletes the document with the given <paramref name="id"/> and <paramref name="partitionKey"/>.
        /// </summary>
        /// <param name="container">The container to delete the document from.</param>
        /// <param name="id">The ID of the document to delete.</param>
        /// <param name="partitionKey">The partition key of the document to delete.</param>
        /// <param name="throwIfNotFound">Specifies whether to throw an exception if the specified document is not found.</param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The default amount of milliseconds to wait in case the server responds with HTTP status 492 (too many requests) 
        /// and that response does not contain a specified interwal to wait before retrying the operation.
        /// </param>
        public static Task<ResponseMessage> DeleteDocumentAsync(this Container container, string id, string? partitionKey, bool throwIfNotFound = true, int defaultRetryAfterMilliseconds = 5000)
        {
            return container.DeleteDocumentAsync(id, partitionKey?.Length > 0 ? new PartitionKey(partitionKey) : PartitionKey.None, throwIfNotfound: throwIfNotFound, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
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

        /// <summary>
        /// Returns whether the given status code indicates a successful operation.
        /// </summary>
        public static bool IsSuccess(this HttpStatusCode statusCode)
        {
            int code = (int)statusCode;
            return code >= 200 && code < 300;
        }

        /// <summary>
        /// Executes the given <paramref name="query"/> and returns the results as a collection of <see cref="JsonElement"/> objects.
        /// </summary>
        /// <param name="container">The container to query.</param>
        /// <param name="query">The query to execute.</param>
        /// <param name="defaultRetryAfterMilliseconds">The default time to wait in case the server responds with HTTP 429 if no other information is available in the response.</param>
        /// <remarks>
        /// You can use the <see cref="QueryDefinitionBuilder"/> class to build the value for the <paramref name="query"/> parameter.
        /// </remarks>
        public static async IAsyncEnumerable<JsonElement> QueryDocumentsAsync(this Container container, QueryDefinition query, int defaultRetryAfterMilliseconds = 5000)
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

        /// <summary>
        /// Executes the given <paramref name="query"/> and returns the results as a collection of <typeparamref name="TDocument"/> objects.
        /// </summary>
        /// <typeparam name="TDocument">The type of document to return the query results as.</typeparam>
        /// <param name="container">The container to query.</param>
        /// <param name="query">The query to execute.</param>
        /// <param name="defaultRetryAfterMilliseconds">The default time to wait in case the server responds with HTTP 429 if no other information is available in the response.</param>
        /// <param name="serializationOptions">Optional serialization options to use when deserializing a <see cref="JsonElement"/> to the type specified in <typeparamref name="TDocument"/>.</param>
        /// <remarks>
        /// You can use the <see cref="QueryDefinitionBuilder"/> class to build the value for the <paramref name="query"/> parameter.
        /// </remarks>
        public static async IAsyncEnumerable<TDocument> QueryDocumentsAsync<TDocument>(this Container container, QueryDefinition query, int defaultRetryAfterMilliseconds = 5000, JsonSerializerOptions? serializationOptions = null)
        {
            var options = serializationOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            await foreach(var item in container.QueryDocumentsAsync(query, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds))
            {
                var resultItem = JsonSerializer.Deserialize<TDocument>(item, options: options);
                if(null != resultItem)
                {
                    yield return resultItem;
                }
            }
        }

        /// <summary>
        /// Enumerates the async enumerable collection and returns it as a list object.
        /// </summary>
        /// <typeparam name="TDocument">The type of items in the resulting list.</typeparam>
        /// <param name="items">The items to produce a list from.</param>
        public static async Task<IList<TDocument>> ToListAsync<TDocument>(this IAsyncEnumerable<TDocument> items)
        {
            var list = new List<TDocument>();

            await foreach(var item in items)
            {
                list.Add(item); 
            }

            return list;
        }

        /// <summary>
        /// Upserts (updates or inserts) the given <paramref name="document"/> into the container.
        /// </summary>
        /// <param name="container">The container to upsert into.</param>
        /// <param name="document">The document to upsert.</param>
        /// <param name="partitionKey">An optional partition key to specify for the document.</param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The default number of milliseconds to wait before retrying in case the server returns HTTP 429
        /// and no other information is available in the response.
        /// </param>
        public static async Task<ItemResponse<object>> UpsertDocumentAsync(this Container container, object document, PartitionKey? partitionKey = null, int defaultRetryAfterMilliseconds = 5000)
        {
            return await container.UpsertDocumentAsync<object>(document, partitionKey: partitionKey, defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
        }

        /// <summary>
        /// Upserts (updates or inserts) the given <paramref name="document"/> into the container.
        /// </summary>
        /// <typeparam name="TDocument">The type of the document to upsert.</typeparam>
        /// <param name="container">The container to upsert into.</param>
        /// <param name="document">The document to upsert.</param>
        /// <param name="partitionKey">An optional partition key to specify for the document.</param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The default number of milliseconds to wait before retrying in case the server returns HTTP 429
        /// and no other information is available in the response.
        /// </param>
        public static async Task<ItemResponse<TDocument>> UpsertDocumentAsync<TDocument>(this Container container, TDocument document, PartitionKey? partitionKey = null, int defaultRetryAfterMilliseconds = 5000)
        {
            bool retry = false;
            ItemResponse<TDocument> response = null!;

            do
            {
                try
                {
                    response = await container.UpsertItemAsync(document, partitionKey);
                    retry = await response.HandleResponseAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                }
                catch (CosmosException ex)
                {
                    retry = await ex.HandleExceptionAsync(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                }

            } while (retry);

            return response;
        }


        /// <summary>
        /// Handles the given response by checking for a HTTP 429 status code.
        /// </summary>
        /// <typeparam name="TResource"></typeparam>
        /// <param name="response">The response to examine.</param>
        /// <param name="defaultRetryAfterMilliseconds">The default delay to wait for in case the response contained HTTP status code 429.</param>
        /// <returns>Returns <c>true</c> if a retry delay was found in the response.</returns>
        /// <remarks>
        /// If a retry after delay was found in the response, this method will wait for that amount of time before returning.
        /// </remarks>
        private static async Task<bool> HandleResponseAsync<TResource>(this Response<TResource>? response, int defaultRetryAfterMilliseconds = 5000)
        {
            if(null != response)
            {
                if(response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = response.Headers.ReadRetryAfterDelay(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                    await Task.Delay(delay);
                    return true;
                }
            }
            return false;
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
        private static async Task<bool> HandleResponseAsync(this ResponseMessage? response, int defaultRetryAfterMilliseconds = 5000, bool throwIfNotFound = true)
        {
            if(null != response)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    TimeSpan delay = response.Headers.ReadRetryAfterDelay(defaultRetryAfterMilliseconds: defaultRetryAfterMilliseconds);
                    await Task.Delay(delay);
                    return true;
                }
                else if(response.StatusCode == HttpStatusCode.NotFound)
                {
                    if (throwIfNotFound)
                    {
                        throw new Exception($"An item was not found. Status: {response.StatusCode}.");
                    }
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Unexpected status code received from Cosmos DB. Status code: {response.StatusCode}");
                }
            }

            return false;
        }

        /// <summary>
        /// Reads the <c>Retry-After</c> header from the given <paramref name="headers"/> and returns that information as
        /// a <see cref="TimeSpan"/> struct.
        /// </summary>
        /// <param name="headers">The headers to read from.</param>
        /// <param name="defaultRetryAfterMilliseconds">The default delay if no <c>Retry-After</c> header is found.</param>
        /// <remarks>
        /// If the current headers do not contain a <c>Retry-After</c> header value, the default <paramref name="defaultRetryAfterMilliseconds"/>
        /// is used as delay.
        /// </remarks>
        private static TimeSpan ReadRetryAfterDelay(this Headers? headers, int defaultRetryAfterMilliseconds = 5000)
        {
            TimeSpan delay = TimeSpan.FromMilliseconds(defaultRetryAfterMilliseconds);
            if(null != headers)
            {
                var retryAfter = headers["Retry-After"];
                if(RetryConditionHeaderValue.TryParse(retryAfter, out var h))
                {
                    delay = h.Delta ?? delay;
                }
            }
            return delay;
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
