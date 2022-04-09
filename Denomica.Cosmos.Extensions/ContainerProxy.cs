using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http.Headers;
using System.Linq.Expressions;
using System.Linq;
using Microsoft.Azure.Cosmos.Linq;
using System.IO;
using Denomica.Text.Json;

namespace Denomica.Cosmos.Extensions
{

    /// <summary>
    /// A wrapper class for working with data stored in a Cosmos DB <see cref="Container"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The main feature that this wrapper class provides is that it can handle HTTP 429 responses (too many requests) from 
    /// Cosmos DB. The Cosmos DB SDK does not handle all of those responses, and under certain circumstances you might end up 
    /// getting HTTP 429 statuses to your Cosmos DB requests. This occurs especially when under heavy load running on the 
    /// upper limit of your configured throughput.
    /// </para>
    /// <para>
    /// Sometimes it is just best to try and patiently retry the request until the request succeeds, and this is where this class
    /// comes in handy.
    /// </para>
    /// </remarks>
    public class ContainerProxy
    {
        /// <summary>
        /// Creates a new instance of the proxy class.
        /// </summary>
        /// <param name="container">The Cosmos DB container to wrap in this class.</param>
        /// <param name="serializationOptions">
        /// Optional JSON serialization options that are used by this wrapper when serializing and deserializing.
        /// </param>
        /// <param name="defaultRetryAfterMilliseconds">
        /// The default number of milliseconds to wait before retrying an operation that produced a response with 
        /// HTTP 429 status. This value is only used if no other information is available with the HTTP 429 response.
        /// </param>
        /// <param name="maxRetryCount">
        /// The maximum number of retries allowed when retrying HTTP 429 responses.
        /// </param>
        /// <exception cref="ArgumentNullException">The exception that is thrown if <paramref name="container"/> is <c>null</c>.</exception>
        public ContainerProxy(Container container, JsonSerializerOptions? serializationOptions = null, int defaultRetryAfterMilliseconds = 5000)
        {
            this.Container = container ?? throw new ArgumentNullException(nameof(container));
            this.SerializationOptions = serializationOptions ?? new JsonSerializerOptions(JsonSerializerDefaults.Web);
            this.DefaultRetryAfterMilliseconds = defaultRetryAfterMilliseconds;
        }

        /// <summary>
        /// The container that was specified in the constructor.
        /// </summary>
        public Container Container { get; private set; }

        /// <summary>
        /// The serialization options used by the wrapper class.
        /// </summary>
        public JsonSerializerOptions SerializationOptions { get; private set; }

