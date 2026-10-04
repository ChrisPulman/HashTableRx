// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.
using TUnit.Assertions;
using TUnit.Core;

namespace CP.Collections.Tests;

/// <summary>Tests for nested indexer access and observation.</summary>
public class NestedAccessTests
{
    /// <summary>Defines the PairCount test input.</summary>
    private const int PairCount = 2;

    /// <summary>Defines the TripleCount test input.</summary>
    private const int TripleCount = 3;

    /// <summary>Defines the InitialPropertyValue test input.</summary>
    private const int InitialPropertyValue = 7;

    /// <summary>Defines the InitialValue test input.</summary>
    private const int InitialValue = 5;

    /// <summary>Defines the NestedValue test input.</summary>
    private const int NestedValue = 6;

    /// <summary>Defines the ABCPath test input.</summary>
    private const string ABCPath = "A.B.C";

    /// <summary>Defines the FirstUpdate test input.</summary>
    private const int FirstUpdate = 10;

    /// <summary>Flat and nested writes preserve normalization and notification ordering.</summary>
    /// <param name = "path">The path input.</param>
    /// <param name = "useUpperCase">The use upper case input.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments("Value", false)]
    [Arguments("Value", true)]
    [Arguments("Root.Value", false)]
    [Arguments("Root.Value", true)]
    public async Task IndexerPreservesNotificationOrder(string path, bool useUpperCase)
    {
        using var table = new HashTableRx(useUpperCase);
        table[path] = 1;
        var normalizedPath = useUpperCase ? path.ToUpperInvariant() : path;
        var notifications = new List<(string kind, string? path, object? value)>();
        table.PropertyChanging += (_, args) => notifications.Add(("changing", args.PropertyName, table[path]));
        using var subscription = table.ObserveAll.Subscribe(new TestObserver<(string key, object? value)>(change => notifications.Add(("observable", change.key, change.value))));
        table.PropertyChanged += (_, args) => notifications.Add(("changed", args.PropertyName, table[path]));
        notifications.Clear();
        table[path] = PairCount;
        await Assert.That(notifications.Count).IsEqualTo(TripleCount);
        await Assert.That(notifications[0]).IsEqualTo(("changing", (string?)normalizedPath, (object?)1));
        await Assert.That(notifications[1]).IsEqualTo(("observable", (string?)normalizedPath, (object?)PairCount));
        await Assert.That(notifications[PairCount]).IsEqualTo(("changed", (string?)normalizedPath, (object?)PairCount));
        await Assert.That(table[normalizedPath]).IsEqualTo((object?)PairCount);
    }

    /// <summary>Empty path segments remain valid dictionary keys during dotted traversal.</summary>
    /// <param name = "path">The path input.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    [Arguments(".")]
    [Arguments(".Value")]
    [Arguments("Root.")]
    [Arguments("Root..Value")]
    public async Task IndexerPreservesEmptyPathSegments(string path)
    {
        using var table = new HashTableRx(false);
        table[path] = InitialPropertyValue;
        await Assert.That(table[path]).IsEqualTo((object?)InitialPropertyValue);
        await Assert.That(table["Missing.Value"]).IsNull();
    }

    /// <summary>Empty keys added through the base API can be read, while empty-path writes remain ignored.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexerPreservesEmptyKeyAndNullPathBehavior()
    {
        using var table = new HashTableRx(false);
        table.Add(string.Empty, InitialValue);
        table[string.Empty] = NestedValue;
        table[null!] = InitialPropertyValue;
        await Assert.That(table[string.Empty]).IsEqualTo((object?)InitialValue);
        await Assert.That(table[null!]).IsNull();
        await Assert.That(table["Missing"]).IsNull();
        await Assert.That(table.Count).IsEqualTo(1);
    }

    /// <summary>Indexer can set and get using dotted full path.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task IndexerSetAndGetUsingFullPath()
    {
        using var ht = new HashTableRx(false);
        ht[ABCPath] = FirstUpdate;
        await Assert.That((int?)ht[ABCPath]).IsEqualTo(FirstUpdate);
    }

    /// <summary>Observe can subscribe to nested path updates.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Test]
    public async Task ObserveNestedPath()
    {
        using var ht = new HashTableRx(false);
        int? seen = null;
        using var sub = ht.Observe(ABCPath, static value => (int)value!).Subscribe(new TestObserver<int>(v => seen = v));
        ht[ABCPath] = InitialPropertyValue;
        await Assert.That(seen).IsEqualTo(InitialPropertyValue);
    }
}
