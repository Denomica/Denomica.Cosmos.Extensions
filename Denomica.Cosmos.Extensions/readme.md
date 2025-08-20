# Denomica.Cosmos.Extensions

A library that facilitates working with data in Azure Cosmos DB using the [.NET SDK v3](https://docs.microsoft.com/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard)

## Further Reading

For more information, check out the [library wiki](https://github.com/Denomica/Denomica.Cosmos.Extensions/wiki) for this library.

## Improvements

Major improvements in various versions.

### v1.2.7

Updated reference to [`Denomica.Cosmos.Extensls.Model`](https://www.nuget.org/packages/Denomica.Cosmos.Extensions.Model/) to v1.2.4, which contains a fix that better handles illegal characters in the `DocumentBase.Id` property.

### v.1.2.6

Added support for Dependency Injection with the `IServiceCollection.AddCosmosExtensions()` extension method, and the `CosmosExtensionsBuilder` class.

### v1.2.5

Added a few extra overloaded methods to the `QueryDefinitionBuilder` class.

### v1.2.4

Updated a few Nuget packages, one of which had a vulnerable version.

### v1.2.3

Included XML documentation file in the package to provide Intellisense documentation.

### v1.2.2

Extracted model classes into separate package [Denomica.Cosmos.Extensions.Model](https://www.nuget.org/packages/Denomica.Cosmos.Extensions.Model). This enables using the model classes in client applications not directly using Cosmos DB, but accesses the data through a REST API for instance.

### v1.2.1

Added support for more advanced format strings in the `PartitionKeyPropertyAttribute`.

### v1.2.0

Introduced a set of model classes that you can use as base for your data model classes when storing data in Cosmos DB. Read more about data modelling support on the [wiki](https://github.com/Denomica/Denomica.Cosmos.Extensions/wiki/Data-Modelling).

### v1.1.0

Exposed the `Parameters` dictionary on the `QueryDefinitionBuilder` class to have a better control on what parameters have already been added to the builder.

### v1.0.1

Just updated the version number from 1.0.0.1 to 1.0.1 to better be in line with [SemVer 2](https://semver.org/).





