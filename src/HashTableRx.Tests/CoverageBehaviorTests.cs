// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.ComponentModel;
using System.Runtime.Serialization;
using ReactiveUI.Primitives.Concurrency;
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

public class CoverageBehaviorTests
{
    [Test]
    public async Task HashTableExposesCollectionMembersAndDisposesIdempotently()
    {
        var table = new HashTable(ImmediateSequencer.Instance);
        table.Add("A", 1);
        table.Add("B", 2);

        await Assert.That(table.Keys.Length).IsEqualTo(2);
        await Assert.That(table.ContainsKey("A")).IsTrue();
        await Assert.That(table.IsSynchronized).IsFalse();
        await Assert.That(table.SyncRoot).IsNotNull();

        var copied = new KeyValuePair<string, object?>[2];
        table.CopyTo(copied, 0);
        await Assert.That(copied.Any(item => item.Key == "A" && (int)item.Value! == 1)).IsTrue();

        var enumerated = new List<KeyValuePair<string, object?>>();
        foreach (KeyValuePair<string, object?> item in table)
        {
            enumerated.Add(item);
        }

        await Assert.That(enumerated.Count).IsEqualTo(2);

        table.Dispose();
        table.Dispose();
        await Assert.That(table.IsDisposed).IsTrue();
    }

    [Test]
    public async Task SourceObservableUpdatesTableAndSubscribers()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTable(ImmediateSequencer.Instance, source);
        var observed = new List<(string key, object? value)>();
        using var subscription = table.Subscribe(new TestObserver<(string key, object? value)>(observed.Add));

        source.Push(("Live.Value", 10));
        source.Push(("Live.Value", 11));

