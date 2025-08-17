using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Denomica.Cosmos.Extensions.Configuration
{
    /// <summary>
    /// Provides a builder for configuring Cosmos-related services in an application.
    /// </summary>
    /// <remarks>This class is used to configure and register services related to Cosmos DB within the
    /// provided <see cref="IServiceCollection"/>. It is typically used in application startup to set up Cosmos-specific
    /// dependencies.</remarks>
    public class CosmosExtensionsBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CosmosExtensionsBuilder"/> class.
        /// </summary>
        /// <param name="services">The collection of service descriptors to configure Cosmos-related services.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
        public CosmosExtensionsBuilder(IServiceCollection services)
        {
            this.Services = services ?? throw new ArgumentNullException(nameof(services));
        }


        /// <summary>
        /// Gets the collection of service descriptors used to configure dependency injection.
        /// </summary>
        public IServiceCollection Services { get; private set; }



        /// <summary>
        /// Configures the connection options for connecting to a Cosmos DB instance.
        /// </summary>
        /// <remarks>This method allows customization of the Cosmos DB connection options by providing a
        /// delegate that modifies the <see cref="CosmosConnectionOptions"/> object. The delegate receives an <see
        /// cref="IServiceProvider"/> to enable dependency injection for advanced configuration scenarios.</remarks>
        /// <param name="configureOptions">A delegate that configures an instance of <see cref="CosmosConnectionOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current <see cref="CosmosExtensionsBuilder"/> instance, allowing for method chaining.</returns>
        public CosmosExtensionsBuilder WithConnectionOptions(Action<CosmosConnectionOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<CosmosConnectionOptions>()
                .Configure<IServiceProvider>(configureOptions)
                .Services

                .AddScoped<CosmosClient>(sp =>
                {
                    var connectionOptions = sp.GetRequiredService<IOptions<CosmosConnectionOptions>>().Value;
                    var clientOptions = sp.GetRequiredService<IOptions<CosmosClientOptions>>().Value;
                    return new CosmosClient(connectionOptions.ConnectionString, clientOptions);
                })
                .AddScoped<ContainerProxy>(sp =>
                {
                    var connectionOptions = sp.GetRequiredService<IOptions<CosmosConnectionOptions>>().Value;
                    var client = sp.GetRequiredService<CosmosClient>();
                    return new ContainerProxy(client, connectionOptions.DatabaseId, connectionOptions.ContainerId);
                })
                ;

            return this;
        }

        /// <summary>
        /// Configures the <see cref="CosmosClientOptions"/> for the Cosmos DB client.
        /// </summary>
        /// <remarks>Use this method to customize the <see cref="CosmosClientOptions"/> for the Cosmos DB
        /// client, such as setting connection policies, retry options, or other client-specific
        /// configurations.</remarks>
        /// <param name="configureOptions">A delegate that configures the <see cref="CosmosClientOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current <see cref="CosmosExtensionsBuilder"/> instance, allowing for method chaining.</returns>
        public CosmosExtensionsBuilder WithCosmosClientOptions(Action<CosmosClientOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<CosmosClientOptions>()
                .Configure<IServiceProvider>(configureOptions)
                ;

            return this;
        }

        /// <summary>
        /// Configures JSON serialization options for the application.
        /// </summary>
        /// <remarks>This method allows customization of JSON serialization behavior by modifying the <see
        /// cref="JsonSerializerOptions"/> used throughout the application. The provided delegate is invoked to apply
        /// the desired configuration.</remarks>
        /// <param name="configureOptions">A delegate that configures an instance of <see cref="JsonSerializerOptions"/> using the provided <see
        /// cref="IServiceProvider"/>.</param>
        /// <returns>The current instance of <see cref="CosmosExtensionsBuilder"/> to allow for method chaining.</returns>
        public CosmosExtensionsBuilder WithJsonSerializationOptions(Action<JsonSerializerOptions, IServiceProvider> configureOptions)
        {
            this.Services
                .AddOptions<JsonSerializerOptions>()
                .Configure<IServiceProvider>(configureOptions)
                ;

            return this;
        }

    }
}
