# Denomica.Cosmos.Extensions

A library that facilitates working with data in Azure Cosmos DB using the [.NET SDK v3](https://docs.microsoft.com/azure/cosmos-db/sql/sql-api-sdk-dotnet-standard)

## Further Reading

For more information, check out the [library wiki](https://github.com/Denomica/Denomica.Cosmos.Extensions/wiki) for this library.

## Improvements

Major improvements in various versions.

### v1.0.1

Just updated the version number from 1.0.0.1 to 1.0.1 to better be in line with [SemVer 2](https://semver.org/).

### v1.1.0

Exposed the `Parameters` dictionary on the `QueryDefinitionBuilder` class to have a better control on what parameters have already been added to the builder.

### v1.2.0

Introduced a set of model classes that you can use as base for your data model classes when storing data in Cosmos DB. Read more about data modelling support on the [wiki](https://github.com/Denomica/Denomica.Cosmos.Extensions/wiki/Data-Modelling).

### v1.2.1

Added support for more advanced format strings in the `PartitionKeyPropertyAttribute`.