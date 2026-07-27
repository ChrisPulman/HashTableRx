[![NuGet Stats](https://img.shields.io/nuget/v/HashTableRx.svg)](https://www.nuget.org/packages/HashTableRx) ![Nuget](https://img.shields.io/nuget/dt/HashTableRx?color=pink&style=plastic)

# HashTableRx

HashTableRx projects a structured object into a reactive, dotted-path hash table. It is designed for live data models where the values inside a fixed or changing object graph are read repeatedly, transformed, observed, updated, and written back to the original structure.

Typical use cases include PLC/ADS structures, telemetry objects, dynamically loaded assemblies, generated structures, and any object graph where values are addressed by names such as `Rig.Axis.Speed.Value` instead of strongly typed property chains.

## Packages

| Package | Namespace | Reactive base | Use when |
| --- | --- | --- | --- |
| `HashTableRx` | `CP.Collections` | `ReactiveUI.Primitives` | You want the lightweight ReactiveUI.Primitives implementation without taking a direct dependency on System.Reactive. |
| `HashTableRx.Reactive` | `CP.Collections.Reactive` | `ReactiveUI.Primitives.Reactive` | You have existing Rx/System.Reactive code and want the same HashTableRx source API under a separate namespace. |

These are the only two distributable library packages in this repository; the benchmark and test projects are not packages. Both packages compile the same production source and expose the same API shape. The intentional differences are the namespace and reactive primitive base, so install **one** package per consuming project unless you specifically need both namespace variants.

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

At the time of this source audit, the product packages use `ReactiveUI.Primitives` 7.1.0 or `ReactiveUI.Primitives.Reactive` 7.1.0 respectively. The latter supplies the System.Reactive-compatible observable and scheduler surface used by its build.

## Repository Projects And Verification

| Project | Purpose | Frameworks |
| --- | --- | --- |
| `src/HashTableRx` | Builds the `HashTableRx` package in `CP.Collections`. | All product frameworks listed above |
| `src/HashTableRx.Reactive` | Links the same production source under `CP.Collections.Reactive` and builds the `HashTableRx.Reactive` package. | All product frameworks listed above |
| `src/HashTableRx.Tests` | TUnit/Microsoft Testing Platform tests for the normal package. | `net9.0` |
| `src/HashTableRx.Reactive.Tests` | The same TUnit behavior suite compiled against the reactive package. | `net9.0` |
| `src/BenchmarkSuite1` | BenchmarkDotNet performance harness; not a distributable package. | `net10.0` |
| `build/_build.csproj` | Nuke build orchestration; not a distributable package. | `net10.0` |

The repository centrally enables Roslynator plus StyleSharp, PerformanceSharp, and SecuritySharp analyzers. A strict validation build treats diagnostics as errors; the two test projects use TUnit assertions and Microsoft Testing Platform.

## Core Concepts

- Dotted paths address nested values: `Rig.Pump.Speed`, `Casing.Temperature.PV.Value`, `System.Online`.
- Primitive leaves are stored as values. Primitive leaves are primitive types, `string`, arrays of primitives or strings, and supported TwinCAT string wrapper arrays.
- Complex members become nested `HashTableRx` nodes.
- Reads use `Value(path, converter)` or the untyped string indexer; the converter makes the result type explicit and inferable.
- New paths are created with the string indexer: `table["A.B.C"] = 1`.
- Existing paths are updated with `Value(path, value)`. This intentionally throws when the path does not exist.
- `Observe(path, converter)` observes one variable and suppresses duplicate consecutive values.
- `ObserveAll` exposes distinct `(string key, object? value)` updates; consecutive equal tuples are suppressed.
- `SetStructure(object)` rebuilds the table from an object by reflection.
- `Structure` applies current table values back onto the original object instance and returns it.
- `UseUpperCase` normalizes keys and paths to uppercase for case-insensitive PLC-style naming.

The source-observable constructor (`new HashTableRx(source)`) leaves `UseUpperCase` at its default `false`; set the property before using paths if normalization is required.

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

bool online = table.Value("System.Online", static value => (bool)value!);
float current = table.Value("Process.Temperature.CV", static value => (float)value!);

using var temperatureSubscription = table
    .Observe("Process.Temperature.CV", static value => (float)value!)
    .Subscribe(new ActionObserver<float>(value =>
        Console.WriteLine($"Temperature changed to {value}")));

// Value writes are for existing variables. A distinct change emits to Observe
// and ObserveAll.
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
    .Observe("Rig.Speed", static value => (int)value!)
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

int speed = table.Value("Plant.Unit1.Pump.Speed", static value => (int)value!);
bool running = table.Value("Plant.Unit1.Pump.Running", static value => (bool)value!);

object? rawSpeed = table["Plant.Unit1.Pump.Speed"];
object? missing = table["Plant.Unit1.Pump.Missing"];
```

Behavior to know:

- Reading a missing path through the indexer returns `null`.
- Setting a `null` value through the indexer is ignored.
- Setting `A.B.C` can replace a previous scalar stored at `A` with a nested branch; setting `A` later can likewise replace its nested branch with a scalar.
- Root and nested keys are stored in separate nested tables. `Keys` returns the current table level keys, not a flattened list of every dotted path.

## Case Handling

`UseUpperCase` controls path normalization.

```csharp
using CP.Collections;

var caseSensitive = new HashTableRx(useUpperCase: false);
caseSensitive["Root.Child.Value"] = 42;

int exact = caseSensitive.Value("Root.Child.Value", static value => (int)value!);
int missing = caseSensitive.Value("ROOT.CHILD.VALUE", static value => (int)value!); // default(int), 0

var normalized = new HashTableRx(useUpperCase: true);
normalized["Root.Child.Value"] = 42;

int upper = normalized.Value("ROOT.CHILD.VALUE", static value => (int)value!);
int lower = normalized.Value("root.child.value", static value => (int)value!);

using var subscription = normalized
    .Observe("root.child.value", static value => (int)value!)
    .Subscribe(new ActionObserver<int>(value => Console.WriteLine(value)));

normalized.Value("ROOT.CHILD.VALUE", 99);
```

When `UseUpperCase` is `true`, indexer access, `Value(path, converter)`, `Value(path, value)`, and `Observe(path, converter)` all normalize paths.

## Typed Reads And Writes

`Value(path, converter)` reads a stored value through a required converter. The lambda return type infers the result type:

```csharp
using CP.Collections;

var table = new HashTableRx(false);
table["A"] = 5;
table["B"] = "text";

int a = table.Value("A", static value => (int)value!);
string? b = table.Value("B", static value => (string?)value);
int missing = table.Value("Missing", static value => (int)value!);       // default(int)
int wrongType = table.Value("B", static value => (int)value!);           // default(int)
int? nullableMissing = table.Value("Missing", static value => (int?)value);
```

The converter is validated before the table receiver or path. A null converter therefore throws `ArgumentNullException`, even if the receiver or path is null. Because the read and write overloads both use the name `Value`, a deliberate null argument must be explicitly cast to either `Func<object?, T?>` or the intended value type; an untyped `null` literal is ambiguous.

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
    .Observe("A.B.C", static value => (int)value!)
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

`ObserveAll` publishes the dotted path passed to the root table and applies `DistinctUntilChanged()` to the complete `(key, value)` tuple. Consecutive equal tuples are therefore suppressed. `Observe(path, converter)` filters that stream by normalized path, converts each value, and applies a second distinct filter to the converted values. Unlike `Value(path, converter)`, a converter exception faults the typed observable.

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

bool valid = table.Value("CalibrationDataValid", static value => (bool)value!);
float pv = table.Value("Casing.Temperature.PV.Value", static value => (float)value!);

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
- Every reflected leaf loaded by `SetStructure` raises the root property-change events and is eligible for `ObserveAll`; subscribe before loading when initial values matter.

`Structure` returns the same object instance that was passed to `SetStructure`, after applying current table values back into it.

A read-only primitive property is loaded but cannot be replaced during `Structure`. A read-only *nested reference* can still have its child values applied because the referenced object itself is mutable; the property setter is not required for that traversal.

The string indexer intentionally bypasses `Value(path, value)`'s runtime-type guard. If a direct indexer write changes a reflected leaf to an incompatible type, `Structure` catches expected reflection assignment failures and leaves that source member unchanged. Prefer `Value(path, value)` for guarded writes to reflected structures.

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

float pv = table.Value("Casing.Temperature.PV.Value", static value => (float)value!);

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

This is an exact type test based on `Type.IsPrimitive`, plus the explicitly listed string and array cases. `decimal`, `DateTime`, enums, nullable value types, and arbitrary structs are not treated as primitive leaves. They are traversed as complex members and may contribute no paths when they expose no usable public members.

TwinCAT string wrapper arrays are converted to `string[]` when the object being reflected exposes a public static `ToStringArray` method. If that converter is absent or cannot be used, the loaded table value is `null`. This conversion is a read-side convenience: assigning the resulting `string[]` back through `Structure` is not a supported wrapper reconstruction mechanism, and an incompatible reflection write is ignored.

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

string[]? names = table.Value("Names", static value => (string[]?)value);
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
- `Remove(key)` and `Clear()` are scheduled through the configured sequencer and do not publish removal notifications.
- `Get(key)` returns a one-shot observable that emits the current value and completes. Although its tuple value is annotated as `object`, a missing key emits `null` at runtime.
- `Subscribe(observer)` subscribes to table changes.
- `Dispose()` releases the source subscription and internal signal.

### Observable source behavior

The source constructors schedule incoming tuples and publish them to subscribers. A source error or an exception thrown while subscribing is caught and written with `Trace.TraceWarning`; source completion requires no extra table action.

For `HashTableRx(IObservable<...>)`, incoming keys are stored exactly as flat base-table keys. They do not pass through dotted-path expansion or `UseUpperCase`, and they do not raise `PropertyChanging` or `PropertyChanged`. `ObserveAll` still receives the source tuple. Prefer simple source keys, or consume dotted source keys through the base `HashTable` API:

```csharp
using CP.Collections;
using ReactiveUI.Primitives.Signals;

var source = new ReplaySignal<(string key, object? value)>(1);
using var table = new HashTableRx(source);

using var changes = table.ObserveAll.Subscribe(
    new ActionObserver<(string key, object? value)>(change =>
        Console.WriteLine($"{change.key} = {change.value}")));

source.OnNext(("Speed", 1450));
int speed = table.Value("Speed", static value => (int)value!);

source.OnNext(("Live.Speed", 1500));

// The source stored this exact flat key. The dotted HashTableRx string indexer
// searches a nested Live -> Speed path, so use the base table for this case.
object? flatSpeed = ((HashTable)table)["Live.Speed"];
object? nestedSpeed = table["Live.Speed"]; // null
```

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
| `void Clear()` | Schedules removal of all values without publishing removal notifications. |
| `void Dispose()` | Releases the source subscription and internal signal. |
| `IObservable<(string key, object value)> Get(object key)` | Returns a scheduled one-shot observable for the current key value. |
| `void Remove(object key)` | Schedules removal of one key without publishing a removal notification. Null keys are ignored. |
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
| `HashTableRx(IObservable<(string key, object? value)> source)` | Creates a table that receives updates from an observable source; `UseUpperCase` initially remains `false`. |
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
| `this[string fullName]` | `object?` | Gets or sets a value by dotted path. A non-null set creates intermediate nodes; a null/empty path or null value is ignored. |

Methods:

| Member | Description |
| --- | --- |
| `void Add(object key, object? value)` | Adds or replaces a value at this table level. |
| `void Add(object key, HashTableRx value)` | Adds or replaces a nested table at this table level. |
| `bool ContainsKey(object key, bool searchAll)` | Checks the current table level or recursively finds that key segment at any nested level when `searchAll` is `true`; this is not a dotted-path lookup. |
| `void SetStructure(object? value)` | Clears current values and loads public fields/properties from a structured object, publishing each reflected leaf. Null input is ignored. |

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
| `IObservable<T?> Observe<T>(string variable, Func<object?, T?> converter)` | `IHashTableRx` | Observes one dotted path, converts each raw value, and suppresses consecutive duplicate values. Throws `ArgumentNullException` when the runtime receiver or converter is null. |
| `T? Value<T>(string? variable, Func<object?, T?> converter)` | `IHashTableRx` | Reads a dotted path through its required converter. Returns `default` when the runtime receiver, path, or value is not available; an `InvalidCastException` from the converter also returns `default`. A null converter throws before those checks. |
| `bool Value<T>(string? variable, T? value)` | `IHashTableRx` | Writes an existing dotted path. Returns `false` for a null runtime receiver. Throws `InvalidVariableException` for null/missing paths and `InvalidCastException` for type mismatch. |
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

    public T? Read<T>(string path) => _table.Value(path, static value => (T?)value);

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
- Use `Observe(path, converter)` for path-specific processing and `ObserveAll` for routing or diagnostics.
- Avoid overwriting `Tag` metadata keys used by reflection support.
- For dynamic external structures, treat `SetStructure` as the shape refresh point.

## License

MIT