        /// <summary>
        /// The default amount of milliseconds used to wait before retrying a request that 
        /// produced a HTTP 429 response unless no other information is available with the response.
        /// </summary>
        public int DefaultRetryAfterMilliseconds { get; private set; }



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
            if(null != value)
            {
                if (value is string)
                {
                    key = new PartitionKey((string)value);
                }
                else if(value is Guid)
                {
                    key = new PartitionKey($"{value}");
                }
                else if (value is double)
                {
                    key = new PartitionKey((double)value);
                }
                else if(double.TryParse($"{value}", out double d))
                {
                    key = new PartitionKey(d);
                }
                else if (value is bool)
                {
                    key = new PartitionKey((bool)value);
                }
                else if(bool.TryParse($"{value}", out bool b))
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

        /// <summary>
        /// Deletes the item with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <param name="partition">The partition of the item to delete.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not
        /// found. Defaults to <c>true</c>.
        /// </param>
        public async Task DeleteItemAsync(string id, PartitionKey partition, bool throwIfNotfound = true)
        {
            bool retry = false;
            int retryCount = 0;
            ResponseMessage response = null!;

            do
            {
                try
                {
                    response = await this.Container.DeleteItemStreamAsync(id, partition);
                    retry = await this.HandleResponseAsync(response, retryCount, throwIfNotFound: throwIfNotfound);
                }
                catch (CosmosException ex)
                {
                    retry = await this.HandleExceptionAsync(ex, retryCount, throwIfNotFound: throwIfNotfound);
                }
                retryCount++;
            } while (retry);

            if(null != response)
            {
                response.EnsureSuccessStatusCode();
            }
            else
            {
                throw new Exception("Unable to get a response for the delete operation.");
            }
        }

        /// <summary>
        /// Deletes the document with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        /// <param name="partition">The partition of the document to delete.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not found. Defaults to <c>true</c>.
        /// </param>
        public Task DeleteItemAsync(string id, string? partition, bool throwIfNotfound = true)
        {
            return this.DeleteItemAsync(id, partition?.Length > 0 ? new PartitionKey(partition) : PartitionKey.None, throwIfNotfound: throwIfNotfound);
        }

        /// <summary>
        /// Deletes the item with the given <paramref name="id"/> and <paramref name="partition"/>.
        /// </summary>
        /// <param name="id">The ID of the item to delete.</param>
        /// <param name="partition">The partition key of the item to delete. Set to <c>null</c> if the item is stored without a partition.</param>
        /// <param name="throwIfNotfound">
        /// Specifies whether to throw an exception if the specified document is not
        /// found. Defaults to <c>true</c>.
        /// </param>
        public Task DeleteItemAsync(string id, double? partition, bool throwIfNotFound = true)
        {
            return this.DeleteItemAsync(id, partition.HasValue ? new PartitionKey(partition.Value) : PartitionKey.None, throwIfNotfound: throwIfNotFound);
        }

        /// <summary>
        /// Returns a queryable object that can be used to query items with using the methods
        /// exposed on the <see cref="ContainerProxy"/> class.
        /// </summary>
        /// <typeparam name="TItem">The type of items in the resulting collection.</typeparam>
        public IOrderedQueryable<TItem> GetItemLinqQueryable<TItem>()
        {
            return this.Container.GetItemLinqQueryable<TItem>();
        }

        /// <summary>
        /// Returns the first item matching the <paramref name="query"/> or <c>null</c> if no item is found.
        /// </summary>
        /// <param name="query">The query to use to find one item with.</param>
        public async Task<JsonElement?> FirstOrDefaultAsync(QueryDefinition query)
        {
            JsonElement? result = null;
            var items = this.QueryItemsAsync(query);
            await foreach(var item in items)
            {
                result = item;
                break;
            }
            return result;
        }

        /// <summary>
        /// Returns the first item matching the specified <paramref name="query"/>, or the default value for <typeparamref name="TItem"/> if no item matching the query is found.
        /// </summary>
        /// <typeparam name="TItem">The type to return the item as.</typeparam>
        /// <param name="query">The query to use to find one item with.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        public async Task<TItem> FirstOrDefaultAsync<TItem>(QueryDefinition query, Type? returnAs = null)
        {
            TItem result = default!;
            var items = this.QueryItemsAsync<TItem>(query, returnAs: returnAs);
            await foreach(var item in items)
            {
                result = item;
                break;
            }

            return result;
        }

        /// <summary>
        /// Returns the first item matching the given <paramref name="linqQuery"/>.
        /// </summary>
        /// <typeparam name="TItem">The type to return the item as.</typeparam>
        /// <param name="linqQuery">The query to use to find one item with.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        public async Task<TItem> FirstOrDefaultAsync<TItem>(IQueryable<TItem> linqQuery, Type? returnAs = null)
        {
            return await this.FirstOrDefaultAsync<TItem>(linqQuery.Take(1).ToQueryDefinition(), returnAs: returnAs);
        }

        /// <summary>
        /// Returns the first item matching the given <paramref name="linqQuery"/>.
        /// </summary>
        /// <typeparam name="TItem">The type to return the item as.</typeparam>
        /// <param name="linqQuery">The query to use to find one item with.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        public async Task<TItem> FirstOrDefaultAsync<TItem>(IOrderedQueryable<TItem> linqQuery, Type? returnAs = null)
        {
            return await this.FirstOrDefaultAsync<TItem>(linqQuery.Take(1).ToQueryDefinition(), returnAs: returnAs);
        }

        /// <summary>
        /// Queries the underlying <see cref="Container"/> for items.
        /// </summary>
        /// <param name="query">The query to execute.</param>
        /// <returns>Returns the results as an async enumerable collection.</returns>
        public async IAsyncEnumerable<JsonElement> QueryItemsAsync(QueryDefinition query)
        {
            string? continuationToken = null;

            do
            {
                ResponseMessage response = await this.GetResponseAsync(async () =>
                {
                    var iterator = this.Container.GetItemQueryStreamIterator(query, continuationToken: continuationToken);
                    if (iterator.HasMoreResults)
                    {
                        return await iterator.ReadNextAsync();
                    }

                    return null;
                });
                continuationToken = response.ContinuationToken;

                if (null != response?.Content)
                {
                    var json = await JsonDocument.ParseAsync(response.Content);
                    if (json.RootElement.TryGetProperty("Documents", out var docs) && docs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in docs.EnumerateArray())
                        {
                            yield return item;
                        }
                    }
                }
            } while (continuationToken?.Length > 0);

            yield break;
        }

