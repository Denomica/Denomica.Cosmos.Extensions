using System;
using System.Collections.Generic;
using System.Text;

namespace Denomica.Cosmos.Extensions.Configuration
{
    /// <summary>
    /// Represents the configuration options required to establish a connection to an Azure Cosmos DB instance.
    /// </summary>
    /// <remarks>This class encapsulates the necessary details for connecting to a specific database and
    /// container within an Azure Cosmos DB account. Use these options to configure a Cosmos DB client.</remarks>
    public class CosmosConnectionOptions
    {
        /// <summary>
        /// Gets or sets the connection string used to establish a connection to the database.
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the database.
        /// </summary>
        public string DatabaseId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the unique identifier for the container.
        /// </summary>
        public string ContainerId { get; set; } = string.Empty;
    }
}
