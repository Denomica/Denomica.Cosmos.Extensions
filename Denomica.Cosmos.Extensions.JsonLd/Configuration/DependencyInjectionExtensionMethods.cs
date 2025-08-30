using Denomica.Cosmos.Extensions.Configuration;
using Denomica.Cosmos.Extensions.JsonLd.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.JsonLd.Configuration
{
    /// <summary>
    /// Provides extension methods for configuring services in a <see cref="CosmosExtensionsBuilder"/>  with additional
    /// functionality, such as JSON-LD defaults and custom data normalizers.
    /// </summary>
    /// <remarks>
    /// These extension methods are designed to simplify the configuration of services in the
    /// <see cref="CosmosExtensionsBuilder"/> pipeline. They allow developers to add default JSON-LD 
    /// processing behavior.
    /// </remarks>
    public static class DependencyInjectionExtensionMethods
    {
        /// <summary>
        /// Configures default services and settings for using the JSON-LD extensions.
        /// </summary>
        public static CosmosExtensionsBuilder WithJsonLdDefaults(this CosmosExtensionsBuilder builder)
        {
            return builder
                .WithDataNormalizer<DataNormalizer>(sp => new DataNormalizer())
                ;
        }

        /// <summary>
        /// Registers a custom data normalizer in the dependency injection container.
        /// </summary>
        /// <remarks>
        /// This method adds the specified data normalizer to the service collection with a scoped lifetime.
        /// The data normalizer is resolved as a <see cref="DataNormalizer"/> instance.
        /// </remarks>
        /// <typeparam name="TNormalizer">The type of the data normalizer to register. Must derive from <see cref="DataNormalizer"/>.</typeparam>
        /// <param name="builder">The <see cref="CosmosExtensionsBuilder"/> used to configure Cosmos extensions.</param>
        /// <param name="serviceBuilder">
        /// A factory function that creates an instance of <typeparamref name="TNormalizer"/> using the provided
        /// <see cref="IServiceProvider"/>.
        /// </param>
        /// <returns>The <see cref="CosmosExtensionsBuilder"/> instance, allowing for method chaining.</returns>
        public static CosmosExtensionsBuilder WithDataNormalizer<TNormalizer>(this CosmosExtensionsBuilder builder, Func<IServiceProvider, TNormalizer> serviceBuilder) where TNormalizer : DataNormalizer
        {
            builder.Services.AddScoped<DataNormalizer>(serviceBuilder);
            return builder;
        }
    }
}