        /// <summary>
        /// Queries the underlying <see cref="Container"/> for items.
        /// </summary>
        /// <typeparam name="TItem">The type to return the items as.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        /// <remarks>
        /// <para>
        /// You can use the <see cref="QueryDefinitionBuilder"/> helper to build the <paramref name="query"/>.
        /// </para>
        /// <para>
        /// The returned items are deserialized from a <see cref="JsonElement"/> object using the options
        /// specified in the constructor and stored in <see cref="SerializationOptions"/>.
        /// </para>
        /// </remarks>
        /// <returns>Returns the results as an async enumerable collection.</returns>
        public async IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(QueryDefinition query, Type? returnAs = null)
        {
            await foreach (var item in this.QueryItemsAsync(query))
            {
                TItem resultItem = default!;
                if(null == returnAs)
                {
                    resultItem = JsonSerializer.Deserialize<TItem>(item, options: this.SerializationOptions);
                }
                else
                {
                    resultItem = (TItem)JsonSerializer.Deserialize(item, returnAs, options: this.SerializationOptions);
                }
                if (null != resultItem)
                {
                    yield return resultItem;
                }
            }

        }

        /// <summary>
        /// Executes the given <paramref name="query"/> and return the results as an async enumerable collection.
        /// </summary>
        /// <typeparam name="TItem">The type for the items to return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        public IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(IOrderedQueryable<TItem> query, Type? returnAs = null)
        {
            return this.QueryItemsAsync<TItem>(query.ToQueryDefinition(), returnAs: returnAs);
        }

        /// <summary>
        /// Executes the given <paramref name="query"/> and return the results as an async enumerable collection.
        /// </summary>
        /// <typeparam name="TItem">The type for the items to return.</typeparam>
        /// <param name="query">The query to execute.</param>
        /// <param name="returnAs">
        /// An optional type that items returned will be converted to. Note that this type 
        /// MUST derive from the type specified in <typeparamref name="TItem"/>. If this
        /// parameter is not specified, the items are returned as the type specified
        /// in <typeparamref name="TItem"/>.
        /// </param>
        public IAsyncEnumerable<TItem> QueryItemsAsync<TItem>(IQueryable<TItem> query, Type? returnAs = null)
        {
            return this.QueryItemsAsync<TItem>(query.ToQueryDefinition(), returnAs: returnAs);
        }

        /// <summary>
        /// Upserts the given item to the underlying <see cref="Container"/>.
        /// </summary>
        /// <param name="item">The item to upsert.</param>
        /// <param name="partitionKey">Optional partition key to use when storing the item in the underlying <see cref="Container"/>.</param>
        public async Task<object> UpsertItemAsync(object item, PartitionKey? partitionKey = null)
        {
            return await this.UpsertItemAsync<object>(item, partitionKey: partitionKey);
        }

        /// <summary>
        /// Upserts the given item to the underlying <see cref="Container"/>.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="item">The item to upsert.</param>
        /// <param name="partitionKey">Optional partition key to use when storing the item in the underlying <see cref="Container"/>.</param>
        public async Task<TItem> UpsertItemAsync<TItem>(TItem item, PartitionKey? partitionKey = null)
        {

            if(null == item) throw new ArgumentNullException(nameof(item));

            bool retry = false;
            ItemResponse<object> response = null!;
            int retryCount = 0;
            TItem upserted = default!;

            do
            {
                try
                {
                    response = await this.Container.UpsertItemAsync<object>(item, partitionKey);
                    retry = await this.HandleResponseAsync(response, retryCount);
                }
                catch (CosmosException ex)
                {
                    retry = await this.HandleExceptionAsync(ex, retryCount);
                }

                retryCount++;
            } while (retry);

            if(null != response)
            {
                if(response.StatusCode.IsSuccess())
                {
                    /*
                     * The response must be handled this way to ensure that we return the response as the 
                     * same type as the item parameter is.
                     * 
                     * Note that the type of item can also be a type derifed from TItem. If we would use
                     * the standard functionality provided by the Cosmos DB SDK, the response from the
                     * server would be typed as TItem, not as a derived type.
                     * 
                     * That's why we upsert the item as an object in the UpsertItemAsync method call above,
                     * and then serialize the response, and deserialize to the type of the item parameter.
                     */
                    object resource = response.Resource is Newtonsoft.Json.Linq.JToken
                        ? ((Newtonsoft.Json.Linq.JToken)response.Resource).ToJsonDictionary()
                        : response.Resource;

                    using (var strm = new MemoryStream())
                    {
                        await JsonSerializer.SerializeAsync(strm, resource, options: this.SerializationOptions);
                        strm.Position = 0;

                        try
                        {
                            upserted = (TItem)await JsonSerializer.DeserializeAsync(strm, item.GetType(), options: this.SerializationOptions);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"Unable to deserialize the upserted item to type '{typeof(TItem).FullName}'.", ex);
                        }
                    }

                }
                else
                {
                    throw new Exception($"Unexpected status code received from server. Status code: {response.StatusCode}.");
                }
            }
            else
            {
                throw new Exception("Unable to get a response from the server.");
            }

