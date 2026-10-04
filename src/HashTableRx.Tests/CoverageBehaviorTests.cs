// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using System.Collections;
using System.ComponentModel;
#if REACTIVE_TESTS
using ImmediateSequencer = System.Reactive.Concurrency.ImmediateScheduler;
#else
using ReactiveUI.Primitives.Concurrency;
#endif
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Exercises coverage behavior tests contracts.</summary>
public class CoverageBehaviorTests
{
    /// <summary>Defines the PairCount test input.</summary>
    private const int PairCount = 2;

    /// <summary>Defines the LiveValuePath test input.</summary>
    private const string LiveValuePath = "Live.Value";

    /// <summary>Defines the FirstUpdate test input.</summary>
    private const int FirstUpdate = 10;

    /// <summary>Defines the SecondUpdate test input.</summary>
    private const int SecondUpdate = 11;

    /// <summary>Defines the FirstCode test input.</summary>
    private const int FirstCode = 12;

    /// <summary>Defines the SampleValue test input.</summary>
    private const int SampleValue = 42;

    /// <summary>Defines the InitialValue test input.</summary>
    private const int InitialValue = 5;

    /// <summary>Defines the NestedValue test input.</summary>
    private const int NestedValue = 6;

    /// <summary>Defines the InitialPropertyValue test input.</summary>
    private const int InitialPropertyValue = 7;

    /// <summary>Defines the InitialAmount test input.</summary>
    private const float InitialAmount = 2.5F;

    /// <summary>Defines the SecondCode test input.</summary>
    private const int SecondCode = 13;

    /// <summary>Defines the TripleCount test input.</summary>
    private const int TripleCount = 3;

    /// <summary>Defines the FieldAmount test input.</summary>
    private const float FieldAmount = 4.5F;

    /// <summary>Defines the ThirdCode test input.</summary>
    private const int ThirdCode = 14;

    /// <summary>Defines the FourthCode test input.</summary>
    private const int FourthCode = 15;

    /// <summary>Defines the FieldValue test input.</summary>
    private const int InitialFieldValue = 9;

    /// <summary>Defines the UpdatedPropertyValue test input.</summary>
    private const int UpdatedPropertyValue = 8;

    /// <summary>Defines the UpdatedFirstCode test input.</summary>
    private const int UpdatedFirstCode = 22;

    /// <summary>Defines the UpdatedSecondCode test input.</summary>
    private const int UpdatedSecondCode = 23;

    /// <summary>Defines the UpdatedAmount test input.</summary>
    private const float UpdatedAmount = 6.5F;

    /// <summary>Defines the UpdatedThirdCode test input.</summary>
    private const int UpdatedThirdCode = 24;

    /// <summary>Defines the UpdatedFourthCode test input.</summary>
    private const int UpdatedFourthCode = 25;

    /// <summary>Verifies hash table exposes collection members and disposes idempotently.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableExposesCollectionMembersAndDisposesIdempotently()
    {
        using var table = new HashTable(ImmediateSequencer.Instance);
        table.Add("A", 1);
        table.Add("B", PairCount);
        await Assert.That(table.Keys.Length).IsEqualTo(PairCount);
        await Assert.That(table.ContainsKey("A")).IsTrue();
        await Assert.That(table.IsSynchronized).IsFalse();
        await Assert.That(table.SyncRoot).IsNotNull();
        var copied = new KeyValuePair<string, object?>[PairCount];
        table.CopyTo(copied, 0);
        await Assert.That(System.Array.Exists(copied, static item => item.Key == "A" && (int)item.Value! == 1)).IsTrue();
        var enumerated = new List<KeyValuePair<string, object?>>();
        foreach (KeyValuePair<string, object?> item in table)
        {
            enumerated.Add(item);
        }

        await Assert.That(enumerated.Count).IsEqualTo(PairCount);
        table.Dispose();
        table.Dispose();
        await Assert.That(table.IsDisposed).IsTrue();
    }

