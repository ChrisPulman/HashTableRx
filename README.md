[![NuGet Stats](https://img.shields.io/nuget/v/HashTableRx.svg)](https://www.nuget.org/packages/HashTableRx) ![Nuget](https://img.shields.io/nuget/dt/HashTableRx?color=pink&style=plastic)

# HashTableRx
A reactive hash table that mirrors the structure of an object into a dotted path key/value store you can observe and update.

- Targets: .NET Standard 2.0, .NET 8, .NET 9
- Dependencies: System.Reactive

HashTableRx lets you:
- Reflect an object (even from an unknown assembly) into a hierarchical hash table.
- Get and set values using dotted path keys like "Process.Temperature.PV.Value".
- Observe individual variables or all changes as IObservable streams.
- Toggle case sensitivity via UseUpperCase.


## Installation
- NuGet: `dotnet add package HashTableRx`
- Or reference the HashTableRx project in your solution.


## Key concepts
- **Dotted variable paths**: Nested members are addressed with dot separators, e.g. "A.B.C".
- **Primitive-like members**: primitives and string (including arrays of these) are treated as values.
- **Complex members**: non-primitive-like types become nested HashTableRx nodes.
- **Case handling**: When `UseUpperCase` is true, all keys are normalized to uppercase for reads/writes/observations.
- **Value API**: Reading works with `Value<T>(path)`. Writing with `Value(path, value)` requires the variable to already exist; otherwise `InvalidVariableException` is thrown (to prevent silent writes).
- **Indexer API**: The string indexer always creates intermediate nodes as needed.


## Quick start
```csharp
using CP.Collections;
using System.Reactive.Linq;

// Create a reactive hash table (case sensitive keys)
var h = new HashTableRx(useUpperCase: false);

// Create a few variables using the string indexer
h["System.Online"] = true;        // bool
h["Process.Temperature.CV"] = 20f; // float

// Read variables
bool online = h.Value<bool>("System.Online") ?? false;
float temp = h.Value<float>("Process.Temperature.CV") ?? 0f;

// Observe changes to an individual variable
var sub1 = h.Observe<float>("Process.Temperature.CV")
            .Subscribe(v => Console.WriteLine($"Temp changed to {v}"));

// Update a value (variable must already exist for Value(..) write)
h.Value("Process.Temperature.CV", 25f);

// Or use the indexer to create and/or set directly (creates if missing)
h["Process.Temperature.SP"] = 30f;

// Observe all changes
var sub2 = h.ObserveAll
            .Subscribe(kv => Console.WriteLine($"{kv.key} => {kv.value}"));

// Cleanup
sub1.Dispose();
sub2.Dispose();
```


## Case sensitivity and UseUpperCase
```csharp
var h = new HashTableRx(useUpperCase: true);

// All keys are normalized to uppercase internally
h["Rig.Temp.PV"] = 10f;

Console.WriteLine(h.Value<float>("rig.temp.pv")); // 10
Console.WriteLine(h.Value<float>("RIG.TEMP.PV")); // 10

// Observations also normalize
using var sub = h.Observe<float>("rig.temp.pv").Subscribe(v => Console.WriteLine(v));
h.Value("RIG.TEMP.PV", 11f); // Emits 11
```


## Reflecting an unknown assembly (structures)
You can reflect any object instance (e.g., created via reflection from an external assembly) into the hash table.

```csharp
using System.Reflection;
using CP.Collections;

// Load an external assembly and create an instance
var asm = Assembly.LoadFrom(@"path\to\UnknownLibrary.dll");
var obj = asm.CreateInstance("Namespace.TypeName");

var h = new HashTableRx(useUpperCase: false);

// Populate the hash table from the object's public fields/properties
h.SetStructure(obj!);

// Now you can read values from reflected primitive-like fields/properties
var pv = h.Value<float>("Some.Structured.Path.PV");

// Update values in the hash table
h.Value("Some.Structured.Path.SP", 42.0f);

// Push current hash table values back to the original object
var updated = h.GetStructure();
// 'updated' is the same instance with primitive-like values written back
```

Notes:
- Primitive-like = primitive or string (including arrays of those). Complex members become nested nodes.
- When trimming/AOT, reflection is annotated and may require preserving members. See code attributes for details.


## Indexer API vs Value API
- **String indexer** `h["A.B.C"]`:
  - Reading returns the current value (or null if missing).
  - Writing creates intermediate nodes as required and sets the value.

- **Value API**:
  - `T? Value<T>(string path)`: typed read, returns default when missing.
  - `bool Value<T>(string path, T value)`: typed write. Throws `InvalidVariableException` if the variable does not exist.

Example:
```csharp
var h = new HashTableRx(false);

// Create then write
h["A.B.C"] = 1;
h.Value("A.B.C", 2); // ok

// Attempting to write unknown path throws
Assert.Throws<InvalidVariableException>(() => h.Value("X.Y.Z", 5));
```


## Observing changes
- `Observe<T>(path)`: emits typed values on change (distinct until changed).
- `ObserveAll`: emits `(key, object?)` for any change (also distinct until changed at the tuple level).

```csharp
var h = new HashTableRx(false);

// Create initial variable then observe
h["A.B.C"] = 10;
using var sub = h.Observe<int>("A.B.C").Subscribe(v => Console.WriteLine($"A.B.C = {v}"));

h.Value("A.B.C", 10); // may not emit due to DistinctUntilChanged
h.Value("A.B.C", 11); // emits 11

using var subAll = h.ObserveAll.Subscribe(kv => Console.WriteLine($"{kv.key} => {kv.value}"));
h["X.Y"] = 3.14f; // emits ("X.Y", 3.14f)
```


## Working with nested structures
```csharp
var h = new HashTableRx(false);

// Writes automatically create intermediate nodes
h["Plant.Unit1.Pump.Speed"] = 1200;

// Reads follow the same structure
int? speed = h.Value<int>("Plant.Unit1.Pump.Speed");

// Switching a scalar to a branch via write
h["Plant.Unit1"] = 123;         // was scalar
h["Plant.Unit1.Pump.Speed"] = 900; // Node becomes a nested table to accommodate deeper path
```


## Using the bool indexer for reflection
The bool indexer `h[true]` is a convenience to reflect the entire object graph to and from the hash table.

```csharp
var h = new HashTableRx(false);

// Push an object's values into the table
h[true] = myObject;    // equivalent to h.SetStructure(myObject)

// Later, materialize current values back into the object
var updated = h[true]; // equivalent to h.GetStructure()
```


## HashTable base members
HashTableRx derives from a reactive HashTable that is observable as a sequence of (key, value) updates.

- `Add(object key, object? value)`: Adds a key/value (also notifies observers).
- `Remove(object key)`: Removes a key (scheduled).
- `Clear()`: Clears all entries (scheduled).
- `Get(object key)`: Returns `IObservable<(string key, object value)>` that reads the indexer on a scheduler.

```csharp
var ht = new HashTable();

ht.Add("K", 123);
var (k, v) = ht.Get("K").Wait();  // ("K", 123)

ht.Remove("K");
ht.Clear();
```


## Error handling
- `InvalidVariableException`: thrown by `Value(path, value)` when the variable does not exist.
- `InvalidCastException`: thrown by `Value(path, value)` when the existing variable type is incompatible with value.

```csharp
try
{
    h.Value("A.B.C", "wrong-type");
}
catch (InvalidCastException ex)
{
    Console.WriteLine(ex.Message);
}
```


## Performance notes
- Reflection for `SetStructure`/`GetStructure` is optimized using compiled expression trees and caching per Type.
- Primitive-like members read/write via fast delegates, avoiding slow `PropertyInfo`/`FieldInfo.GetValue`/`SetValue` loops.
- For heavy usage, prefer creating variables once (via indexer or `SetStructure`) and then use `Value(path, value)` for updates.


## Tips
- If you need case-insensitive behavior, construct `HashTableRx` with `useUpperCase: true`.
- Prefer `Value<T>(path)` for typed reads. It returns `default(T)` when missing instead of throwing.
- To write to a brand new path, use the indexer (`h["A.B.C"] = value`) or `SetStructure` first. `Value(path, value)` enforces that the variable already exists.


## License
MIT
