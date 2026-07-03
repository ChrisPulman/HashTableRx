[![NuGet Stats](https://img.shields.io/nuget/v/HashTableRx.svg)](https://www.nuget.org/packages/HashTableRx) ![Nuget](https://img.shields.io/nuget/dt/HashTableRx?color=pink&style=plastic)

# HashTableRx

HashTableRx projects a structured object into a reactive, dotted-path hash table. It is designed for live data models where the values inside a fixed or changing object graph are read repeatedly, transformed, observed, updated, and written back to the original structure.

Typical use cases include PLC/ADS structures, telemetry objects, dynamically loaded assemblies, generated structures, and any object graph where values are addressed by names such as `Rig.Axis.Speed.Value` instead of strongly typed property chains.

## Packages

| Package | Namespace | Reactive base | Use when |
| --- | --- | --- | --- |
| `HashTableRx` | `CP.Collections` | `ReactiveUI.Primitives` | You want the lightweight ReactiveUI.Primitives implementation without taking a direct dependency on System.Reactive. |
| `HashTableRx.Reactive` | `CP.Collections.Reactive` | `ReactiveUI.Primitives.Reactive` | You have existing Rx/System.Reactive code and want the same HashTableRx source API under a separate namespace. |

Both packages share the same source code and public API shape. The only intended difference is the namespace and reactive package base.

```powershell
dotnet add package HashTableRx
dotnet add package HashTableRx.Reactive
```

## Target Frameworks

The library targets:

- `net462`
- `net472`
- `net48`
- `net481`
- `net8.0`
- `net9.0`
- `net10.0`
- `net11.0`

The product packages use `ReactiveUI.Primitives` 6.x and analyzers include `StyleSharp.Analyzers` 3.13.4.

## Core Concepts

- Dotted paths address nested values: `Rig.Pump.Speed`, `Casing.Temperature.PV.Value`, `System.Online`.
- Primitive leaves are stored as values. Primitive leaves are primitive types, `string`, arrays of primitives or strings, and supported TwinCAT string wrapper arrays.
- Complex members become nested `HashTableRx` nodes.
- Reads can be typed with `Value<T>(path)` or untyped through the string indexer.
- New paths are created with the string indexer: `table["A.B.C"] = 1`.
- Existing paths are updated with `Value(path, value)`. This intentionally throws when the path does not exist.
- `Observe<T>(path)` observes one variable and suppresses duplicate consecutive values.
- `ObserveAll` observes every value change as `(string key, object? value)`.
- `SetStructure(object)` rebuilds the table from an object by reflection.
- `Structure` applies current table values back onto the original object instance and returns it.
- `UseUpperCase` normalizes keys and paths to uppercase for case-insensitive PLC-style naming.

## Minimal Example

`IObservable<T>` is a BCL interface, so the examples below use a tiny observer helper. If your application uses Rx, ReactiveUI, or another observable helper package, replace this with your normal subscription style.

```csharp
public sealed class ActionObserver<T>(Action<T> onNext) : IObserver<T>
{
    public void OnCompleted()
    {
    }

    public void OnError(Exception error) => Console.Error.WriteLine(error);

    public void OnNext(T value) => onNext(value);
}
```

Create paths, read values, observe changes, and update existing values:

```csharp
using CP.Collections;

using var table = new HashTableRx(useUpperCase: false);

// The indexer creates missing branches and leaf values.
table["System.Online"] = true;
table["Process.Temperature.CV"] = 20.0f;

bool online = table.Value<bool>("System.Online");
float current = table.Value<float>("Process.Temperature.CV");

using var temperatureSubscription = table
    .Observe<float>("Process.Temperature.CV")
    .Subscribe(new ActionObserver<float>(value =>
        Console.WriteLine($"Temperature changed to {value}")));

// Value writes are for existing variables. This emits to Observe<T> and ObserveAll.
table.Value("Process.Temperature.CV", 25.0f);

// New paths still use the indexer.
table["Process.Temperature.SP"] = 30.0f;
```

## Choosing The Namespace

Use `HashTableRx` for the normal package:

```csharp
using CP.Collections;

var table = new HashTableRx(useUpperCase: false);
```

Use `HashTableRx.Reactive` for the Rx-compatible package:

```csharp
using CP.Collections.Reactive;

var table = new HashTableRx(useUpperCase: false);
```

If a file references both packages, alias one or both namespaces to avoid ambiguous type names:

```csharp
using PrimitiveHash = CP.Collections.HashTableRx;
using ReactiveHash = CP.Collections.Reactive.HashTableRx;

var primitiveTable = new PrimitiveHash(false);
var reactiveTable = new ReactiveHash(false);
```

With `HashTableRx.Reactive`, existing System.Reactive-style consumers can use their usual Rx helpers:

```csharp
using CP.Collections.Reactive;
using System.Reactive.Linq;

var table = new HashTableRx(false);
table["Rig.Speed"] = 0;

using var subscription = table
    .Observe<int>("Rig.Speed")
    .Subscribe(value => Console.WriteLine($"Speed: {value}"));

table.Value("Rig.Speed", 1450);
```

## Dotted Path Access

The string indexer walks or creates nested `HashTableRx` nodes based on dot separators.

```csharp
using CP.Collections;

var table = new HashTableRx(false);

table["Plant.Unit1.Pump.Speed"] = 1200;
table["Plant.Unit1.Pump.Running"] = true;

int speed = table.Value<int>("Plant.Unit1.Pump.Speed");
bool running = table.Value<bool>("Plant.Unit1.Pump.Running");

object? rawSpeed = table["Plant.Unit1.Pump.Speed"];
object? missing = table["Plant.Unit1.Pump.Missing"];
```

Behavior to know:

- Reading a missing path through the indexer returns `null`.
- Setting a `null` value through the indexer is ignored.
- Setting `A.B.C` can replace a previous scalar stored at `A` with a nested branch.
- Root and nested keys are stored in separate nested tables. `Keys` returns the current table level keys, not a flattened list of every dotted path.

## Case Handling

`UseUpperCase` controls path normalization.

```csharp
using CP.Collections;

var caseSensitive = new HashTableRx(useUpperCase: false);
caseSensitive["Root.Child.Value"] = 42;

int exact = caseSensitive.Value<int>("Root.Child.Value");
int missing = caseSensitive.Value<int>("ROOT.CHILD.VALUE"); // default(int), 0

var normalized = new HashTableRx(useUpperCase: true);
normalized["Root.Child.Value"] = 42;

int upper = normalized.Value<int>("ROOT.CHILD.VALUE");
int lower = normalized.Value<int>("root.child.value");

using var subscription = normalized
    .Observe<int>("root.child.value")
    .Subscribe(new ActionObserver<int>(value => Console.WriteLine(value)));

normalized.Value("ROOT.CHILD.VALUE", 99);
```

When `UseUpperCase` is `true`, indexer access, `Value<T>`, `Value(path, value)`, and `Observe<T>` all normalize paths.

## Typed Reads And Writes

`Value<T>(path)` reads and casts a stored value:

```csharp
using CP.Collections;

var table = new HashTableRx(false);
table["A"] = 5;
table["B"] = "text";

int a = table.Value<int>("A");
string? b = table.Value<string>("B");
int missing = table.Value<int>("Missing");       // default(int)
int wrongType = table.Value<int>("B");           // default(int)
int? nullableMissing = table.Value<int?>("Missing");
```

`Value(path, value)` updates an existing variable:

```csharp
using CP.Collections;

var table = new HashTableRx(false);

table["Rig.Speed"] = 0;
table.Value("Rig.Speed", 1450); // ok

try
{
    table.Value("Rig.Unknown", 1);
}
catch (InvalidVariableException ex)
{
    Console.WriteLine(ex.Message);
}

try
{
    table.Value("Rig.Speed", "fast");
}
catch (InvalidCastException ex)
{
    Console.WriteLine(ex.Message);
}
```

Important write rules:

- The path must already exist.
- The existing value must be non-null.
- The new value must have the same runtime type as the existing value.
- No numeric conversion is performed. For example, an existing `int` value cannot be written with a `short`, `long`, or `double`.
- Passing `null` to `Value(path, value)` for an existing non-null variable throws `InvalidCastException`.

Use the indexer when you intentionally want to create a new variable:

```csharp
table["Rig.NewValue"] = 123;
```

## Observing Values

Observe one path:

```csharp
using CP.Collections;

var table = new HashTableRx(false);
var received = new List<int>();

using var subscription = table
    .Observe<int>("A.B.C")
    .Subscribe(new ActionObserver<int>(received.Add));

table["A.B.C"] = 1;      // emits 1
table.Value("A.B.C", 1); // duplicate, suppressed by DistinctUntilChanged
table.Value("A.B.C", 2); // emits 2
```

Observe all value changes:

```csharp
using CP.Collections;

var table = new HashTableRx(false);

using var subscription = table.ObserveAll.Subscribe(
    new ActionObserver<(string key, object? value)>(change =>
        Console.WriteLine($"{change.key} = {change.value}")));

table["X.Y"] = 3.14f;
table["Z"] = true;
```

`ObserveAll` publishes the dotted path passed to the root table for value changes. `Observe<T>` filters that stream by path and casts the value to `T`.

## Property Change Events

`HashTableRx` implements `INotifyPropertyChanging` and `INotifyPropertyChanged`. The event property name is the dotted path being updated.

```csharp
using CP.Collections;

var table = new HashTableRx(false);

table.PropertyChanging += (_, args) =>
    Console.WriteLine($"Changing {args.PropertyName}");

table.PropertyChanged += (_, args) =>
    Console.WriteLine($"Changed {args.PropertyName}");

table["Casing.Temperature.PV.Value"] = 20.0f;
table.Value("Casing.Temperature.PV.Value", 21.5f);
```

This is useful when adapting `HashTableRx` into UI binding layers or diagnostics.

## Reflecting A Structured Object

`SetStructure` reads public fields and public readable properties from an object. Primitive leaves become values and complex members become nested tables.

```csharp
using CP.Collections;

public sealed class RigSTRUCT
{
    public bool CalibrationDataValid;

    public CasingData Casing { get; set; } = new();
}

public sealed class CasingData
{
    public TemperatureData Temperature { get; set; } = new();
}

public sealed class TemperatureData
{
    public ProcessValue PV { get; set; } = new();
}

public sealed class ProcessValue
{
    public float Value { get; set; }
}

var rig = new RigSTRUCT
{
    CalibrationDataValid = true,
    Casing = { Temperature = { PV = { Value = 18.5f } } }
};

var table = new HashTableRx(useUpperCase: false);
table.SetStructure(rig);

bool valid = table.Value<bool>("CalibrationDataValid");
float pv = table.Value<float>("Casing.Temperature.PV.Value");

table.Value("Casing.Temperature.PV.Value", 20.0f);

var updatedRig = (RigSTRUCT)table.Structure!;
Console.WriteLine(updatedRig.Casing.Temperature.PV.Value); // 20
```

Reflection behavior:

- Public fields are read and written.
- Public readable properties are read.
- Public writable properties are written back by `Structure`.
- Indexer properties are ignored.
- Write-only properties are ignored because they cannot be read.
- Throwing getters and setters are caught for expected reflection access failures.
- Null nested objects are skipped and left unchanged on write-back.
- `SetStructure(null)` is a no-op.
- Calling `SetStructure` clears the current table values before loading the new object.

`Structure` returns the same object instance that was passed to `SetStructure`, after applying current table values back into it.

## Dynamic Structure Reloads

If the source structure can change at runtime, call `SetStructure` again with a new object instance after the external type or shape changes.

```csharp
using CP.Collections;

var table = new HashTableRx(false);

void OnStructureReadFromLiveSource(object currentRigStructure)
{
    // Rebuilds the table from the current object shape.
    table.SetStructure(currentRigStructure);
}

void WriteValueBackToLiveSource<T>(string path, T value, Action<object> writeStructure)
{
    // The path must exist in the most recently loaded structure.
    table.Value(path, value);

    // Materializes current table values back into the original object instance.
    object updatedStructure = table.Structure!;

    // Your integration layer writes the object to the live source.
    writeStructure(updatedStructure);
}
```

For changing PLC structures, a common pattern is:

1. Read the current PLC structure object.
2. Call `SetStructure(currentObject)`.
3. Use `ObserveAll` to route changes without assuming the full path list is fixed forever.
4. Use `Value(path, value)` only after the path exists in the current structure.
5. Call `Structure` to write current values back to the object before writing it through the PLC/ADS layer.

Path-specific subscriptions remain valid for paths that still exist and emit changes. New paths can be discovered from `ObserveAll` or from your integration layer's structure metadata.

## Loading Types Dynamically

HashTableRx does not require compile-time knowledge of the reflected type.

```csharp
using System.Reflection;
using CP.Collections;

var assembly = Assembly.LoadFrom(@"C:\Path\To\GeneratedStructures.dll");
object rig = assembly.CreateInstance("TwinCATRx.RigSTRUCT")
    ?? throw new InvalidOperationException("Unable to create RigSTRUCT.");

var table = new HashTableRx(false);
table.SetStructure(rig);

float pv = table.Value<float>("Casing.Temperature.PV.Value");

table.Value("Casing.Temperature.PV.Value", pv + 1.0f);

object updated = table.Structure!;
```

This is useful when a PLC or generated library can be updated independently of the application and the application needs to adapt to the loaded object shape.

## Primitive Leaves And TwinCAT String Wrappers

The type helper `IsPrimitiveArray()` controls which members are treated as leaves:

- Primitive values such as `bool`, `int`, `float`, `double`.
- `string`.
- Arrays of primitive values.
- `string[]`.
- TwinCAT string wrapper arrays whose type name contains `STRING_` and `_WRAPPER`.

TwinCAT string wrapper arrays are converted to `string[]` when the source type exposes a public static `ToStringArray` method.

```csharp
using CP.Collections;

public sealed class STRING_80_WRAPPER
{
    public string Value { get; set; } = string.Empty;
}

public sealed class TwinCatStringRoot
{
    public STRING_80_WRAPPER[] Names { get; set; } = [];

    public static string[] ToStringArray(STRING_80_WRAPPER[] values) =>
        [.. values.Select(value => value.Value)];
}

var source = new TwinCatStringRoot
{
    Names =
    [
        new() { Value = "P1" },
        new() { Value = "P2" },
    ],
};

var table = new HashTableRx(false);
table.SetStructure(source);

string[]? names = table.Value<string[]>("Names");
```

The compatibility alias `IsPrimativeArray()` is retained for existing callers. Prefer `IsPrimitiveArray()` in new code.

## Base HashTable API

`HashTable` is the observable key/value base class used by `HashTableRx`. It stores values by `key.ToString()` and publishes `(string key, object? value)` changes when values are added or replaced.

```csharp
using CP.Collections;
using ReactiveUI.Primitives.Concurrency;
using ReactiveUI.Primitives.Signals;

var source = new ReplaySignal<(string key, object? value)>(1);
using var table = new HashTable(ImmediateSequencer.Instance, source);

using var subscription = table.Subscribe(
    new ActionObserver<(string key, object? value)>(change =>
        Console.WriteLine($"{change.key}: {change.value}")));

source.OnNext(("Live.Speed", 1450));

object? speed = table["Live.Speed"];
bool exists = table.ContainsKey("Live.Speed");

var oneShot = table.Get("Live.Speed");
using var readSubscription = oneShot.Subscribe(
    new ActionObserver<(string key, object value)>(change =>
        Console.WriteLine($"Read {change.key}: {change.value}")));

table.Remove("Live.Speed");
table.Clear();
```

Notes:

- `Add(key, value)` adds or replaces a value and notifies observers.
- The object indexer adds or replaces a value without publishing through `Subject`.
- `Remove(key)` and `Clear()` are scheduled through the configured sequencer.
- `Get(key)` returns a one-shot observable that emits the current value and completes.
- `Subscribe(observer)` subscribes to table changes.
- `Dispose()` releases the source subscription and internal signal.

## Full API Reference

### `HashTable`

Namespace:

- `CP.Collections` in `HashTableRx`
- `CP.Collections.Reactive` in `HashTableRx.Reactive`

Implements:

- `IObservable<(string key, object? value)>`
- `IDisposable`
- `ICollection`
- `IEnumerable`

Constructors:

| Member | Description |
| --- | --- |
| `HashTable()` | Creates a table using the package default sequencer. |
| `HashTable(ISequencer scheduler)` | Creates a table using the supplied sequencer. In the reactive package, `ISequencer` is an alias over the Rx scheduler type used by that build. |
| `HashTable(IObservable<(string key, object? value)> source)` | Creates a table and subscribes to a source of key/value changes using the package default sequencer. |
| `HashTable(ISequencer scheduler, IObservable<(string key, object? value)> source)` | Creates a table, subscribes to a source, and schedules incoming updates through the supplied sequencer. |

Properties:

| Member | Type | Description |
| --- | --- | --- |
| `Count` | `int` | Number of keys at this table level. |
| `IsSynchronized` | `bool` | Always `false`. |
| `IsDisposed` | `bool` | Indicates whether the internal subscription slot is disposed. |
| `Keys` | `string[]` | Snapshot of keys at this table level. |
| `Source` | `IObservable<(string key, object? value)>` | The table update stream. |
| `SyncRoot` | `object` | Collection synchronization object. |
| `this[object key]` | `object?` | Gets or sets a value by `key.ToString()`. Null keys are ignored and read as `null`. |

Methods:

| Member | Description |
| --- | --- |
| `void Add(object key, object? value)` | Adds or replaces a value and publishes an update. Null keys are ignored. |
| `void Clear()` | Schedules removal of all values. |
| `void Dispose()` | Releases the source subscription and internal signal. |
| `IObservable<(string key, object value)> Get(object key)` | Returns a scheduled one-shot observable for the current key value. |
| `void Remove(object key)` | Schedules removal of one key. Null keys are ignored. |
| `IDisposable Subscribe(IObserver<(string key, object? value)> observer)` | Subscribes to table updates. |
| `void CopyTo(Array array, int index)` | Implements `ICollection.CopyTo`. |
| `IEnumerator GetEnumerator()` | Enumerates stored `KeyValuePair<string, object?>` values. |
| `bool ContainsKey(object key)` | Checks whether the current table level contains the key. |

### `HashTableRx`

Inherits from `HashTable` and implements `IHashTableRx`.

Constructors:

| Member | Description |
| --- | --- |
| `HashTableRx(bool useUpperCase)` | Creates an empty dotted-path table and sets path normalization mode. |
| `HashTableRx(IObservable<(string key, object? value)> source)` | Creates a table that receives updates from an observable source. |
| `HashTableRx(SerializationInfo info, StreamingContext context)` | Protected serialization constructor. |

Events:

| Member | Description |
| --- | --- |
| `PropertyChanging` | Raised before a dotted path changes. |
| `PropertyChanged` | Raised after a dotted path changes. |

Properties:

| Member | Type | Description |
| --- | --- | --- |
| `ObserveAll` | `IObservable<(string key, object? value)>` | Distinct stream of all changed dotted-path values. |
| `Tag` | `HashTable` | Metadata table associated with this instance. When using reflection, avoid overwriting `Data`, `FieldInfo`, or `PropertyInfo` keys. |
| `UseUpperCase` | `bool` | Normalizes dotted paths to uppercase when `true`. |
| `Structure` | `object?` | Applies current table values back to the object loaded by `SetStructure` and returns it. Returns `null` when no structure has been loaded. |
| `this[string fullName]` | `object?` | Gets or sets a value by dotted path. Setting creates intermediate nodes. |

Methods:

| Member | Description |
| --- | --- |
| `void Add(object key, object? value)` | Adds or replaces a value at this table level. |
| `void Add(object key, HashTableRx value)` | Adds or replaces a nested table at this table level. |
| `bool ContainsKey(object key, bool searchAll)` | Checks the current table level or, when `searchAll` is `true`, recursively checks nested tables. |
| `void SetStructure(object? value)` | Clears current values and loads public fields/properties from a structured object. Null input is ignored. |

### `IHashTableRx`

`IHashTableRx` is the public abstraction for dotted-path reactive tables.

It combines:

- `IEnumerable`
- `IDisposable`
- `ICollection`
- `INotifyPropertyChanged`
- `INotifyPropertyChanging`

It exposes:

- `UseUpperCase`
- `ObserveAll`
- `Tag`
- `Structure`
- `this[string fullName]`
- `Add(object, object?)`
- `Add(object, HashTableRx)`
- `ContainsKey(object, bool)`
- `SetStructure(object?)`

### Extension Members

`HashTableRxExtensions` adds the main consumer helpers.

| Member | Applies to | Description |
| --- | --- | --- |
| `IObservable<T?> Observe<T>(string variable)` | `IHashTableRx?` | Observes one dotted path and suppresses consecutive duplicate values. Throws `ArgumentNullException` when the table is null. |
| `T? Value<T>(string? variable)` | `IHashTableRx?` | Reads a dotted path. Returns `default` when the table, path, value, or cast is not available. |
| `bool Value<T>(string? variable, T? value)` | `IHashTableRx?` | Writes an existing dotted path. Returns `false` for a null table. Throws `InvalidVariableException` for null/missing paths and `InvalidCastException` for type mismatch. |
| `bool IsPrimitiveArray()` | `Type?` | Returns `true` when the type is treated as a leaf value. |
| `bool IsPrimativeArray()` | `Type?` | Compatibility alias for the historical misspelling. |
| `bool IsTwinCATStringArray()` | `Type?` | Returns `true` for TwinCAT string wrapper array type names. |

`RxExtensions.OnNextHasObservers<T>(ReplaySignal<T>, T)` is public for package-internal signal publishing scenarios. Most consumers do not need to call it directly.

### `InvalidVariableException`

Thrown when a write is attempted against a missing variable path.

Constructors:

| Member | Description |
| --- | --- |
| `InvalidVariableException()` | Creates an exception with an empty variable name. |
| `InvalidVariableException(string? variable)` | Creates an exception for a specific variable. |
| `InvalidVariableException(string message, Exception innerException)` | Creates an exception with an inner exception. |

Message format:

```text
The variable - {variable} - does not exist in the PLC
```

## Error Handling Patterns

Use the indexer for initial creation and `Value(path, value)` for guarded writes:

```csharp
using CP.Collections;

var table = new HashTableRx(false);
table["Rig.Enabled"] = false;

bool TryWrite<T>(HashTableRx target, string path, T value)
{
    try
    {
        return target.Value(path, value);
    }
    catch (InvalidVariableException)
    {
        // The current structure does not contain this path.
        return false;
    }
    catch (InvalidCastException)
    {
        // The value exists but has a different runtime type.
        return false;
    }
}

_ = TryWrite(table, "Rig.Enabled", true);
_ = TryWrite(table, "Rig.Enabled", 1);       // false, wrong type
_ = TryWrite(table, "Rig.Unknown", true);    // false, missing path
```

## Live Data Pattern

The library does not perform PLC or network IO. It stores, observes, and mutates values from objects supplied by your IO layer.

```csharp
using CP.Collections;

public sealed class LiveRigAdapter(Action<object> writeRig)
{
    private readonly HashTableRx _table = new(useUpperCase: false);

    public IObservable<(string key, object? value)> Changes => _table.ObserveAll;

    public void ApplyRead(object rigStructure)
    {
        _table.SetStructure(rigStructure);
    }

    public T? Read<T>(string path) => _table.Value<T>(path);

    public void Write<T>(string path, T value)
    {
        _table.Value(path, value);
        writeRig(_table.Structure!);
    }
}
```

This keeps IO ownership outside the library while still giving the application a consistent reactive model for every live value.

## Trimming And AOT

`SetStructure` and `Structure` use reflection over public fields and properties. They are annotated with `RequiresUnreferencedCode` for target frameworks that support trimming analysis.

If you publish with trimming or Native AOT, preserve the members of reflected structure types. For generated PLC structures, this usually means preserving the generated model assembly or the specific root structure types that are passed to `SetStructure`.

## Performance Notes

- Keep one table instance per live structure where possible.
- Load or reload shape with `SetStructure`.
- Use `Value(path, value)` for high-frequency updates to known values.
- Use `Observe<T>` for path-specific processing and `ObserveAll` for routing or diagnostics.
- Avoid overwriting `Tag` metadata keys used by reflection support.
- For dynamic external structures, treat `SetStructure` as the shape refresh point.

## License

MIT