    /// <summary>Verifies source observable updates table and subscribers.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceObservableUpdatesTableAndSubscribers()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTable(ImmediateSequencer.Instance, source);
        var observed = new List<(string key, object? value)>();
        using var subscription = table.Subscribe(new TestObserver<(string key, object? value)>(observed.Add));
        source.Push((LiveValuePath, FirstUpdate));
        source.Push((LiveValuePath, SecondUpdate));
        await Assert.That(table.ContainsKey(LiveValuePath)).IsTrue();
        await Assert.That((int?)table[LiveValuePath]).IsEqualTo(SecondUpdate);
        await Assert.That(observed.Count).IsEqualTo(PairCount);
        await Assert.That((int?)observed[1].value).IsEqualTo(SecondUpdate);
    }

    /// <summary>Verifies source observable retains updates when subscriber throws.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SourceObservableRetainsUpdatesWhenSubscriberThrows()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTable(ImmediateSequencer.Instance, source);
        using var subscription = table.Subscribe(new TestObserver<(string key, object? value)>(static _ => throw new InvalidOperationException("Subscriber failed.")));
        source.Push((LiveValuePath, FirstCode));
        await Assert.That(table.ContainsKey(LiveValuePath)).IsTrue();
        await Assert.That((int?)table[LiveValuePath]).IsEqualTo(FirstCode);
    }

    /// <summary>Verifies source observable faults completion and subscribe failure are handled.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
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

    /// <summary>Verifies hash table null keys are ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableNullKeysAreIgnored()
    {
        using var table = new HashTable(ImmediateSequencer.Instance);
        table.Add(null!, 1);
        table[null!] = PairCount;
        table.Remove(null!);
        await Assert.That(table[null!]).IsNull();
        await Assert.That(table.ContainsKey(null!)).IsFalse();
        await Assert.That(table.Count).IsEqualTo(0);
    }

    /// <summary>Verifies add after dispose does not throw.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task AddAfterDisposeDoesNotThrow()
    {
        using var table = new HashTable(ImmediateSequencer.Instance);
        table.Dispose();
        table.Add("A", 1);
        await Assert.That(table.IsDisposed).IsTrue();
    }

    /// <summary>Verifies hash table rx constructor from source receives updates.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task HashTableRxConstructorFromSourceReceivesUpdates()
    {
        var source = new ManualObservable<(string key, object? value)>();
        using var table = new HashTableRx(source);
        source.Push(("A", SampleValue));
        _ = SpinWait.SpinUntil(() => table.ContainsKey("A"), TimeSpan.FromSeconds(1));
        await Assert.That(table.Value("A", static value => (int)value!)).IsEqualTo(SampleValue);
    }

    /// <summary>Verifies search all finds nested keys.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SearchAllFindsNestedKeys()
    {
        using var table = new HashTableRx(false);
        table["Root.Child.Value"] = InitialValue;
        table.Add("Explicit", new(false));
        table.Add("Direct", NestedValue);
        await Assert.That(table.ContainsKey("Root", searchAll: false)).IsTrue();
        await Assert.That(table.ContainsKey("Value", searchAll: false)).IsFalse();
        await Assert.That(table.ContainsKey("Value", searchAll: true)).IsTrue();
        await Assert.That(table.ContainsKey("Missing", searchAll: true)).IsFalse();
        await Assert.That(table.Value("Direct", static value => (int)value!)).IsEqualTo(NestedValue);
    }

    /// <summary>Verifies missing nested path and null set are no ops.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MissingNestedPathAndNullSetAreNoOps()
    {
        using var table = new HashTableRx(false);
        table["A"] = 1;
        await Assert.That(table["A.B"]).IsNull();
        await Assert.That(new HashTableRx(false).Structure).IsNull();
        table["A"] = null;
        await Assert.That(table.Value("A", static value => (int)value!)).IsEqualTo(1);
    }

    /// <summary>Verifies property events fire for value changes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task PropertyEventsFireForValueChanges()
    {
        using var table = new HashTableRx(false);
        var changing = new List<string?>();
        var changed = new List<string?>();
        table.PropertyChanging += (_, args) => changing.Add(args.PropertyName);
        table.PropertyChanged += (_, args) => changed.Add(args.PropertyName);
        table["A.B"] = 1;
        _ = table.Value("A.B", PairCount);
        await Assert.That(changing.Contains("A.B")).IsTrue();
        await Assert.That(changed.Contains("A.B")).IsTrue();
    }

    /// <summary>Verifies reflection round trips fields and properties.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionRoundTripsFieldsAndProperties()
    {
        var model = CreateReflectionRoot();
        using var table = new HashTableRx(false);
        table.SetStructure(ReflectionFixture.Create(model));
        await Assert.That(table.Value("PropertyValue", static value => (int)value!)).IsEqualTo(InitialPropertyValue);
        await Assert.That(table.Value("Text", static value => (string?)value)).IsEqualTo("initial");
        await Assert.That(table.Value("ChildProperty.Flag", static value => (bool)value!)).IsTrue();
        await Assert.That(table.Value("ChildProperty.GrandChildProperty.Code", static value => (int)value!)).IsEqualTo(FirstCode);
        await Assert.That(table.Value("ChildProperty.GrandChildField.Code", static value => (int)value!)).IsEqualTo(SecondCode);
        await Assert.That(table.Value("ChildField.AmountField", static value => (float)value!)).IsEqualTo(FieldAmount);
        await Assert.That(table.Value("ChildField.GrandChildProperty.Code", static value => (int)value!)).IsEqualTo(ThirdCode);
        await Assert.That(table.Value("ChildField.GrandChildField.Code", static value => (int)value!)).IsEqualTo(FourthCode);
        _ = table.Value("PropertyValue", UpdatedPropertyValue);
        _ = table.Value("Text", "updated");
        _ = table.Value("ChildProperty.Flag", false);
        _ = table.Value("ChildProperty.GrandChildProperty.Code", UpdatedFirstCode);
        _ = table.Value("ChildProperty.GrandChildField.Code", UpdatedSecondCode);
        _ = table.Value("ChildField.AmountField", UpdatedAmount);
        _ = table.Value("ChildField.GrandChildProperty.Code", UpdatedThirdCode);
        _ = table.Value("ChildField.GrandChildField.Code", UpdatedFourthCode);
        var updated = ReflectionFixture.Read<ReflectionRoot>(table.Structure!);
        await Assert.That(updated.PropertyValue).IsEqualTo(UpdatedPropertyValue);
        await Assert.That(updated.Text).IsEqualTo("updated");
        await Assert.That(updated.ChildProperty.Flag).IsFalse();
        await Assert.That(updated.ChildProperty.GrandChildProperty.Code).IsEqualTo(UpdatedFirstCode);
        await Assert.That(updated.ChildProperty.GrandChildField.Code).IsEqualTo(UpdatedSecondCode);
        await Assert.That(updated.ChildField.AmountField).IsEqualTo(UpdatedAmount);
        await Assert.That(updated.ChildField.GrandChildProperty.Code).IsEqualTo(UpdatedThirdCode);
        await Assert.That(updated.ChildField.GrandChildField.Code).IsEqualTo(UpdatedFourthCode);
    }

    /// <summary>Verifies mixin null and type fallback paths return defaults.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task MixinNullAndTypeFallbackPathsReturnDefaults()
    {
        IHashTableRx? table = null;
        await Assert.That(table!.Value("A", static value => (int)value!)).IsEqualTo(0);
        await Assert.That(table!.Value("A", 1)).IsFalse();
        await Assert.That(() => table!.Observe("A", static value => (int)value!)).Throws<ArgumentNullException>();
        using var concrete = new HashTableRx(false);
        concrete["A"] = "text";
        await Assert.That(concrete.Value("A", static value => (int)value!)).IsEqualTo(0);
        var throwing = new ThrowingHashTableRx();
        await Assert.That(() => throwing.Value("A", 1)).Throws<InvalidVariableException>();
    }

    /// <summary>Verifies primitive array helpers cover supported type shapes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
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

    /// <summary>Verifies invalid variable exception messages include variable name.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task InvalidVariableExceptionMessagesIncludeVariableName()
    {
        await Assert.That(new InvalidVariableException().Message).IsEqualTo("The variable -  - does not exist in the PLC");
        await Assert.That(new InvalidVariableException("A").Message).IsEqualTo("The variable - A - does not exist in the PLC");
        await Assert.That(new InvalidVariableException("B", new InvalidOperationException()).Message).IsEqualTo("The variable - B - does not exist in the PLC");
    }

    /// <summary>Verifies serialization constructor initializes tag.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task SerializationConstructorInitializesTag()
    {
        using var table = new SerializationConstructorHashTableRx();
        await Assert.That(table.Tag).IsNotNull();
    }

    /// <summary>Verifies twin cat string arrays are converted on set structure.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task TwinCatStringArraysAreConvertedOnSetStructure()
    {
        var model = new TwinCatStringRoot
        {
            PropertyStrings = [new()
            {
                Value = "P1"
            }, new()
            {
                Value = "P2"
            }

            ],
            FieldStrings = [new()
            {
                Value = "F1"
            }

            ],
        };
        using var table = new HashTableRx(false);
        table.SetStructure(ReflectionFixture.Create(model));
        await Assert.That(table.Value("PropertyStrings", static value => (string[]?)value)).IsEquivalentTo(["P1", "P2"]);
        await Assert.That(table.Value("FieldStrings", static value => (string[]?)value)).IsEquivalentTo(["F1"]);
    }

    /// <summary>Verifies reflection get structure swallows invalid back writes.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionGetStructureSwallowsInvalidBackWrites()
    {
        var throwingProperty = new ThrowingSetterRoot();
        using var propertyTable = new HashTableRx(false);
        propertyTable.SetStructure(ReflectionFixture.Create(throwingProperty));
        propertyTable["Throwing"] = PairCount;
        var invalidField = ReflectionFixture.Create(new ReflectionRoot
        {
            FieldValue = 1
        });
        using var fieldTable = new HashTableRx(false);
        fieldTable.SetStructure(ReflectionFixture.Create(invalidField));
        fieldTable[nameof(ReflectionRoot.FieldValue)] = "invalid";
        await Assert.That(propertyTable.Structure).IsSameReferenceAs(throwingProperty);
        await Assert.That(fieldTable.Structure).IsSameReferenceAs(invalidField);
    }

    /// <summary>Verifies reflection get structure leaves null nested objects untouched.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionGetStructureLeavesNullNestedObjectsUntouched()
    {
        var model = new ReflectionRoot
        {
            ChildProperty = null!,
        };
        using var table = new HashTableRx(false);
        table.SetStructure(ReflectionFixture.Create(model));
        var updated = ReflectionFixture.Read<ReflectionRoot>(table.Structure!);
        await Assert.That(updated.ChildProperty).IsNull();
    }

    /// <summary>Verifies reflection set structure swallows throwing getters from properties and fields.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionSetStructureSwallowsThrowingGettersFromPropertiesAndFields()
    {
        using var propertyTable = new HashTableRx(false);
        using var fieldTable = new HashTableRx(false);
        propertyTable.SetStructure(ReflectionFixture.Create(new ThrowingGetterRoot()));
        fieldTable.SetStructure(ReflectionFixture.Create(new ThrowingNestedFieldRoot()));
        await Assert.That(propertyTable.Count).IsEqualTo(0);
        await Assert.That(fieldTable.Count).IsEqualTo(0);
    }

    /// <summary>Verifies structure reload and lookup guard paths are no ops.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StructureReloadAndLookupGuardPathsAreNoOps()
    {
        using var table = new HashTableRx(false);
        table.SetStructure(null);
        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table[null!]).IsNull();
        await Assert.That(() => table.Value(null, 1)).Throws<InvalidVariableException>();
    }

    /// <summary>Verifies structure apply skips missing and null nested members.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StructureApplySkipsMissingAndNullNestedMembers()
    {
        var fieldMissing = new ReflectionRoot();
        using var fieldMissingTable = new HashTableRx(false);
        fieldMissingTable.SetStructure(ReflectionFixture.Create(fieldMissing));
        fieldMissingTable["ChildField"] = SampleValue;
        var fieldMissingResult = ReflectionFixture.Read<ReflectionRoot>(fieldMissingTable.Structure!);
        var fieldNull = ReflectionFixture.Create(new ReflectionRoot());
        using var fieldNullTable = new HashTableRx(false);
        fieldNullTable.SetStructure(ReflectionFixture.Create(fieldNull));
        ReflectionFixture.Set(fieldNull, nameof(ReflectionRoot.ChildField), null);
        var fieldNullResult = ReflectionFixture.Read<ReflectionRoot>(fieldNullTable.Structure!);
        var propertyMissing = new ReflectionRoot();
        using var propertyMissingTable = new HashTableRx(false);
        propertyMissingTable.SetStructure(ReflectionFixture.Create(propertyMissing));
        propertyMissingTable["ChildProperty"] = SampleValue;
        var propertyMissingResult = ReflectionFixture.Read<ReflectionRoot>(propertyMissingTable.Structure!);
        var propertyNull = ReflectionFixture.Create(new ReflectionRoot());
        using var propertyNullTable = new HashTableRx(false);
        propertyNullTable.SetStructure(ReflectionFixture.Create(propertyNull));
        ReflectionFixture.Set(propertyNull, nameof(ReflectionRoot.ChildProperty), null);
        var propertyNullResult = ReflectionFixture.Read<ReflectionRoot>(propertyNullTable.Structure!);
        await Assert.That(fieldMissingResult.ChildField).IsNotNull();
        await Assert.That(fieldNullResult.ChildField).IsNull();
        await Assert.That(propertyMissingResult.ChildProperty).IsNotNull();
        await Assert.That(propertyNullResult.ChildProperty).IsNull();
    }

    /// <summary>Verifies structure reflection skips indexers and non primitive throwing getters.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task StructureReflectionSkipsIndexersAndNonPrimitiveThrowingGetters()
    {
        var indexed = new IndexedRoot();
        using var indexedTable = new HashTableRx(false);
        indexedTable.SetStructure(ReflectionFixture.Create(indexed));
        var indexedResult = (IndexedRoot)indexedTable.Structure!;
        using var throwingTable = new HashTableRx(false);
        throwingTable.SetStructure(ReflectionFixture.Create(new ThrowingNestedPropertyRoot()));
        await Assert.That(indexedResult.Value).IsEqualTo(1);
        await Assert.That(throwingTable.Count).IsEqualTo(0);
    }

    /// <summary>Verifies reflection branch guards cover read only and missing conversion paths.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionBranchGuardsCoverReadOnlyAndMissingConversionPaths()
    {
        var readOnly = new ReadOnlyNestedRoot();
        using var readOnlyTable = new HashTableRx(false);
        readOnlyTable.SetStructure(ReflectionFixture.Create(readOnly));
        readOnlyTable["Child.Code"] = SampleValue;
        var readOnlyPrimitive = new ReadOnlyPrimitiveRoot();
        using var readOnlyPrimitiveTable = new HashTableRx(false);
        readOnlyPrimitiveTable.SetStructure(ReflectionFixture.Create(readOnlyPrimitive));
        readOnlyPrimitiveTable["Number"] = InitialFieldValue;
        var writeOnly = ReflectionFixture.CreateWriteOnly();
        using var writeOnlyTable = new HashTableRx(false);
        writeOnlyTable.SetStructure(ReflectionFixture.Create(writeOnly));
        var noConverter = new TwinCatStringRootWithoutConverter
        {
            PropertyStrings = [new()
            {
                Value = "P1"
            }

            ],
        };
        using var noConverterTable = new HashTableRx(false);
        noConverterTable.SetStructure(ReflectionFixture.Create(noConverter));
        using var uppercase = new HashTableRx(true);
        uppercase["A"] = 1;
        uppercase[null!] = PairCount;
        using var untypedNullSet = new HashTableRx(false);
        untypedNullSet["A"] = 1;
        var readOnlyResult = (ReadOnlyNestedRoot)readOnlyTable.Structure!;
        await Assert.That(readOnlyResult.Child.Code).IsEqualTo(SampleValue);
        await Assert.That(((ReadOnlyPrimitiveRoot)readOnlyPrimitiveTable.Structure!).Number).IsEqualTo(1);
        await Assert.That(writeOnlyTable.Count).IsEqualTo(0);
        await Assert.That(noConverterTable.Value("PropertyStrings", static value => (string[]?)value)).IsNull();
        await Assert.That(uppercase.Count).IsEqualTo(1);
        await Assert.That(() => untypedNullSet.Value("A", (object?)null)).Throws<InvalidCastException>();
    }

    /// <summary>Verifies reflection exception filter covers expected exception types.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ReflectionExceptionFilterCoversExpectedExceptionTypes()
    {
        var method = typeof(HashTableRx).GetMethod("IsExpectedReflectionException", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        await Assert.That((bool)method.Invoke(null, [new ArgumentException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new InvalidOperationException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new MethodAccessException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new NotSupportedException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new System.Reflection.TargetException()])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new System.Reflection.TargetInvocationException(null)])!).IsTrue();
        await Assert.That((bool)method.Invoke(null, [new TimeoutException()])!).IsFalse();
    }

    /// <summary>Creates populated property and field branches for reflection round-trip assertions.</summary>
    /// <returns>The reflection fixture template.</returns>
    private static ReflectionRoot CreateReflectionRoot() => new()
    {
        PropertyValue = InitialPropertyValue,
        Text = "initial",
        ChildProperty = new()
        {
            Flag = true,
            AmountField = InitialAmount,
            GrandChildProperty = new() { Code = FirstCode },
            GrandChildField = new() { Code = SecondCode },
        },
        Numbers = [1, PairCount, TripleCount],
        ChildField = new()
        {
            Flag = false,
            AmountField = FieldAmount,
            GrandChildProperty = new() { Code = ThirdCode },
            GrandChildField = new() { Code = FourthCode },
        },
        FieldValue = InitialFieldValue,
    };

    /// <summary>Exercises reflection root contracts.</summary>
    public sealed class ReflectionRoot
    {
        /// <summary>Gets or sets the child field fixture value.</summary>
        public ReflectionChild ChildField { get; set; } = new();

        /// <summary>Gets or sets the field value fixture value.</summary>
        public int FieldValue { get; set; }

        /// <summary>Gets or sets the child property fixture value.</summary>
        public ReflectionChild ChildProperty { get; set; } = new();

        /// <summary>Gets or sets the numbers fixture value.</summary>
        public int[] Numbers { get; init; } = [];

        /// <summary>Gets or sets the property value fixture value.</summary>
        public int PropertyValue { get; set; }

        /// <summary>Gets or sets the text fixture value.</summary>
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>Exercises reflection child contracts.</summary>
    public sealed class ReflectionChild
    {
        /// <summary>Gets or sets the amount field fixture value.</summary>
        public float AmountField { get; set; }

        /// <summary>Gets or sets the grand child field fixture value.</summary>
        public ReflectionGrandChild GrandChildField { get; set; } = new();

        /// <summary>Gets or sets the flag fixture value.</summary>
        public bool Flag { get; set; }

        /// <summary>Gets or sets the grand child property fixture value.</summary>
        public ReflectionGrandChild GrandChildProperty { get; set; } = new();
    }

    /// <summary>Exercises reflection grand child contracts.</summary>
    public sealed class ReflectionGrandChild
    {
        /// <summary>Gets or sets the code fixture value.</summary>
        public int Code { get; set; }
    }

    /// <summary>Exercises read only nested root contracts.</summary>
    public sealed class ReadOnlyNestedRoot
    {
        /// <summary>Gets the child fixture value.</summary>
        public ReflectionGrandChild Child { get; } = new();
    }

    /// <summary>Exercises read only primitive root contracts.</summary>
    public sealed class ReadOnlyPrimitiveRoot
    {
        /// <summary>Gets the number fixture value.</summary>
        public int Number { get; } = 1;
    }

    /// <summary>Exercises string_80_wrapper contracts.</summary>
    public sealed class STRING_80_WRAPPER
    {
        /// <summary>Gets or sets the value fixture value.</summary>
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>Exercises twin cat string root contracts.</summary>
    public sealed class TwinCatStringRoot
    {
        /// <summary>Gets or sets the field strings fixture value.</summary>
        public STRING_80_WRAPPER[] FieldStrings { get; init; } = [];

        /// <summary>Gets or sets the property strings fixture value.</summary>
        public STRING_80_WRAPPER[] PropertyStrings { get; init; } = [];

        /// <summary>Provides to string array behavior.</summary>
        /// <param name = "values">The values input.</param>
        /// <returns>The requested test value.</returns>
        public static string[] ToStringArray(STRING_80_WRAPPER[] values) => Array.ConvertAll(values, static value => value.Value);
    }

    /// <summary>Exercises twin cat string root without converter contracts.</summary>
    public sealed class TwinCatStringRootWithoutConverter
    {
        /// <summary>Gets or sets the property strings fixture value.</summary>
        public STRING_80_WRAPPER[] PropertyStrings { get; init; } = [];
    }

    /// <summary>Exercises throwing setter root contracts.</summary>
    public sealed class ThrowingSetterRoot
    {
        /// <summary>Gets or sets the throwing fixture value.</summary>
        public int Throwing
        {
            get => field;
            set => throw new InvalidOperationException("Setter failed.");
        } = 1;
    }

    /// <summary>Exercises throwing getter root contracts.</summary>
    public sealed class ThrowingGetterRoot
    {
        /// <summary>Gets the throwing fixture value.</summary>
        public int Throwing => throw new InvalidOperationException("Getter failed.");
    }

    /// <summary>Exercises throwing nested field root contracts.</summary>
    public sealed class ThrowingNestedFieldRoot
    {
        /// <summary>Gets or sets the throwing field fixture value.</summary>
        public ThrowingNestedField ThrowingField { get; set; } = new();
    }

    /// <summary>Exercises throwing nested property root contracts.</summary>
    public sealed class ThrowingNestedPropertyRoot
    {
        /// <summary>Gets the child fixture value.</summary>
        public ReflectionChild Child => throw new InvalidOperationException("Nested property getter failed.");
    }

    /// <summary>Exercises throwing nested field contracts.</summary>
    public sealed class ThrowingNestedField
    {
        /// <summary>Gets the throwing fixture value.</summary>
        public int Throwing => throw new InvalidOperationException("Nested getter failed.");
    }

    /// <summary>Exercises indexed root contracts.</summary>
    public sealed class IndexedRoot
    {
        /// <summary>Gets or sets the value fixture value.</summary>
        public int Value { get; set; } = 1;

        /// <summary>Gets or sets a fixture value at the requested index.</summary>
        /// <param name = "index">The lookup index.</param>
        public int this[int index]
        {
            get => index;
            set
            {
            }
        }
    }

    /// <summary>Exercises serialization constructor hash table rx contracts.</summary>
    private sealed class SerializationConstructorHashTableRx : HashTableRx
    {
        /// <summary>Initializes a new instance of the SerializationConstructorHashTableRx fixture.</summary>
        public SerializationConstructorHashTableRx()
            : base(null!, default)
        {
        }
    }

    /// <summary>Exercises throwing hash table rx contracts.</summary>
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

        /// <summary>Gets the count fixture value.</summary>
        public int Count => 0;

        /// <summary>Gets the is synchronized fixture value.</summary>
        public bool IsSynchronized => false;

        /// <summary>Gets the observe all fixture value.</summary>
        public IObservable<(string key, object? value)> ObserveAll => new ManualObservable<(string key, object? value)>();

        /// <summary>Gets the sync root fixture value.</summary>
        public object SyncRoot { get; } = new();

        /// <summary>Gets the tag fixture value.</summary>
        public HashTable Tag { get; } = [];

        /// <summary>Gets or sets the use upper case fixture value.</summary>
        public bool UseUpperCase { get; set; }

        /// <summary>Gets the structure fixture value.</summary>
        public object? Structure => null;

        /// <summary>Gets or sets a fixture value at the requested index.</summary>
        /// <param name = "fullName">The lookup index.</param>
        public object? this[string fullName]
        {
            get => throw new InvalidOperationException("Read failed.");
            set
            {
            }
        }

        /// <summary>Provides add behavior.</summary>
        /// <param name = "key">The key input.</param>
        /// <param name = "value">The value input.</param>
        public void Add(object key, object? value)
        {
        }

        /// <summary>Provides add behavior.</summary>
        /// <param name = "key">The key input.</param>
        /// <param name = "value">The value input.</param>
        public void Add(object key, HashTableRx value)
        {
        }

        /// <summary>Provides contains key behavior.</summary>
        /// <param name = "key">The key input.</param>
        /// <param name = "searchAll">The search all input.</param>
        /// <returns>The requested test value.</returns>
        public bool ContainsKey(object key, bool searchAll) => false;

        /// <summary>Provides copy to behavior.</summary>
        /// <param name = "array">The array input.</param>
        /// <param name = "index">The index input.</param>
        public void CopyTo(Array array, int index)
        {
        }

        /// <summary>Provides dispose behavior.</summary>
        public void Dispose()
        {
        }

        /// <summary>Provides get enumerator behavior.</summary>
        /// <returns>The requested test value.</returns>
        public IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();

        /// <summary>Provides set structure behavior.</summary>
        /// <param name = "value">The value input.</param>
        public void SetStructure(object? value)
        {
        }
    }
}