            return upserted!;
        }



        /// <summary>
        /// Uses the <paramref name="responseProvider"/> delegate to get a response, optionally retrying until the
        /// <paramref name="responseProvider"/> returns a valid response.
        /// </summary>
        /// <param name="responseProvider">A delegate that will return the response to examine.</param>
        /// <returns></returns>
        /// <exception cref="Exception">The exception that is thrown if <paramref name="responseProvider"/> returns <c>null</c>.</exception>
        private async Task<ResponseMessage> GetResponseAsync(Func<Task<ResponseMessage?>> responseProvider)
        {
            ResponseMessage? response = null!;
            bool retry = false;
            int retryCount = 0;

            do
            {
                try
                {
                    response = await responseProvider();
                    retry = await this.HandleResponseAsync(response, retryCount);
                }
                catch (CosmosException ex)
                {
                    retry = await this.HandleExceptionAsync(ex, retryCount);
                }

                retryCount++;
            } while (retry);

            if (null == response)
            {
                throw new Exception("Unable to resolve response.");
            }
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
        private async Task<bool> HandleResponseAsync<TResource>(Response<TResource>? response, int retryCount)
        {
            if (null != response)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = this.ReadRetryAfterDelay(response.Headers);
                    await this.WaitAsync(delay, retryCount);
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
        private async Task<bool> HandleResponseAsync(ResponseMessage? response, int retryCount, int defaultRetryAfterMilliseconds = 5000, bool throwIfNotFound = true)
        {
            if (null != response)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    var delay = this.ReadRetryAfterDelay(response.Headers);
                    await this.WaitAsync(delay, retryCount);
                    return true;
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
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
        /// Handles the exception if it was thrown from HTTP status 429.
        /// </summary>
        /// <param name="ex">The exception to handle.</param>
        /// <returns>Returns <c>true</c> if the exception was associated with HTTP 429 and the operation that threw the exception needs to be retried.</returns>
        private async Task<bool> HandleExceptionAsync(CosmosException? ex, int retryCount, bool throwIfNotFound = true)
        {
            if (null != ex)
            {
                if (ex.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    await this.WaitAsync(ex.RetryAfter, retryCount);
                    return true;
                }
                else if (ex.StatusCode == HttpStatusCode.NotFound)
                {
                    if (throwIfNotFound)
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
        private TimeSpan? ReadRetryAfterDelay(Headers? headers)
        {
            TimeSpan? delay = null;
            if (null != headers)
            {
                var retryAfter = headers["Retry-After"];
                if (RetryConditionHeaderValue.TryParse(retryAfter, out var h))
                {
                    delay = h.Delta ?? delay;
                }
            }
            return delay;
        }

        /// <summary>
        /// Waits for the given timespan to pass, optionally using <paramref name="retryCount"/> as factor for modifying the delay.
        /// </summary>
        /// <param name="delay">The delay to wait before the method returns.</param>
        /// <param name="retryCount">The number of times an operation has been retried.</param>
        private Task WaitAsync(TimeSpan? delay, int retryCount)
        {
            var actualDelay = delay ?? TimeSpan.FromMilliseconds(this.DefaultRetryAfterMilliseconds);
            return Task.Delay(actualDelay);
        }

    }
}
