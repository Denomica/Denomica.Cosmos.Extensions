using Denomica.Cosmos.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides extension methods for configuring dependency injection services related to Cosmos functionality.
    /// </summary>
    /// <remarks>This static class contains methods that simplify the registration of Cosmos-related services
    /// into an <see cref="IServiceCollection"/> for dependency injection.</remarks>
    public static class DependencyInjectionConfigurationMethods
    {
        /// <summary>
        /// Adds Cosmos DB extension services to the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the Cosmos DB extension services will be added. Cannot be <see
        /// langword="null"/>.</param>
        /// <returns>A <see cref="CosmosExtensionsBuilder"/> that can be used to configure the Cosmos DB extensions.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is <see langword="null"/>.</exception>
        public static CosmosExtensionsBuilder AddCosmosExtensions(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return new CosmosExtensionsBuilder(services);
        }
    }
}
