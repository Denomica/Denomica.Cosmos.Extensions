# Denomica.Cosmos.Extensions.JsonLd

A library that facilitates working with JSON-LD data and store that in an Azure Cosmos DB container. The primary focus is on objects defined by [Schema.org](https://schema.org), but not restricted to that. The main features of this library are:

- Provides an envelope (JsonLdObject) to wrap around your JSON-LD data.
- The envelope takes care of the requirements for storing data in Cosmos DB, i.e. having an `id` and a `partitionKey` property.
- Normalization - Schema.org is pretty flexible. For instance, it allows properties to be either a single value/object or an array of values/objects. The library normalizes all properties to be contained in arrays in order to make it easier to construct SQL queries against the data.

## Further Reading

For more information, check out the [library wiki](https://github.com/Denomica/Denomica.Cosmos.Extensions/wiki) for this library.

## Version Highlights

The most prominent changes in various versions.

### v1.0.0-beta.1

The initial beta release of this library.
