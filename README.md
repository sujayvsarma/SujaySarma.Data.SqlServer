# Sujay Sarma's SQL Server Client SDK
---

(SQL Server, SQL Express, LocalDB, SQL Azure, SQL Server on Linux compatible)

Library                      | Current version
-----------------------------|------------------
SujaySarma.Data.SqlServer    | Version 8.5.0


### Changelog

Version | Changes
--------|----------
8.5.0 | Added `UpsertAsync` method to support SQL MERGE operations. We have also added a new `DEBUG` flag -- project must be compiled in `DEBUG` mode, set an environment variable named `SQLTABLECONTEXT_DUMPSQL` (value can be anything). This causes SQL statements to be dumped to the active `CONSOLE`.
8.2.0 | New method `ExecuteStoredProcedure` added to support stored procedure execution. Has other performance improvements and bug fixes.
8.0.0 | Initial version. Supports synchronous and async operations against SQL databases, with full object/data mapping support similar to our 'Azure Tables Client SDK' package.

*Some notes about `SQLTABLECONTEXT_DUMPSQL`* 

1. This will make your output very chatty and will decrease performance by a slight amount due to console writes.
2. SQL that is output is NOT sanitized and WILL leak sensitive information.
3. ALL the SQL that is passed through the library is output.

Therefore, use this ONLY while debugging and turn it OFF immediately.

---

## About this library
This library simplifies writing data storage and retrieval code against databases hosted on Microsoft SQL Server technologies. You 
no longer need to use cumbersome frameworks like EntityFramework (EF) to simplify or automate your database interaction and ORM process.

This library is built on the same lines as my popular [Azure Tables Client SDK](https://www.nuget.org/packages/SujaySarma.Data.Azure.Tables/) and 
offers a highly simplified and super-performant structure.

## Dependencies
This package depends on the 'Microsoft.Data.SqlClient' library. And uses 'System.Data' and 'System.Reflection' extensively.

## Dependability and Trustworthiness

- The codebase is mostly the same as used in the 'Azure Tables Client SDK' (see link above).
- Given that the Azure Tables Client SDK is heavily performance optimized, all the same learnings have been directly applied here.
- A readonly copy of source code is available on request. Drop me an email from the Contact Owners page.

## Usage
Write your .NET business object as a `class` or `record`. For example:

```
public class Person
{
	public string LastName { get; set; }

	public string FirstName { get; set; }

	public DateTime DateOfBirth { get; set; }

	//...
}
```

Let's say you want to store this object in an SQL Server named `People`, add the following to the top 
of your class file:

*IMPORTANT:* You must create the database and tables beforehand. The SDK will NOT automatically create or manage them for you 
(unlike the Azure Tables Client SDK) as SQL Server object structure is much more complex than we want to deal with in this library. 

```
using SujaySarma.Data.SqlServer.Attributes;
```

and add this attribute above your class declaration:

```
[Table("People")]
public class Person ...
```

You ideally need at least one primary key for this class/table. To specify these fields, add the relevant attributes from 
our `SujaySarma.Data.SqlServer.Attributes` namespace. Let's define our `LastName` property as the `Primary Key`:

```
[TableColumn("LastName", KeyBehaviour = KeyBehaviourEnum.PrimaryKey)]
public string LastName { get; set; }
```

Just for fun, let's add a property to this class named `Hobbies` as a `List<string>`. We cannot store such types directly 
in the underlying table. Therefore, we need to tell the SDK to magically serialize it to Json and store that Json in the table.

```
[TableColumn("Hobbies", JsonSerialize = true)]
public List<string> Hobbies { get; set; } = new();
```

The completed class definition becomes:

```
[Table("People")]
public class Person
{
	[TableColumn("LastName", KeyBehaviour = KeyBehaviourEnum.PrimaryKey)]
	public string LastName { get; set; }

	[TableColumn("FirstName")]
	public string FirstName { get; set; }

	[TableColumn("DateOfBirth")]
	public DateTime DateOfBirth { get; set; }

	[TableColumn("Hobbies", JsonSerialize = true)]
	public List<string> Hobbies { get; set; } = new();
}
```

Let's instantiate an item and provide some data:

```
Person p = new() 
{
	LastName = "Doe",
	FirstName = "John",
	DateOfBirth = new DateTime(2000, 1, 1),
	Hobbies = new() { "Painting", "Reading" }
};
```

Now, we need to store this in our tables. The library provides the class `SqlTableContext`, which provides a "connection-less" paradigm 
to operate against the underlying Tables API. Initialize it with the connection string from above.

```
SqlTableContext tablesContext = new("<connection string>");
```

Of course, you can store the connection string in your Environment Variables or an appsettings.json or the KeyVault or wherever and 
use it from there.

You can also use the fluid-style initializers:

1. To initialize with local system database: 

```
SqlTableContext tablesContext = SqlTableContext.WithLocalDatabase("TempDB");
```

2. To initialize with provided connection string:

```
SqlTableContext tablesContext = SqlTableContext.WithConnectionString("<connection string>");
```


Insert the item we created into this storage (the method is `async`):

```
await tablesContext.InsertAsync<Person>(p);
```

That's it. Open up the table in your SQL Server Studio, you will find the `People` table there with the data you 
set above. Notice how the value of the `Hobbies` column is the Json-serialized value of the .NET property.

To read it back:

```
Person? p2 = tablesContext.Select<Person>("SELECT * FROM People WHERE ([LastName] = 'Doe');");
```

The return of that call will be a Null if it could not find the information requested. Examine the values of the properties in `p2` to 
confirm that all the values you stored earlier have been retrieved correctly. 

## Asynchronous operations

As of v8.x, the SDK supports `async` operations via the new `xxxAsync()` methods such as `InsertAsync` in the `SqlTableContext` class. However, 
do note that the Async fetch operations (eg: `SelectAsync<T>`) will return `IAsyncEnumerable<T>` and you need to use your own `async foreach()` for example 
to loop through and fetch the results. To help, we do provide two extension methods (they are in the `AzureTablesContextExtensions` class but will attach to 
any `IAsyncEnumerable<T>` instance):

1. `async Task<bool> AnyAsync<T>(this IAsyncEnumerable<T> e, Predicate<T>? validation = null)`

This method checks if any item in the `IAsyncEnumerable` matches the provided condition in the same way as the `System.Linq` method `Any()` works.

2. `async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> e)`

This method accepts an `IAsyncEnumerable<T>` and returns a `List<T>`. It performs the `await foreach()` for you.

---

## Some typical gotchas

- When storing arrays/lists/collections of Enums as serialized Json, ensure you set the `JsonConverter(typeof(JsonStringEnumConverter))` 
attribute on the `enum` **definition** and **not** on the property! You set the converter attribute on the property only if it is a 
single value (not an array/collection).

---

### Powerful features

- You can store almost any type into a table using this library. If it does not store properly, just add a `JsonSerialize = true` to the 
`TableColumn` attribute. 

Happy coding!