        await Assert.That(table.ContainsKey("Live.Value")).IsTrue();
        await Assert.That((int?)table["Live.Value"]).IsEqualTo(11);
        await Assert.That(observed.Count).IsEqualTo(2);
        await Assert.That((int?)observed[1].value).IsEqualTo(11);
    }

    [Test]
    public async Task SourceObservableFaultsCompletionAndSubscribeFailureAreHandled()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTable(ImmediateSequencer.Instance, source);

        source.PushError(new InvalidOperationException("live feed failed"));
        source.Complete();

        using var failedSubscriptionTable = new HashTable(ImmediateSequencer.Instance, new ThrowingObservable<(string key, object? value)>());

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(failedSubscriptionTable.Count).IsEqualTo(0);
    }

    [Test]
    public async Task HashTableNullKeysAreIgnored()
    {
        var table = new HashTable(ImmediateSequencer.Instance);

        table.Add(null!, 1);
        table[null!] = 2;
        table.Remove(null!);

        await Assert.That(table[null!]).IsNull();
        await Assert.That(table.ContainsKey(null!)).IsFalse();
        await Assert.That(table.Count).IsEqualTo(0);
    }

    [Test]
    public async Task AddAfterDisposeDoesNotThrow()
    {
        var table = new HashTable(ImmediateSequencer.Instance);
        table.Dispose();

        table.Add("A", 1);

        await Assert.That(table.IsDisposed).IsTrue();
    }

    [Test]
    public async Task HashTableRxConstructorFromSourceReceivesUpdates()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTableRx(source);

        source.Push(("A", 42));
        SpinWait.SpinUntil(() => table.ContainsKey("A"), TimeSpan.FromSeconds(1));

        await Assert.That(table.Value<int>("A")).IsEqualTo(42);
    }

    [Test]
    public async Task SearchAllFindsNestedKeys()
    {
        var table = new HashTableRx(false);
        table["Root.Child.Value"] = 5;
        table.Add("Explicit", new HashTableRx(false));
        table.Add("Direct", 6);

        await Assert.That(table.ContainsKey("Root", searchAll: false)).IsTrue();
        await Assert.That(table.ContainsKey("Value", searchAll: false)).IsFalse();
        await Assert.That(table.ContainsKey("Value", searchAll: true)).IsTrue();
        await Assert.That(table.ContainsKey("Missing", searchAll: true)).IsFalse();
        await Assert.That(table.Value<int>("Direct")).IsEqualTo(6);
    }

    [Test]
    public async Task MissingNestedPathAndNullSetAreNoOps()
    {
        var table = new HashTableRx(false);
        table["A"] = 1;

        await Assert.That(table["A.B"]).IsNull();
        await Assert.That(new HashTableRx(false).Structure).IsNull();

        table["A"] = null;
        await Assert.That(table.Value<int>("A")).IsEqualTo(1);
    }

    [Test]
    public async Task PropertyEventsFireForValueChanges()
    {
        var table = new HashTableRx(false);
        var changing = new List<string?>();
        var changed = new List<string?>();
        table.PropertyChanging += (_, args) => changing.Add(args.PropertyName);
        table.PropertyChanged += (_, args) => changed.Add(args.PropertyName);

        table["A.B"] = 1;
        table.Value("A.B", 2);

        await Assert.That(changing.Contains("A.B")).IsTrue();
        await Assert.That(changed.Contains("A.B")).IsTrue();
    }

    [Test]
    public async Task ReflectionRoundTripsFieldsAndProperties()
    {
        var model = new ReflectionRoot
        {
            PropertyValue = 7,
            Text = "initial",
            ChildProperty = new ReflectionChild
            {
                Flag = true,
                AmountField = 2.5f,
                GrandChildProperty = new ReflectionGrandChild { Code = 12 },
                GrandChildField = new ReflectionGrandChild { Code = 13 },
            },
            Numbers = [1, 2, 3],
            ChildField = new ReflectionChild
            {
                Flag = false,
                AmountField = 4.5f,
                GrandChildProperty = new ReflectionGrandChild { Code = 14 },
                GrandChildField = new ReflectionGrandChild { Code = 15 },
            },
            FieldValue = 9,
        };

        var table = new HashTableRx(false);
        table.SetStructure(model);

        await Assert.That(table.Value<int>("PropertyValue")).IsEqualTo(7);
        await Assert.That(table.Value<string>("Text")).IsEqualTo("initial");
        await Assert.That(table.Value<bool>("ChildProperty.Flag")).IsTrue();
        await Assert.That(table.Value<int>("ChildProperty.GrandChildProperty.Code")).IsEqualTo(12);
        await Assert.That(table.Value<int>("ChildProperty.GrandChildField.Code")).IsEqualTo(13);
        await Assert.That(table.Value<float>("ChildField.AmountField")).IsEqualTo(4.5f);
        await Assert.That(table.Value<int>("ChildField.GrandChildProperty.Code")).IsEqualTo(14);
        await Assert.That(table.Value<int>("ChildField.GrandChildField.Code")).IsEqualTo(15);

        table.Value("PropertyValue", 8);
        table.Value("Text", "updated");
        table.Value("ChildProperty.Flag", false);
        table.Value("ChildProperty.GrandChildProperty.Code", 22);
        table.Value("ChildProperty.GrandChildField.Code", 23);
        table.Value("ChildField.AmountField", 6.5f);
        table.Value("ChildField.GrandChildProperty.Code", 24);
        table.Value("ChildField.GrandChildField.Code", 25);

        var updated = (ReflectionRoot)table.Structure!;

        await Assert.That(updated.PropertyValue).IsEqualTo(8);
        await Assert.That(updated.Text).IsEqualTo("updated");
        await Assert.That(updated.ChildProperty.Flag).IsFalse();
        await Assert.That(updated.ChildProperty.GrandChildProperty.Code).IsEqualTo(22);
        await Assert.That(updated.ChildProperty.GrandChildField.Code).IsEqualTo(23);
        await Assert.That(updated.ChildField.AmountField).IsEqualTo(6.5f);
        await Assert.That(updated.ChildField.GrandChildProperty.Code).IsEqualTo(24);
        await Assert.That(updated.ChildField.GrandChildField.Code).IsEqualTo(25);
    }

    [Test]
    public async Task MixinNullAndTypeFallbackPathsReturnDefaults()
    {
        IHashTableRx? table = null;
        await Assert.That(table!.Value<int>("A")).IsEqualTo(0);
        await Assert.That(table!.Value("A", 1)).IsFalse();
        await Assert.That(() => table!.Observe<int>("A")).Throws<ArgumentNullException>();

        var concrete = new HashTableRx(false);
        concrete["A"] = "text";
        await Assert.That(concrete.Value<int>("A")).IsEqualTo(0);

        var throwing = new ThrowingHashTableRx();
        await Assert.That(() => throwing.Value("A", 1)).Throws<InvalidVariableException>();
    }

    [Test]
    public async Task PrimitiveArrayHelpersCoverSupportedTypeShapes()
    {
        Type? nullType = null;

        await Assert.That(nullType.IsPrimitiveArray()).IsFalse();
        await Assert.That(typeof(int).IsPrimativeArray()).IsTrue();
        await Assert.That(typeof(string).IsPrimitiveArray()).IsTrue();
        await Assert.That(typeof(int[]).IsPrimitiveArray()).IsTrue();
        await Assert.That(typeof(string[]).IsPrimitiveArray()).IsTrue();
        await Assert.That(typeof(object).IsPrimitiveArray()).IsFalse();
        await Assert.That(typeof(object[]).IsPrimitiveArray()).IsFalse();
        await Assert.That(typeof(STRING_80_WRAPPER[]).IsTwinCATStringArray()).IsTrue();
        await Assert.That(((Type?)null).IsTwinCATStringArray()).IsFalse();
        await Assert.That(typeof(string).IsTwinCATStringArray()).IsFalse();
        await Assert.That(typeof(string[]).IsTwinCATStringArray()).IsFalse();
    }

    [Test]
    public async Task InvalidVariableExceptionMessagesIncludeVariableName()
    {
        await Assert.That(new InvalidVariableException().Message).IsEqualTo("The variable -  - does not exist in the PLC");
        await Assert.That(new InvalidVariableException("A").Message).IsEqualTo("The variable - A - does not exist in the PLC");
        await Assert.That(new InvalidVariableException("B", new InvalidOperationException()).Message).IsEqualTo("The variable - B - does not exist in the PLC");
    }

    [Test]
    public async Task SerializationConstructorInitializesTag()
    {
        var table = new SerializationConstructorHashTableRx();

        await Assert.That(table.Tag).IsNotNull();
    }

    [Test]
    public async Task TwinCatStringArraysAreConvertedOnSetStructure()
    {
        var model = new TwinCatStringRoot
        {
            PropertyStrings = [new() { Value = "P1" }, new() { Value = "P2" }],
            FieldStrings = [new() { Value = "F1" }],
        };

        var table = new HashTableRx(false);
        table.SetStructure(model);

        await Assert.That(table.Value<string[]>("PropertyStrings")).IsEquivalentTo(["P1", "P2"]);
        await Assert.That(table.Value<string[]>("FieldStrings")).IsEquivalentTo(["F1"]);
    }

    [Test]
    public async Task ReflectionGetStructureSwallowsInvalidBackWrites()
    {
        var throwingProperty = new ThrowingSetterRoot();
        var propertyTable = new HashTableRx(false);
        propertyTable.SetStructure(throwingProperty);
        propertyTable["Throwing"] = 2;

        var invalidField = new ReflectionRoot { FieldValue = 1 };
        var fieldTable = new HashTableRx(false);
        fieldTable.SetStructure(invalidField);
        fieldTable["FieldValue"] = "invalid";

        await Assert.That(propertyTable.Structure).IsSameReferenceAs(throwingProperty);
        await Assert.That(fieldTable.Structure).IsSameReferenceAs(invalidField);
    }

    [Test]
    public async Task ReflectionGetStructureLeavesNullNestedObjectsUntouched()
    {
        var model = new ReflectionRoot
        {
            ChildProperty = null!,
        };

        var table = new HashTableRx(false);
        table.SetStructure(model);

        var updated = (ReflectionRoot)table.Structure!;

        await Assert.That(updated.ChildProperty).IsNull();
    }

    [Test]
    public async Task ReflectionSetStructureSwallowsThrowingGettersFromPropertiesAndFields()
    {
        var propertyTable = new HashTableRx(false);
        var fieldTable = new HashTableRx(false);

        propertyTable.SetStructure(new ThrowingGetterRoot());
        fieldTable.SetStructure(new ThrowingNestedFieldRoot());

        await Assert.That(propertyTable.Count).IsEqualTo(0);
        await Assert.That(fieldTable.Count).IsEqualTo(0);
    }

    [Test]
    public async Task StructureReloadAndLookupGuardPathsAreNoOps()
    {
        var table = new HashTableRx(false);

        table.SetStructure(null);

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table[null!]).IsNull();
        await Assert.That(() => table.Value<int>(null, 1)).Throws<InvalidVariableException>();
    }

    [Test]
    public async Task StructureApplySkipsMissingAndNullNestedMembers()
    {
        var fieldMissing = new ReflectionRoot();
        var fieldMissingTable = new HashTableRx(false);
        fieldMissingTable.SetStructure(fieldMissing);
        fieldMissingTable["ChildField"] = 42;

        var fieldMissingResult = (ReflectionRoot)fieldMissingTable.Structure!;

        var fieldNull = new ReflectionRoot();
        var fieldNullTable = new HashTableRx(false);
        fieldNullTable.SetStructure(fieldNull);
        fieldNull.ChildField = null!;

        var fieldNullResult = (ReflectionRoot)fieldNullTable.Structure!;

        var propertyMissing = new ReflectionRoot();
        var propertyMissingTable = new HashTableRx(false);
        propertyMissingTable.SetStructure(propertyMissing);
        propertyMissingTable["ChildProperty"] = 42;

        var propertyMissingResult = (ReflectionRoot)propertyMissingTable.Structure!;

        var propertyNull = new ReflectionRoot();
        var propertyNullTable = new HashTableRx(false);
        propertyNullTable.SetStructure(propertyNull);
        propertyNull.ChildProperty = null!;

        var propertyNullResult = (ReflectionRoot)propertyNullTable.Structure!;

        await Assert.That(fieldMissingResult.ChildField).IsNotNull();
        await Assert.That(fieldNullResult.ChildField).IsNull();
        await Assert.That(propertyMissingResult.ChildProperty).IsNotNull();
        await Assert.That(propertyNullResult.ChildProperty).IsNull();
    }

    [Test]
    public async Task StructureReflectionSkipsIndexersAndNonPrimitiveThrowingGetters()
    {
        var indexed = new IndexedRoot();
        var indexedTable = new HashTableRx(false);
        indexedTable.SetStructure(indexed);

        var indexedResult = (IndexedRoot)indexedTable.Structure!;

        var throwingTable = new HashTableRx(false);
        throwingTable.SetStructure(new ThrowingNestedPropertyRoot());

        await Assert.That(indexedResult.Value).IsEqualTo(1);
        await Assert.That(throwingTable.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ReflectionBranchGuardsCoverReadOnlyAndMissingConversionPaths()
    {
        var readOnly = new ReadOnlyNestedRoot();
        var readOnlyTable = new HashTableRx(false);
        readOnlyTable.SetStructure(readOnly);
        readOnlyTable["Child.Code"] = 42;

        var readOnlyPrimitive = new ReadOnlyPrimitiveRoot();
        var readOnlyPrimitiveTable = new HashTableRx(false);
        readOnlyPrimitiveTable.SetStructure(readOnlyPrimitive);
        readOnlyPrimitiveTable["Number"] = 9;

        var writeOnly = new WriteOnlyRoot();
        var writeOnlyTable = new HashTableRx(false);
        writeOnlyTable.SetStructure(writeOnly);

        var noConverter = new TwinCatStringRootWithoutConverter
        {
            PropertyStrings = [new() { Value = "P1" }],
        };
        var noConverterTable = new HashTableRx(false);
        noConverterTable.SetStructure(noConverter);

        var uppercase = new HashTableRx(true);
        uppercase["A"] = 1;
        uppercase[null!] = 2;

        var untypedNullSet = new HashTableRx(false);
        untypedNullSet["A"] = 1;

        var readOnlyResult = (ReadOnlyNestedRoot)readOnlyTable.Structure!;

        await Assert.That(readOnlyResult.Child.Code).IsEqualTo(42);
        await Assert.That(((ReadOnlyPrimitiveRoot)readOnlyPrimitiveTable.Structure!).Number).IsEqualTo(1);
        await Assert.That(writeOnlyTable.Count).IsEqualTo(0);
        await Assert.That(noConverterTable.Value<string[]>("PropertyStrings")).IsNull();
        await Assert.That(uppercase.Count).IsEqualTo(1);
        await Assert.That(() => untypedNullSet.Value<object>("A", null)).Throws<InvalidCastException>();
    }

    [Test]
    public async Task ReflectionExceptionFilterCoversExpectedExceptionTypes()
    {
        var method = typeof(HashTableRx).GetMethod(
            "IsExpectedReflectionException",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        await Assert.That((bool)method.Invoke(null, [new ArgumentException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new InvalidOperationException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new MethodAccessException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new NotSupportedException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new System.Reflection.TargetException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new System.Reflection.TargetInvocationException(null)])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new TimeoutException()])!).IsFalse();
    }

    public sealed class ReflectionRoot
    {
        public ReflectionChild ChildField = new();

        public int FieldValue;

        public ReflectionChild ChildProperty { get; set; } = new();

        public int[] Numbers { get; set; } = [];

        public int PropertyValue { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    public sealed class ReflectionChild
    {
        public float AmountField;

        public ReflectionGrandChild GrandChildField = new();

        public bool Flag { get; set; }

        public ReflectionGrandChild GrandChildProperty { get; set; } = new();
    }

    public sealed class ReflectionGrandChild
    {
        public int Code { get; set; }
    }

    public sealed class ReadOnlyNestedRoot
    {
        public ReflectionGrandChild Child { get; } = new();
    }

    public sealed class ReadOnlyPrimitiveRoot
    {
        public int Number { get; } = 1;
    }

    public sealed class WriteOnlyRoot
    {
        public int WriteOnly
        {
            set
            {
            }
        }
    }

    public sealed class STRING_80_WRAPPER
    {
        public string Value { get; set; } = string.Empty;
    }

    public sealed class TwinCatStringRoot
    {
        public STRING_80_WRAPPER[] FieldStrings = [];

        public STRING_80_WRAPPER[] PropertyStrings { get; set; } = [];

        public static string[] ToStringArray(STRING_80_WRAPPER[] values) => [.. values.Select(value => value.Value)];
    }

    public sealed class TwinCatStringRootWithoutConverter
    {
        public STRING_80_WRAPPER[] PropertyStrings { get; set; } = [];
    }

    public sealed class ThrowingSetterRoot
    {
        private int _throwing = 1;

        public int Throwing
        {
            get => _throwing;
            set => throw new InvalidOperationException("Setter failed.");
        }
    }

    public sealed class ThrowingGetterRoot
    {
        public int Throwing => throw new InvalidOperationException("Getter failed.");
    }

    public sealed class ThrowingNestedFieldRoot
    {
        public ThrowingNestedField ThrowingField = new();
    }

    public sealed class ThrowingNestedPropertyRoot
    {
        public ReflectionChild Child => throw new InvalidOperationException("Nested property getter failed.");
    }

    public sealed class ThrowingNestedField
    {
        public int Throwing => throw new InvalidOperationException("Nested getter failed.");
    }

    public sealed class IndexedRoot
    {
        public int Value { get; set; } = 1;

        public int this[int index]
        {
            get => index;
            set
            {
            }
        }
    }

    private sealed class SerializationConstructorHashTableRx : HashTableRx
    {
#pragma warning disable SYSLIB0050
        public SerializationConstructorHashTableRx()
            : base(new SerializationInfo(typeof(HashTableRx), new FormatterConverter()), new StreamingContext())
        {
        }
#pragma warning restore SYSLIB0050
    }

    private sealed class ThrowingHashTableRx : IHashTableRx
    {
        event PropertyChangedEventHandler? INotifyPropertyChanged.PropertyChanged
        {
            add
            {
            }

            remove
            {
            }
        }

        event PropertyChangingEventHandler? INotifyPropertyChanging.PropertyChanging
        {
            add
            {
            }

            remove
            {
            }
        }

        public int Count => 0;

        public bool IsSynchronized => false;

        public IObservable<(string key, object? value)> ObserveAll => new ManualObservable<(string key, object? value)>();

        public object SyncRoot { get; } = new();

        public HashTable Tag { get; } = [];

        public bool UseUpperCase { get; set; }

        public object? this[string fullName]
        {
            get => throw new InvalidOperationException("Read failed.");
            set
            {
            }
        }

        public void Add(object key, object? value)
        {
        }

        public void Add(object key, HashTableRx value)
        {
        }

        public bool ContainsKey(object key, bool searchAll) => false;

        public void CopyTo(Array array, int index)
        {
        }

        public void Dispose()
        {
        }

        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        public object? Structure => null;

        public void SetStructure(object? value)
        {
        }
    }
}
